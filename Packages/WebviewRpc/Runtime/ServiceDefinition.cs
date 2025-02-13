using System;
using System.Collections.Generic;
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
    }
}