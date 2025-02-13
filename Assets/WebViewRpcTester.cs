using System;
using System.Threading.Tasks;
using HelloWorld;
using SampleRpc;
using UnityEngine;
using Vuplex.WebView;
using WebViewRPC;

public class WebViewRpcTester : MonoBehaviour
{
    [SerializeField] private CanvasWebViewPrefab webViewPrefab;

    private void Awake()
    {
        Web.ClearAllData();
    }

    private async void Start()
    {
        await InitializeWebView(webViewPrefab);
        
        // Initialize C# Server to handle JS -> C# RPC
        var bridge = new ViewplexWebViewBridge(webViewPrefab);
        var server = new WebViewRPC.WebViewRpcServer(bridge)
        {
            Services =
            {
                HelloService.BindService(new HelloWorldService()),
            }
        };
        
        server.Start();
        
        
        // Initialize C# Client to handle C# -> JS RPC
        var rpcClient = new WebViewRPC.WebViewRpcClient(bridge);
        var client = new HelloServiceClient(rpcClient);
        
        // Call RPC method
        var response = await client.SayHello(new HelloRequest()
        {
            Name = "World"
        });
        
        Debug.Log($"Received response: {response.Greeting}");
    }
    
    private async Task InitializeWebView(CanvasWebViewPrefab webView)
    {
        await webView.WaitUntilInitialized();
        webView.WebView.LoadUrl("http://localhost:8081");
        await webView.WebView.WaitForNextPageLoadToFinish();
    }
}
