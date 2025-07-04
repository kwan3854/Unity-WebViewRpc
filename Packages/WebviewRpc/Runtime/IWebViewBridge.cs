using System;

namespace WebViewRPC
{
    /// <summary>
    /// Interface for communication between C# and JS
    /// - SendMessageToWeb: C# -> JS send string
    /// - OnMessageReceived: JS -> C# receive string
    /// </summary>
    public interface IWebViewBridge : IDisposable
    {
        /// <summary>
        /// C# to WebView send string
        /// </summary>
        public void SendMessageToWeb(string message);

        /// <summary>
        /// JS to C# receive string
        /// </summary>
        public event Action<string> OnMessageReceived;
    }
}
