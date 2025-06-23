using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace WebViewRPC
{
    /// <summary>
    /// Set of 'MethodName' -> 'Handler' mappings
    /// </summary>
    public class ServiceDefinition
    {
        public Dictionary<string, Func<ByteString, ByteString>> MethodHandlers
            = new Dictionary<string, Func<ByteString, ByteString>>();
            
        public Dictionary<string, Func<ByteString, UniTask<ByteString>>> AsyncMethodHandlers
            = new Dictionary<string, Func<ByteString, UniTask<ByteString>>>();
    }
}