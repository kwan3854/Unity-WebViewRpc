using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace WebViewRPC
{
    /// <summary>
    /// Represents a service definition with method handlers.
    /// All methods are async by default.
    /// </summary>
    public class ServiceDefinition
    {
        /// <summary>
        /// Dictionary mapping method names to their async handlers.
        /// </summary>
        public Dictionary<string, Func<ByteString, UniTask<ByteString>>> MethodHandlers { get; set; } = 
            new Dictionary<string, Func<ByteString, UniTask<ByteString>>>();
    }
}