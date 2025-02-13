using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace WebViewRPC
{
    /// <summary>
    /// RPC Client for WebView
    /// </summary>
    public class WebViewRpcClient : IDisposable
    {
        private readonly IWebViewBridge _bridge;

        // Mapping RequestId -> TaskCompletionSource
        private readonly Dictionary<string, TaskCompletionSource<RpcEnvelope>> _pendingRequests
            = new Dictionary<string, TaskCompletionSource<RpcEnvelope>>();

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
        public async Task<TResponse> CallMethodAsync<TResponse>(string methodName, IMessage request)
            where TResponse : IMessage<TResponse>, new()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WebViewRpcClient));
            
            var requestId = Guid.NewGuid().ToString("N");
            
            var tcs = new TaskCompletionSource<RpcEnvelope>();
            lock (_pendingRequests)
            {
                _pendingRequests[requestId] = tcs;
            }

            // (Protobuf -> byte[] -> Base64)
            var requestBytes = request.ToByteArray();
            var env = new RpcEnvelope
            {
                RequestId = requestId,
                IsRequest = true,
                Method = methodName,
                Payload = ByteString.CopyFrom(requestBytes)
            };
            var envBytes = env.ToByteArray();
            var envBase64 = Convert.ToBase64String(envBytes);

            // Send to WebView
            _bridge.SendMessageToWeb(envBase64);
            
            var responseEnv = await tcs.Task;
            if (!string.IsNullOrEmpty(responseEnv.Error))
            {
                throw new Exception($"RPC Error: {responseEnv.Error}");
            }

            // Payload -> TResponse
            var resp = new TResponse();
            resp.MergeFrom(responseEnv.Payload);
            return resp;
        }
        
        private void OnBridgeMessage(string base64)
        {
            if (_disposed) return;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var env = RpcEnvelope.Parser.ParseFrom(bytes);

                if (!env.IsRequest)
                {
                    HandleResponse(env);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception while handling message: {ex}");
            }
        }

        private void HandleResponse(RpcEnvelope env)
        {
            TaskCompletionSource<RpcEnvelope> tcs = null;
            lock (_pendingRequests)
            {
                _pendingRequests.Remove(env.RequestId, out tcs);
            }
            if (tcs != null)
            {
                tcs.TrySetResult(env);
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
