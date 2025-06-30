using System;
using Cysharp.Threading.Tasks;
using HelloWorld;
using UnityEngine;

namespace SampleRpc
{
    public class HelloWorldService : HelloServiceBase
    {
        public override async UniTask<HelloResponse> SayHello(HelloRequest request)
        {
            Debug.Log($"[C# Server] Received request: Name='{request.Name}', LongMessage Length={request.LongMessage.Length}, RepeatCount={request.RepeatCount}");

            // Simulate some async work
            await UniTask.Delay(TimeSpan.FromMilliseconds(50));

            return new HelloResponse
            {
                Greeting = $"Hello from C#, {request.Name}!",
                EchoedMessage = request.LongMessage, // Echo back for bidirectional chunking test
                ProcessedAt = DateTime.UtcNow.ToString("o"),
                OriginalMessageSize = request.LongMessage.Length
            };
        }
    }
}
