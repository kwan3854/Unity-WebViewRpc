using System;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HelloWorld;
using UnityEngine;
using Vuplex.WebView;
using WebViewRPC;

namespace SampleRpc
{
    public class WebViewRpcTester : MonoBehaviour
    {
        [SerializeField] private CanvasWebViewPrefab webViewPrefab;
        private HelloServiceClient _client;

        private void Awake()
        {
            Web.ClearAllData();
        }

        private async void Start()
        {
            // Set chunking configuration
            WebViewRpcConfiguration.MaxChunkSize = 900; // 900 bytes
            WebViewRpcConfiguration.EnableChunking = true;
            
            await InitializeWebView(webViewPrefab);
        
            // Initialize C# Server to handle JS -> C# RPC
            var bridge = new ViewplexWebViewBridge(webViewPrefab);
            var server = new WebViewRpcServer(bridge);
            server.Services.Add(HelloService.BindService(new HelloWorldService()));
            server.Start();
        
            // Initialize C# Client to handle C# -> JS RPC
            var rpcClient = new WebViewRpcClient(bridge);
            _client = new HelloServiceClient(rpcClient);
        
            // Run bidirectional test
            await UniTask.Delay(3000); // Wait for web to be ready
            // RunBidirectionalChunkingTest();
        }

        private async void RunBidirectionalChunkingTest()
        {
            Debug.Log("--- [C# Client] Sending Hello Request ---");
            
            // Create a very long message for chunking test
            var sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                sb.Append(new string((char)('X' + i), 2000));
            }
            var longMessage = sb.ToString();

            var request = new HelloRequest
            {
                Name = "Chunking Test from Unity",
                LongMessage = longMessage,
                RepeatCount = 1
            };
            
            Debug.Log($"[C# Client] Request: Name='{request.Name}', LongMessage Length={request.LongMessage.Length}");

            try
            {
                var response = await _client.SayHello(request);

                Debug.Log("--- [C# Client] Received Hello Response ---");
                Debug.Log($"[C# Client] Response: Greeting='{response.Greeting}', EchoedMessage Length={response.EchoedMessage.Length}, ProcessedAt='{response.ProcessedAt}', OriginalSize={response.OriginalMessageSize}");
                Debug.Log("------------------------------------");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[C# Client] Error: {ex.Message}");
            }
        }
    
        private async Task InitializeWebView(CanvasWebViewPrefab webView)
        {
            await webView.WaitUntilInitialized();
            webView.WebView.LoadUrl("http://localhost:8081");
            await webView.WebView.WaitForNextPageLoadToFinish();
        }

        private void Update()
        {
            // if space key is pressed, send a message to the web view
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Space key pressed, sending message to web view.");
                RunBidirectionalChunkingTest();
            }
        }
    }
}
