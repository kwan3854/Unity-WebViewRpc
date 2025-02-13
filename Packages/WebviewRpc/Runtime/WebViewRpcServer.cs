using System;
using System.Collections.Generic;
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
            }
        }

        private void OnBridgeMessage(string base64)
        {
            if (_disposed) return;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var env = RpcEnvelope.Parser.ParseFrom(bytes);

                if (env.IsRequest)
                {
                    HandleRequest(env);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while handling message: {ex}");
            }
        }

        private void HandleRequest(RpcEnvelope reqEnv)
        {
            var respEnv = new RpcEnvelope
            {
                RequestId = reqEnv.RequestId,
                IsRequest = false,
                Method = reqEnv.Method,
            };

            try
            {
                if (_methodHandlers.TryGetValue(reqEnv.Method, out var handler))
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
