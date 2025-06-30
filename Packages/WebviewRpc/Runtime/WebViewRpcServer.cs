using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

namespace WebViewRPC
{
    public class WebViewRpcServer : IDisposable
    {
        private readonly IWebViewBridge _bridge;
        private readonly ChunkAssembler _chunkAssembler = new();
        private bool _disposed;

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
                            await HandleRequestAsync(completeEnvelope);
                        }
                    }
                    // else: waiting for more chunks
                }
                else
                {
                    // Process as regular message
                    if (envelope.IsRequest)
                    {
                        await HandleRequestAsync(envelope);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while handling message: {ex}");
            }
        }

        private async UniTask HandleRequestAsync(RpcEnvelope requestEnvelope)
        {
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

            // Send response
            if (responsePayload != null)
            {
                var responseBytes = responsePayload.ToByteArray();
                
                // Check if chunking is needed
                int effectivePayloadSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
                if (WebViewRpcConfiguration.EnableChunking && 
                    responseBytes.Length > effectivePayloadSize)
                {
                    // Send as chunks
                    await SendChunkedMessage(requestEnvelope.RequestId, requestEnvelope.Method, 
                        responseBytes, false, error);
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
                    
                    // Only set Error if it's not null or empty
                    if (!string.IsNullOrEmpty(error))
                    {
                        responseEnvelope.Error = error;
                    }
                    
                    var bytes = responseEnvelope.ToByteArray();
                    var base64 = Convert.ToBase64String(bytes);
                    _bridge.SendMessageToWeb(base64);
                }
            }
            else
            {
                // Error response
                var responseEnvelope = new RpcEnvelope
                {
                    RequestId = requestEnvelope.RequestId,
                    IsRequest = false,
                    Method = requestEnvelope.Method,
                    Error = error ?? "Unknown error"
                };
                
                var bytes = responseEnvelope.ToByteArray();
                var base64 = Convert.ToBase64String(bytes);
                _bridge.SendMessageToWeb(base64);
            }
        }
        
        private async UniTask SendChunkedMessage(string requestId, string method, byte[] data, 
            bool isRequest, string error = null)
        {
            var chunkSetId = $"{requestId}_{Guid.NewGuid():N}";
            int effectivePayloadSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
            var totalChunks = (int)Math.Ceiling((double)data.Length / effectivePayloadSize);
            
            for (int i = 1; i <= totalChunks; i++)
            {
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
                
                // Optional: Add small delay between chunks to avoid overwhelming the bridge
                if (i < totalChunks)
                {
                    await UniTask.Delay(1);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _bridge.OnMessageReceived -= OnBridgeMessage;
                _disposed = true;
            }
        }
    }
}
