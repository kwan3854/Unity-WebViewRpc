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
        private bool _webViewDestroyed;
        
        public ViewplexWebViewBridge(CanvasWebViewPrefab webViewPrefab)
        {
            _webViewPrefab = webViewPrefab;
            
            // Store handler for proper cleanup
            _webViewPrefab.WebView.MessageEmitted += OnWebViewMessageEmitted;
            
            // WebView의 파괴를 감지하기 위한 이벤트 등록
            // if (_webViewPrefab.WebView is IWebView webView)
            // {
            //     webView.Disposed += OnWebViewDisposed;
            // }
        }
        
        private void OnWebViewDisposed(object sender, EventArgs args)
        {
            _webViewDestroyed = true;
        }
        
        private void OnWebViewMessageEmitted(object sender, EventArgs<string> args)
        {
            // WebView가 파괴된 후에는 메시지를 처리하지 않음
            if (_disposed || _webViewDestroyed) return;
            
            OnMessageReceived?.Invoke(args.Value);
        }
        
        public void SendMessageToWeb(string message)
        {
            // WebView가 파괴되었거나 dispose된 경우 메시지를 보내지 않음
            if (_disposed || _webViewDestroyed || _webViewPrefab == null || _webViewPrefab.WebView == null)
            {
                // 디버그 로그로 확인 (필요시 주석 처리)
                // UnityEngine.Debug.Log("[ViewplexWebViewBridge] Message dropped - WebView is destroyed");
                return;
            }
            
            try
            {
                _webViewPrefab.WebView.PostMessage(message);
            }
            catch (Exception ex)
            {
                // WebView가 이미 파괴된 경우 예외 처리
                UnityEngine.Debug.LogWarning($"[ViewplexWebViewBridge] Failed to send message: {ex.Message}");
            }
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
                    
                    // if (_webViewPrefab.WebView is IWebView webView)
                    // {
                    //     webView.Disposed -= OnWebViewDisposed;
                    // }
                }
                
                OnMessageReceived = null;
            }
        }
    }
}
