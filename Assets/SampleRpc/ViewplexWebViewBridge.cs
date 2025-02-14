using System;
using Vuplex.WebView;
using WebViewRPC;

namespace SampleRpc
{
    public class ViewplexWebViewBridge : IWebViewBridge
    {
        public event Action<string> OnMessageReceived;
        private readonly CanvasWebViewPrefab _webViewPrefab;
    
        public ViewplexWebViewBridge(CanvasWebViewPrefab webViewPrefab)
        {
            _webViewPrefab = webViewPrefab;
        
            _webViewPrefab.WebView.MessageEmitted += (sender, args) =>
            {
                OnMessageReceived?.Invoke(args.Value);
            };
        }
    
        public void SendMessageToWeb(string message)
        {
            _webViewPrefab.WebView.PostMessage(message);
        }
    }
}
