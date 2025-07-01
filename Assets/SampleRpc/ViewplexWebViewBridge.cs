using System;
using Cysharp.Threading.Tasks;
using Vuplex.WebView;
using WebViewRPC;

public class ViewplexWebViewBridge : IWebViewBridge
{
    public event Action<string> OnMessageReceived;

    private readonly IWebView _webView;
    private bool _disposed;

    public ViewplexWebViewBridge(CanvasWebViewPrefab webViewPrefab)
    {
        _webView = webViewPrefab.WebView;
        _webView.MessageEmitted += OnWebViewMessageEmitted;

        // webViewPrefab의 GameObject가 파괴될 때 Dispose를 호출하도록 콜백을 등록합니다.
        webViewPrefab.GetCancellationTokenOnDestroy().Register(Dispose);
    }

    private void OnWebViewMessageEmitted(object sender, EventArgs<string> args)
    {
        if (_disposed) return;
        OnMessageReceived?.Invoke(args.Value);
    }

    public void SendMessageToWeb(string message)
    {
        if (_disposed)
        {
            return;
        }

        _webView.PostMessage(message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_webView != null)
        {
            _webView.MessageEmitted -= OnWebViewMessageEmitted;
        }

        OnMessageReceived = null;
    }
}