using System;
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
        await webViewPrefab.WaitUntilInitialized();
        webViewPrefab.WebView.LoadUrl("http://localhost:8081");

        await webViewPrefab.WebView.WaitForNextPageLoadToFinish();
        
        var bridge = new ViewplexWebViewBridge(webViewPrefab);
        var server = new WebViewRPC.WebViewRpcServer(bridge)
        {
            Services =
            {
                HelloService.BindService(new HelloWorldService()),
            }
        };
        
        server.Start();
        
        var rpcClient = new WebViewRPC.WebViewRpcClient(bridge);
        var client = new HelloServiceClient(rpcClient);
        
        var response = await client.SayHello(new HelloRequest()
        {
            Name = "World"
        });
        
        Debug.Log($"Received response: {response.Greeting}");
    }
    
    void Update()
    {
        
    }
}
