using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

namespace WebViewRPC
{
    /// <summary>
    /// RPC Client for WebView
    /// </summary>
    public class WebViewRpcClient : IDisposable
    {
        private bool _disposed;
        
        private readonly IWebViewBridge _bridge;
        private readonly Dictionary<string, UniTaskCompletionSource<RpcEnvelope>> _pendingRequests = new();
        private readonly ChunkAssembler _chunkAssembler = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly object _pendingRequestsLock = new object();


        public WebViewRpcClient(IWebViewBridge bridge)
        {
            _bridge = bridge;
            _bridge.OnMessageReceived += OnBridgeMessage;
        }

        /// <summary>
        /// Call Rpc Method: (methodName, requestMessage) -> TResponse
        /// User should not call this method directly.
        /// Instead, use generated method from .proto file.
        /// </summary>
        public async UniTask<TResponse> CallMethod<TResponse>(string method, IMessage request)
            where TResponse : IMessage<TResponse>, new()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebViewRpcClient), "Cannot call method on disposed client");
            }
            
            var requestId = Guid.NewGuid().ToString("N");
            var requestBytes = request.ToByteArray();
            
            var tcs = new UniTaskCompletionSource<RpcEnvelope>();
            lock (_pendingRequestsLock)
            {
                _pendingRequests[requestId] = tcs;
            }

            try
            {
                // Check if chunking is needed
                int effectivePayloadSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
                if (WebViewRpcConfiguration.EnableChunking && 
                    requestBytes.Length > effectivePayloadSize)
                {
                    // Send as chunks
                    await SendChunkedMessage(requestId, method, requestBytes, true);
                }
                else
                {
                    // Send as single message
                    var envelope = new RpcEnvelope
                    {
                        RequestId = requestId,
                        IsRequest = true,
                        Method = method,
                        Payload = ByteString.CopyFrom(requestBytes)
                    };
                    
                    var bytes = envelope.ToByteArray();
                    var base64 = Convert.ToBase64String(bytes);
                    _bridge.SendMessageToWeb(base64);
                }

                var responseEnvelope = await tcs.Task;

                if (!string.IsNullOrEmpty(responseEnvelope.Error))
                {
                    throw new Exception($"RPC Error: {responseEnvelope.Error}");
                }

                var response = new TResponse();
                response.MergeFrom(responseEnvelope.Payload);
                return response;
            }
            finally
            {
                lock (_pendingRequestsLock)
                {
                    _pendingRequests.Remove(requestId);
                }
            }
        }
        
        private async UniTask SendChunkedMessage(string requestId, string method, byte[] data, bool isRequest)
        {
            // Check disposed state
            if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested) return;
            
            int effectivePayloadSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
            var totalChunks = (int)Math.Ceiling((double)data.Length / effectivePayloadSize);
            
            for (int i = 1; i <= totalChunks; i++)
            {
                // Check disposed state before each chunk
                if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested) return;
                
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
                        ChunkSetId = "", // Not used anymore, but required by protobuf
                        ChunkIndex = i,
                        TotalChunks = totalChunks,
                        OriginalSize = data.Length
                    }
                };
                
                var bytes = envelope.ToByteArray();
                var base64 = Convert.ToBase64String(bytes);
                _bridge.SendMessageToWeb(base64);
            }

            await UniTask.CompletedTask;
        }
        
        private void OnBridgeMessage(string base64)
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
                    var completeData = _chunkAssembler.TryAssemble(envelope, out var timedOutRequestIds);
                    
                    // Handle timed out requests
                    foreach (var requestId in timedOutRequestIds)
                    {
                        UniTaskCompletionSource<RpcEnvelope> tcs = null;
                        lock (_pendingRequestsLock)
                        {
                            _pendingRequests.TryGetValue(requestId, out tcs);
                            _pendingRequests.Remove(requestId);
                        }
                        
                        if (tcs != null)
                        {
                            tcs.TrySetException(new TimeoutException($"Chunk reassembly timeout for request {requestId} after {WebViewRpcConfiguration.ChunkTimeoutSeconds} seconds"));
                        }
                    }
                    
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
                        
                        ProcessCompleteEnvelope(completeEnvelope);
                    }
                    // else: waiting for more chunks
                }
                else
                {
                    // Process as regular message
                    ProcessCompleteEnvelope(envelope);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing message: {ex}");
            }
        }
        
        private void ProcessCompleteEnvelope(RpcEnvelope envelope)
        {
            if (!envelope.IsRequest)
            {
                UniTaskCompletionSource<RpcEnvelope> tcs = null;
                lock (_pendingRequestsLock)
                {
                    _pendingRequests.TryGetValue(envelope.RequestId, out tcs);
                }
                
                if (tcs != null)
                {
                    tcs.TrySetResult(envelope);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // Cancel all operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                _bridge.OnMessageReceived -= OnBridgeMessage;
                
                // Cancel all pending requests
                lock (_pendingRequestsLock)
                {
                    foreach (var pending in _pendingRequests.Values)
                    {
                        pending.TrySetCanceled();
                    }
                    _pendingRequests.Clear();
                }
                
                // Note: Do not dispose the bridge here as it may be shared
                // The owner of the bridge should dispose it
            }
        }
    }
}
