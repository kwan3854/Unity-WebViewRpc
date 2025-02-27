// AUTO-GENERATED by protoc-gen-webviewrpc
using Google.Protobuf;
using WebViewRPC;

namespace HelloWorld
{
    /// <summary>
    /// Override your own implementation of this class
    /// </summary>
    public abstract class HelloServiceBase
    {
        
        public abstract HelloResponse SayHello(HelloRequest request);
        
    }

    /// <summary>
    /// Provides "BindService" method to bind your implementation to the generated service definition.
    /// Works similar to gRPC's ServerServiceDefinition.BindService.
    /// </summary>
    public static class HelloService
    {
        public static ServiceDefinition BindService(HelloServiceBase impl)
        {
            var def = new ServiceDefinition();

            
            def.MethodHandlers["HelloService.SayHello"] = (reqBytes) =>
            {
                var req = new HelloRequest();
                req.MergeFrom(reqBytes);
                var resp = impl.SayHello(req);
                return Google.Protobuf.ByteString.CopyFrom(resp.ToByteArray());
            };
            

            return def;
        }
    }
}