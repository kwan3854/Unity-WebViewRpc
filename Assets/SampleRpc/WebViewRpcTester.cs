using System;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HelloWorld;
using UnityEngine;
using UnityEngine.UI;
using Vuplex.WebView;
using WebViewRPC;

namespace SampleRpc
{
    public class WebViewRpcTester : MonoBehaviour
    {
        [SerializeField] private CanvasWebViewPrefab webViewPrefab;
        [SerializeField] private Button image;
        [SerializeField] private Text text;
        
        private HelloServiceClient _client;
        private WebViewRpcServer _server;
        private WebViewRpcClient _rpcClient;
        private IWebViewBridge _bridge;
        
        // 시작 시간
        private float _startTime;
        
        // 지연 시간 설정 (Inspector에서 조정 가능)
        [SerializeField] private float serviceRegistrationDelay = 3.0f;

        private void Awake()
        {
            Web.ClearAllData();
        }

        private async void Start()
        {
            // Set chunking configuration
            try
            {
                WebViewRpcConfiguration.MaxChunkSize = 1024; // 900 bytes for testing
                WebViewRpcConfiguration.EnableChunking = true;
                WebViewRpcConfiguration.ChunkTimeoutSeconds = 10; // 10 seconds for testing
                
                Debug.Log($"[Configuration] MaxChunkSize: {WebViewRpcConfiguration.MaxChunkSize} bytes");
                Debug.Log($"[Configuration] EnableChunking: {WebViewRpcConfiguration.EnableChunking}");
                Debug.Log($"[Configuration] ChunkTimeoutSeconds: {WebViewRpcConfiguration.ChunkTimeoutSeconds} seconds");
                Debug.Log($"[Configuration] EffectivePayloadSize: {WebViewRpcConfiguration.GetEffectivePayloadSize()} bytes");
                Debug.Log($"[Configuration] MinimumSafeChunkSize: {WebViewRpcConfiguration.GetMinimumSafeChunkSize()} bytes");
                Debug.Log($"[Configuration] IsChunkSizeValid: {WebViewRpcConfiguration.IsChunkSizeValid()}");
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[Configuration Error] {ex.Message}");
                // Use default values
                WebViewRpcConfiguration.MaxChunkSize = 256 * 1024;
            }
            
            await InitializeWebView(webViewPrefab);
            
            Debug.Log($"[EXTREME TIMING TEST] WebView loaded, but intentionally delaying service registration for {serviceRegistrationDelay} seconds...");
            Debug.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Starting delay...");
            
            // 의도적으로 서비스 등록을 지연시킴
            await UniTask.Delay(TimeSpan.FromSeconds(serviceRegistrationDelay));
            
            Debug.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Delay finished, now registering services...");
        
            // Initialize C# Server to handle JS -> C# RPC
            _bridge = new ViewplexWebViewBridge(webViewPrefab);
            _server = new WebViewRpcServer(_bridge);
            _server.Services.Add(HelloService.BindService(new HelloWorldService()));
            _server.Start();
            
            Debug.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Unity RPC Server is now ready!");
        
            // Initialize C# Client to handle C# -> JS RPC
            _rpcClient = new WebViewRpcClient(_bridge);
            _client = new HelloServiceClient(_rpcClient);
            
            // On Button Click, run the bidirectional chunking test
            image.onClick.AddListener(RunBidirectionalChunkingTest);
            // Set button color to blue initially
            image.GetComponent<Image>().color = Color.blue;
            
            text.text = "Click to send another request"; // Reset UI text
        }

        private async void RunBidirectionalChunkingTest()
        {
            Debug.Log("--- [C# Client] Starting Bidirectional Chunking Test ---");
            Debug.Log("[C# Client] Waiting for WebView server to be ready...");
            
            _startTime = Time.time; // Record start time
            
            // Create a very long message for chunking test
            var sb = new StringBuilder();
            for (int i = 0; i < 60; i++)
            {
                sb.Append(new string((char)('X' + i), 1024));
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
                
                // Check if response has error or data
                if (response.ResultCase == HelloResponse.ResultOneofCase.Error)
                {
                    Debug.LogError($"[C# Client] Error Response: Code={response.Error.Code}, Message='{response.Error.Message}'");
                }
                else if (response.ResultCase == HelloResponse.ResultOneofCase.Data)
                {
                    Debug.Log($"[C# Client] Success Response:");
                    Debug.Log($"  Greeting: '{response.Data.Greeting}'");
                    Debug.Log($"  EchoedMessage Length: {response.Data.EchoedMessage.Length}");
                    Debug.Log($"  ProcessedAt: '{response.Data.ProcessedAt}'");
                    Debug.Log($"  OriginalSize: {response.Data.OriginalMessageSize}");
                }
                else
                {
                    Debug.LogWarning("[C# Client] Unknown response format");
                }
                Debug.Log("------------------------------------");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[C# Client] Exception: {ex.Message}");
            }
        }

        private async void RunErrorTest()
        {
            Debug.Log("--- [C# Client] Sending Error Test Request ---");

            var request = new HelloRequest
            {
                Name = "error", // This will trigger error response
                LongMessage = "test message",
                RepeatCount = 1
            };
            
            Debug.Log($"[C# Client] Request: Name='{request.Name}'");

            try
            {
                var response = await _client.SayHello(request);

                Debug.Log("--- [C# Client] Received Hello Response ---");
                
                // Check if response has error or data
                if (response.ResultCase == HelloResponse.ResultOneofCase.Error)
                {
                    Debug.LogError($"[C# Client] Error Response: Code={response.Error.Code}, Message='{response.Error.Message}'");
                }
                else if (response.ResultCase == HelloResponse.ResultOneofCase.Data)
                {
                    Debug.Log($"[C# Client] Success Response: Greeting='{response.Data.Greeting}'");
                }
                Debug.Log("------------------------------------");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[C# Client] Exception: {ex.Message}");
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
            // if space key is pressed, send a normal message to the web view
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Space key pressed, sending message to web view.");
                RunBidirectionalChunkingTest();
            }
            
            // if E key is pressed, send an error test message
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("E key pressed, sending error test message to web view.");
                RunErrorTest();
            }
        }
        
        private void OnDestroy()
        {
            Debug.Log("[WebViewRpcTester] OnDestroy - Cleaning up RPC components");
            
            // WebView가 이미 파괴되었을 수 있으므로, 먼저 Bridge를 dispose
            _bridge?.Dispose();
            _bridge = null;
            
            // 그 다음 RPC 컴포넌트들을 정리
            _client = null;
            
            _rpcClient?.Dispose();
            _rpcClient = null;
            
            _server?.Dispose();
            _server = null;
            
            Debug.Log("[WebViewRpcTester] OnDestroy - Cleanup complete");
        }
    }
}
