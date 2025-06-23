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

                if (envelope.IsRequest)
                {
                    await HandleRequestAsync(envelope);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while handling message: {ex}");
            }
        }

        private async UniTask HandleRequestAsync(RpcEnvelope requestEnvelope)
        {
            var responseEnvelope = new RpcEnvelope
            {
                RequestId = requestEnvelope.RequestId,
                IsRequest = false,
                Method = requestEnvelope.Method,
            };

            try
            {
                if (_methodHandlers.TryGetValue(requestEnvelope.Method, out var handler))
                {
                    var responsePayload = await handler(requestEnvelope.Payload);
                    responseEnvelope.Payload = responsePayload;
                }
                else
                {
                    responseEnvelope.Error = $"Unknown method: {requestEnvelope.Method}";
                }
            }
            catch (Exception ex)
            {
                responseEnvelope.Error = ex.Message;
            }

            var responseBytes = responseEnvelope.ToByteArray();
            var responseBase64 = Convert.ToBase64String(responseBytes);
            _bridge.SendMessageToWeb(responseBase64);
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
