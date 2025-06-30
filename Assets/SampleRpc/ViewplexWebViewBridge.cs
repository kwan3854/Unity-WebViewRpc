using System;
using Vuplex.WebView;
using WebViewRPC;

namespace SampleRpc
{
    public class ViewplexWebViewBridge : IWebViewBridge
    {
        public event Action<string> OnMessageReceived;
        private readonly CanvasWebViewPrefab _webViewPrefab;
        private bool _disposed;
        
        public ViewplexWebViewBridge(CanvasWebViewPrefab webViewPrefab)
        {
            _webViewPrefab = webViewPrefab;
            
            // Store handler for proper cleanup
            _webViewPrefab.WebView.MessageEmitted += OnWebViewMessageEmitted;
        }
        
        private void OnWebViewMessageEmitted(object sender, EventArgs<string> args)
        {
            OnMessageReceived?.Invoke(args.Value);
        }
        
        public void SendMessageToWeb(string message)
        {
            if (_disposed || _webViewPrefab == null || _webViewPrefab.WebView == null)
            {
                // WebView is already destroyed, don't send message
                return;
            }
            
            _webViewPrefab.WebView.PostMessage(message);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // Unsubscribe from WebView events
                if (_webViewPrefab != null && _webViewPrefab.WebView != null)
                {
                    _webViewPrefab.WebView.MessageEmitted -= OnWebViewMessageEmitted;
                }
                
                OnMessageReceived = null;
            }
        }
    }
}
