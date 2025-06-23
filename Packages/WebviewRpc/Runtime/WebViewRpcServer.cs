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
        private bool _disposed;

        /// <summary>
        /// You can add multiple services to the server.
        /// </summary>
        public List<ServiceDefinition> Services { get; } = new();
        
        private readonly Dictionary<string, Func<ByteString, ByteString>> _methodHandlers = new();
        private readonly Dictionary<string, Func<ByteString, UniTask<ByteString>>> _asyncMethodHandlers = new();

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
            foreach (var sd in Services)
            {
                foreach (var kv in sd.MethodHandlers)
                {
                    _methodHandlers[kv.Key] = kv.Value;
                }
                
                foreach (var kv in sd.AsyncMethodHandlers)
                {
                    _asyncMethodHandlers[kv.Key] = kv.Value;
                }
            }
        }

        private async void OnBridgeMessage(string base64)
        {
            if (_disposed) return;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var env = RpcEnvelope.Parser.ParseFrom(bytes);

                if (env.IsRequest)
                {
                    await HandleRequestAsync(env);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while handling message: {ex}");
            }
        }

        private async UniTask HandleRequestAsync(RpcEnvelope reqEnv)
        {
            var respEnv = new RpcEnvelope
            {
                RequestId = reqEnv.RequestId,
                IsRequest = false,
                Method = reqEnv.Method,
            };

            try
            {
                // Check async handlers first
                if (_asyncMethodHandlers.TryGetValue(reqEnv.Method, out var asyncHandler))
                {
                    var responsePayload = await asyncHandler(reqEnv.Payload);
                    respEnv.Payload = ByteString.CopyFrom(responsePayload.ToByteArray());
                }
                // Fallback to sync handlers
                else if (_methodHandlers.TryGetValue(reqEnv.Method, out var handler))
                {
                    var responsePayload = handler(reqEnv.Payload);
                    respEnv.Payload = ByteString.CopyFrom(responsePayload.ToByteArray());
                }
                else
                {
                    respEnv.Error = $"Unknown method: {reqEnv.Method}";
                }
            }
            catch (Exception ex)
            {
                respEnv.Error = ex.Message;
            }

            var respBytes = respEnv.ToByteArray();
            var respBase64 = Convert.ToBase64String(respBytes);
            _bridge.SendMessageToWeb(respBase64);
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
