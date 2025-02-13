using HelloWorld;
using UnityEngine;

namespace SampleRpc
{
    public class HelloWorldService : HelloServiceBase
    {
        public override HelloResponse SayHello(HelloRequest request)
        {
            Debug.Log($"Received request: {request.Name}");
            return new HelloResponse()
            {
                Greeting = $"Hello, {request.Name}!"
            };
        }
    }
}
