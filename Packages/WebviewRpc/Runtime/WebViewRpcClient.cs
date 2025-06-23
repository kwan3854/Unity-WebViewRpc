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
        private readonly IWebViewBridge _bridge;
        private readonly Dictionary<string, UniTaskCompletionSource<RpcEnvelope>> _pendingRequests = new();
        private int _requestIdCounter = 1;
        private bool _disposed;

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
            var requestId = (_requestIdCounter++).ToString();
            var envelope = new RpcEnvelope
            {
                RequestId = requestId,
                IsRequest = true,
                Method = method,
                Payload = ByteString.CopyFrom(request.ToByteArray())
            };

            var tcs = new UniTaskCompletionSource<RpcEnvelope>();
            _pendingRequests[requestId] = tcs;

            try
            {
                var bytes = envelope.ToByteArray();
                var base64 = Convert.ToBase64String(bytes);
                _bridge.SendMessageToWeb(base64);

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
                _pendingRequests.Remove(requestId);
            }
        }
        
        private void OnBridgeMessage(string base64)
        {
            if (_disposed) return;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var envelope = RpcEnvelope.Parser.ParseFrom(bytes);

                if (!envelope.IsRequest && _pendingRequests.TryGetValue(envelope.RequestId, out var tcs))
                {
                    tcs.TrySetResult(envelope);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing message: {ex}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _bridge.OnMessageReceived -= OnBridgeMessage;
                
                // Cancel all pending requests
                foreach (var pending in _pendingRequests.Values)
                {
                    pending.TrySetCanceled();
                }
                _pendingRequests.Clear();
                
                _disposed = true;
            }
        }
    }
}
