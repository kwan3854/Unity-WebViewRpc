using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

namespace WebViewRPC
{
    public class WebViewRpcServer : IDisposable
    {
        private bool _disposed;
        
        private readonly IWebViewBridge _bridge;
        private readonly ChunkAssembler _chunkAssembler = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// You can add multiple services to the server.
        /// </summary>
        public List<ServiceDefinition> Services { get; } = new();
        
        private readonly Dictionary<string, Func<ByteString, UniTask<ByteString>>> _methodHandlers = new();

        public WebViewRpcServer(IWebViewBridge bridge)
        {
            _bridge = bridge;
            _bridge.OnMessageReceived += OnBridgeMessage;
        }

        /// <summary>
        /// Register ServiceDefinition list to actual method handlers
        /// </summary>
        public void Start()
        {
            foreach (var service in Services)
            {
                foreach (var handler in service.MethodHandlers)
                {
                    _methodHandlers[handler.Key] = handler.Value;
                }
            }
        }

        private async void OnBridgeMessage(string base64)
        {
            if (_disposed) return;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var envelope = RpcEnvelope.Parser.ParseFrom(bytes);

                // Check if this is a chunked message
                if (envelope.ChunkInfo != null)
                {
                    // Try to reassemble
                    var completeData = _chunkAssembler.TryAssemble(envelope);
                    if (completeData != null)
                    {
                        // Create a new envelope with the complete data
                        var completeEnvelope = new RpcEnvelope
                        {
                            RequestId = envelope.RequestId,
                            IsRequest = envelope.IsRequest,
                            Method = envelope.Method,
                            Payload = ByteString.CopyFrom(completeData)
                        };
                        
                        // Only set Error if it's not null or empty
                        if (!string.IsNullOrEmpty(envelope.Error))
                        {
                            completeEnvelope.Error = envelope.Error;
                        }
                        
                        if (completeEnvelope.IsRequest)
                        {
                            await HandleRequestAsync(completeEnvelope, _cancellationTokenSource.Token);
                        }
                    }
                    // else: waiting for more chunks
                }
                else
                {
                    // Process as regular message
                    if (envelope.IsRequest)
                    {
                        await HandleRequestAsync(envelope, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disposing
                Debug.Log($"RPC operation cancelled for disposed server");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while handling message: {ex}");
            }
        }

        private async UniTask HandleRequestAsync(RpcEnvelope requestEnvelope, CancellationToken cancellationToken)
        {
            // Check disposed state before processing
            if (_disposed || cancellationToken.IsCancellationRequested) return;
            
            ByteString responsePayload = null;
            string error = null;

            try
            {
                if (_methodHandlers.TryGetValue(requestEnvelope.Method, out var handler))
                {
                    responsePayload = await handler(requestEnvelope.Payload);
                }
                else
                {
                    error = $"Unknown method: {requestEnvelope.Method}";
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            // Check disposed state before sending response
            if (_disposed || cancellationToken.IsCancellationRequested) return;

            // Send response
            // Check if this is an error response
            if (!string.IsNullOrEmpty(error))
            {
                // Error response
                var responseEnvelope = new RpcEnvelope
                {
                    RequestId = requestEnvelope.RequestId,
                    IsRequest = false,
                    Method = requestEnvelope.Method,
                    Error = error
                };
                
                // Include payload if available (even if empty)
                if (responsePayload != null)
                {
                    responseEnvelope.Payload = responsePayload;
                }
                
                var bytes = responseEnvelope.ToByteArray();
                var base64 = Convert.ToBase64String(bytes);
                _bridge.SendMessageToWeb(base64);
            }
            else if (responsePayload != null)
            {
                // Success response
                var responseBytes = responsePayload.ToByteArray();
                
                // Check if chunking is needed
                int effectivePayloadSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
                if (WebViewRpcConfiguration.EnableChunking && 
                    responseBytes.Length > effectivePayloadSize)
                {
                    // Send as chunks
                    await SendChunkedMessage(requestEnvelope.RequestId, requestEnvelope.Method, 
                        responseBytes, false, null, cancellationToken);
                }
                else
                {
                    // Send as single message
                    var responseEnvelope = new RpcEnvelope
                    {
                        RequestId = requestEnvelope.RequestId,
                        IsRequest = false,
                        Method = requestEnvelope.Method,
                        Payload = responsePayload
                    };
                    
                    var bytes = responseEnvelope.ToByteArray();
                    var base64 = Convert.ToBase64String(bytes);
                    _bridge.SendMessageToWeb(base64);
                }
            }
            else
            {
                // No payload and no error - this is an error condition
                var responseEnvelope = new RpcEnvelope
                {
                    RequestId = requestEnvelope.RequestId,
                    IsRequest = false,
                    Method = requestEnvelope.Method,
                    Error = "Method returned null without error"
                };
                
                var bytes = responseEnvelope.ToByteArray();
                var base64 = Convert.ToBase64String(bytes);
                _bridge.SendMessageToWeb(base64);
            }
        }
        
        private async UniTask SendChunkedMessage(string requestId, string method, byte[] data, 
            bool isRequest, string error = null, CancellationToken cancellationToken = default)
        {
            // Check disposed state before starting
            if (_disposed || cancellationToken.IsCancellationRequested) return;
            
            var chunkSetId = $"{requestId}_{Guid.NewGuid():N}";
            int effectivePayloadSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
            var totalChunks = (int)Math.Ceiling((double)data.Length / effectivePayloadSize);
            
            for (int i = 1; i <= totalChunks; i++)
            {
                // Check disposed state before each chunk
                if (_disposed || cancellationToken.IsCancellationRequested) return;
                
                var offset = (i - 1) * effectivePayloadSize;
                var length = Math.Min(effectivePayloadSize, data.Length - offset);
                var chunkData = new byte[length];
                Array.Copy(data, offset, chunkData, 0, length);
                
                var envelope = new RpcEnvelope
                {
                    RequestId = requestId,
                    IsRequest = isRequest,
                    Method = method,
                    Payload = ByteString.CopyFrom(chunkData),
                    ChunkInfo = new ChunkInfo
                    {
                        ChunkSetId = chunkSetId,
                        ChunkIndex = i,
                        TotalChunks = totalChunks,
                        OriginalSize = data.Length
                    }
                };
                
                // Only set error on the first chunk
                if (i == 1 && !string.IsNullOrEmpty(error))
                {
                    envelope.Error = error;
                }
                
                var bytes = envelope.ToByteArray();
                var base64 = Convert.ToBase64String(bytes);
                _bridge.SendMessageToWeb(base64);
            }
            
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // Cancel all pending operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                _bridge.OnMessageReceived -= OnBridgeMessage;
                
                // Note: Do not dispose the bridge here as it may be shared
                // The owner of the bridge should dispose it
            }
        }
    }
}
