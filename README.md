[English](README.md) | [Korean](README_ko.md)

[![openupm](https://img.shields.io/npm/v/com.kwanjoong.webviewrpc?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.kwanjoong.webviewrpc/)
[![License](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE.md)

# Unity WebView RPC

Unity WebView RPC provides an abstraction layer that allows communication between the Unity client (C#) and WebView (HTML, JS) using protobuf, similar to gRPC.
It extends the traditional `JavaScript bridge` communication method to work similarly to a Remote Procedure Call (RPC).
To avoid dependency on a specific WebView library, it provides a Bridge interface so that communication can be achieved with the same code, regardless of the WebView library used.

## What's New in v2.0

WebView RPC v2.0 introduces significant improvements for robustness, performance, and memory management.

### Key Changes
- **Unique Request IDs (Breaking Change)**: `RequestId` now uses UUIDs instead of incremental numbers, preventing collisions when using multiple WebViews.
- **Smarter Chunking (Breaking Change)**: The `maxChunkSize` setting now accurately represents the final Base64-encoded message size, including all overhead. New utility methods like `getEffectivePayloadSize()` help you configure chunking precisely.
- **Improved Timeout Handling**: Fixed a critical bug where requests could wait indefinitely. Incomplete messages now correctly time out.
- **Resource Management**: Added `dispose()` methods to RPC clients, servers, and bridges to prevent memory leaks from event listeners.

For a complete list of changes, see the [CHANGELOG.md](webview_rpc_runtime_library/CHANGELOG.md) for the `app-webview-rpc` library.

## Architecture

WebView RPC simplifies the workflow compared to the traditional `JavaScript bridge` method.

```mermaid
flowchart LR
    subgraph Traditional Method
        direction LR
        A[Unity C#] <--> B[Data Class<br>& Manual Method Implementation]
        B <--> C[JSON Parser]
        C <--> D[JavaScript Bridge]
        D <--> E[JSON Parser]
        E <--> F[Data Class<br>& Manual Method Implementation]
        F <--> G[JavaScript WebView]
    end

    subgraph WebView RPC Method
        direction LR
        H[Unity C#<br>Direct Method Call] <--> I[Magic Space]
        I <--> J[JavaScript<br>Direct Method Call]
    end
```

### Internal Implementation

Internally, WebView RPC is structured as follows:

```mermaid
flowchart LR
    A[Unity C# Direct Call] <--> B[protobuf-generated C# Code] <--> C[protobuf Serializer/Deserializer] <--> D[Base64 Serializer/Deserializer] <--> E[JavaScript Bridge] <--> F[Base64 Serializer/Deserializer] <--> G[protobuf Serializer/Deserializer] <--> H[protobuf-generated JavaScript Code] <--> I[JavaScript Direct Call]
```

1. **Unity C# Direct Call**
    - Calls an RPC interface function like a regular method in Unity.
2. **protobuf-generated C# Code**
    - Auto-generated C# wrapper/stub from the proto definition.
    - RPC methods and data structures are based on protobuf.
3. **Base64 Serializer + JavaScript Bridge**
    - Converts raw byte data to Base64 before sending it through the WebView (browser).
    - JavaScript receives the same format.
4. **protobuf-generated JavaScript Code**
    - Auto-generated JavaScript code from the same proto definition.
    - Deserializes the serialized data from C# and directly calls JavaScript methods.

With WebView RPC, method calls between C# and JavaScript behave like regular function calls, significantly reducing the need for repetitive JSON parsing and bridge implementations. This structure becomes even more maintainable as the project scales.

## Examples
### Unity Example
Clone this whole repository.
> [!NOTE]
> Require [Viewplex Webview Asset(paid asset)](https://assetstore.unity.com/publishers/40309?locale=ko-KR&srsltid=AfmBOoqtnjTpJ-pw_5iGoS88XRtGX-tY2eJmP86PYoYCOhxrvz1OXRaJ) to run sample.
### Javascript Example
[webview_rpc_sample](https://github.com/kwan3854/Unity-WebViewRpc/tree/main/webview_rpc_sample)
```bash
# 1. Move to sample directory
cd webview_rpc_sample

# 2. Install dependencies
npm install

# 3. Build project
npm run build
```

## Installation

### Adding WebView RPC to a Unity Project

1. Install the `Protobuf` package via NuGet Package Manager.
2. Install the WebViewRpc package either via Package Manager or OpenUPM.

- Add to `Packages/manifest.json`:

  ```json
  {
   "dependencies": {
     "com.kwanjoong.webviewrpc": "https://github.com/kwan3854/Unity-WebViewRpc.git?path=/Packages/WebviewRpc"
   }
  }
  ```

- Or via Package Manager:

    1. `Window` → `Package Manager` → `Add package from git URL...`
    2. Enter: `https://github.com/kwan3854/Unity-WebViewRpc.git?path=/Packages/WebviewRpc`

- Or via OpenUPM:

  ```bash
  openupm add com.kwanjoong.webviewrpc
  ```

### Adding `app-webview-rpc` Library to Javascript Side
[npm package](https://www.npmjs.com/package/app-webview-rpc)
#### Install
```bash
npm install app-webview-rpc@2.0.11
```

#### Usage
```javascript
import { VuplexBridge, WebViewRpcClient, WebViewRpcServer } from 'app-webview-rpc';

// RPC client
const bridge = new VuplexBridge();
const rpcClient = new WebViewRpcClient(bridge);

// RPC server
const rpcServer = new WebViewRpcServer(bridge);
```

### Installing the protobuf Compiler

#### Convert protobuf files to C# and JavaScript using `protoc`.

**Mac**

```bash
brew install protobuf
protoc --version  # Ensure compiler version is 3+
```

**Windows**

```bash
winget install protobuf
protoc --version  # Ensure compiler version is 3+
```

**Linux**

```bash
apt install -y protobuf-compiler
protoc --version  # Ensure compiler version is 3+
```

### Installing the WebView RPC Code Generator

Download the latest release from the [WebViewRPC Code Generator repository](https://github.com/kwan3854/ProtocGenWebviewRpc).

- **Windows**: `protoc-gen-webviewrpc.exe`
- **Mac**: `protoc-gen-webviewrpc`
- **Linux**: Not provided (requires manual build).

## Chunking for Large Messages

To handle environments with message size limitations, such as certain Android WebViews where the JavaScript bridge limit is around 1KB, WebView RPC includes a chunking feature. This allows large messages to be split into smaller chunks and reassembled on the receiving end.

### How It Works

When chunking is enabled, any message exceeding `MaxChunkSize` is automatically divided into smaller parts. Each chunk is sent individually and then reassembled by the receiver. This process is handled transparently by the library.

The chunking system automatically accounts for:
- **RPC Envelope Overhead**: Approximately 150 bytes for metadata (requestId, method, chunkInfo)
- **Base64 Encoding Overhead**: About 34% increase in size
- **Effective Payload Size**: Actual usable data size after accounting for all overheads

### Configuration Options

WebView RPC provides several configuration options through `WebViewRpcConfiguration`:

#### 1. Enable/Disable Chunking

Control whether chunking is active. When disabled, large messages will be sent as-is, which may fail in environments with size limitations.

**Unity (C#)**
```csharp
WebViewRpcConfiguration.EnableChunking = true;  // Default: true
```

**JavaScript**
```javascript
WebViewRpcConfiguration.enableChunking = true;  // Default: true
```

#### 2. Maximum Chunk Size

Sets the maximum size of the final Base64-encoded message. The library automatically calculates the effective payload size after accounting for encoding overhead.

**Unity (C#)**
```csharp
// Set max chunk size to 900 bytes (recommended for 1KB limit environments)
WebViewRpcConfiguration.MaxChunkSize = 900;  // Default: 256KB
// Minimum: ~335 bytes (dynamically calculated)
// Maximum: 10MB
```

**JavaScript**
```javascript
// Set max chunk size to 900 bytes
WebViewRpcConfiguration.maxChunkSize = 900;  // Default: 256KB
```

#### 3. Chunk Timeout

Configure how long to wait for incomplete chunk sets before cleaning them up.

**Unity (C#)**
```csharp
WebViewRpcConfiguration.ChunkTimeoutSeconds = 30;  // Default: 30 seconds
// Minimum: 5 seconds
// Maximum: 300 seconds (5 minutes)
```

**JavaScript**
```javascript
WebViewRpcConfiguration.chunkTimeoutSeconds = 30;  // Default: 30 seconds
```

#### 4. Maximum Concurrent Chunk Sets

Limit the number of simultaneous chunked messages to prevent memory exhaustion.

**Unity (C#)**
```csharp
WebViewRpcConfiguration.MaxConcurrentChunkSets = 100;  // Default: 100
// Minimum: 10
// Maximum: 1000
```

**JavaScript**
```javascript
WebViewRpcConfiguration.maxConcurrentChunkSets = 100;  // Default: 100
```

### Utility Methods

WebView RPC provides utility methods to help you understand and optimize chunk sizing:

**Unity (C#)**
```csharp
// Get the actual payload size available after overhead
int effectiveSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
Console.WriteLine($"Effective payload size: {effectiveSize} bytes");

// Get the minimum safe chunk size
int minSize = WebViewRpcConfiguration.GetMinimumSafeChunkSize();
Console.WriteLine($"Minimum chunk size: {minSize} bytes");

// Validate current configuration
bool isValid = WebViewRpcConfiguration.IsChunkSizeValid();
if (!isValid)
{
    Console.WriteLine("Warning: Chunk size too small for meaningful payload");
}
```

**JavaScript**
```javascript
// Get the actual payload size available after overhead
const effectiveSize = WebViewRpcConfiguration.getEffectivePayloadSize();
console.log(`Effective payload size: ${effectiveSize} bytes`);

// Get the minimum safe chunk size
const minSize = WebViewRpcConfiguration.getMinimumSafeChunkSize();
console.log(`Minimum chunk size: ${minSize} bytes`);

// Validate current configuration
const isValid = WebViewRpcConfiguration.isChunkSizeValid();
if (!isValid) {
    console.warn("Chunk size too small for meaningful payload");
}
```

### Best Practices

1. **Size Configuration**: For Android WebViews with 1KB limits, use `MaxChunkSize = 900` to safely stay under the limit
2. **Consistent Settings**: Ensure both Unity and JavaScript use identical configuration values
3. **Monitor Performance**: If you see warnings about small effective payload sizes, consider increasing `MaxChunkSize`
4. **Timeout Tuning**: Adjust `ChunkTimeoutSeconds` based on your network conditions and message sizes
5. **Testing**: Test with your largest expected messages to ensure chunking works smoothly

### Example: Complete Configuration

#### Scenario 1: Identical Settings (Recommended)
Using the same configuration on both sides for bidirectional consistency:

**Unity (C#)**
```csharp
void Start()
{
    // Match the most restrictive environment (Android WebView 1KB)
    WebViewRpcConfiguration.EnableChunking = true;
    WebViewRpcConfiguration.MaxChunkSize = 900;
    WebViewRpcConfiguration.ChunkTimeoutSeconds = 60;
    WebViewRpcConfiguration.MaxConcurrentChunkSets = 50;
    
    // Verify configuration
    int effectiveSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
    Debug.Log($"Sending with effective payload: {effectiveSize} bytes per chunk");
    
    // Initialize RPC
    var bridge = new ViewplexWebViewBridge(webViewPrefab);
    var server = new WebViewRPC.WebViewRpcServer(bridge);
}
```

**JavaScript**
```javascript
// Match Unity settings for bidirectional consistency
WebViewRpcConfiguration.enableChunking = true;
WebViewRpcConfiguration.maxChunkSize = 900;
WebViewRpcConfiguration.chunkTimeoutSeconds = 60;
WebViewRpcConfiguration.maxConcurrentChunkSets = 50;

const effectiveSize = WebViewRpcConfiguration.getEffectivePayloadSize();
console.log(`Sending with effective payload: ${effectiveSize} bytes per chunk`);

const bridge = new VuplexBridge();
const rpcServer = new WebViewRpcServer(bridge);
```

#### Scenario 2: Different Chunk Sizes
Using different chunk sizes within the same platform constraints:

**Android WebView Environment Example (1KB limit both directions)**
```csharp
// Unity (C#)
void Start()
{
    WebViewRpcConfiguration.EnableChunking = true;
    WebViewRpcConfiguration.MaxChunkSize = 990;  // Unity uses 990-byte chunks
    WebViewRpcConfiguration.ChunkTimeoutSeconds = 60;
    WebViewRpcConfiguration.MaxConcurrentChunkSets = 50;
    
    Debug.Log("Unity: Sending with 990-byte chunks, can receive any size");
}
```

```javascript
// JavaScript
WebViewRpcConfiguration.enableChunking = true;
WebViewRpcConfiguration.maxChunkSize = 800;  // JS uses 800-byte chunks
WebViewRpcConfiguration.chunkTimeoutSeconds = 60;
WebViewRpcConfiguration.maxConcurrentChunkSets = 50;

console.log("JS: Sending with 800-byte chunks, can receive any size");
```

**Regular Browser Environment Example (large messages supported)**
```csharp
// Unity (C#)
WebViewRpcConfiguration.MaxChunkSize = 1024 * 1024;  // 1MB chunks
```

```javascript
// JavaScript  
WebViewRpcConfiguration.maxChunkSize = 256 * 1024;  // 256KB chunks
```

> While each side can use different chunk sizes under the same platform constraints, there's little practical benefit to doing so.

> [!NOTE]
> **Chunking Configuration Independence and Platform Constraints**
> 
> **Platform/bridge constraints apply equally to both directions**:
> - Android WebView environment: Unity ↔ JavaScript both limited to ~1KB
> - iOS WKWebView environment: Unity ↔ JavaScript both support larger messages
> - Regular browser environment: Most support large messages
> 
> **Technical Flexibility**:
> - **Sender**: Can set MaxChunkSize freely within platform constraints
> - **Receiver**: Can receive and reassemble chunks of any size (MaxChunkSize doesn't affect reception)
> 
> **Example - Android WebView Environment (1KB limit)**:
> - Unity side: Sets MaxChunkSize = 990 bytes for sending
> - JavaScript side: Sets MaxChunkSize = 800 bytes for sending
> - Both sides receive and reassemble chunks from each other without issues
> 
> **Recommendations**:
> 1. **Required**: Identify your platform/bridge message size limitations
> 2. **Optional Unification**: Use identical settings for easier maintenance and debugging
> 3. **Receiver Settings**: ChunkTimeoutSeconds and MaxConcurrentChunkSets affect receiver behavior

## Quick Start

HelloWorld is a simple RPC service that receives a `HelloRequest` message and returns a `HelloResponse` message. In this example, we will implement HelloWorld and verify communication between the Unity client and the WebView client.

The HelloWorld service takes a `HelloRequest` and returns a `HelloResponse`. First, let's look at the example where the C# side acts as the server and the JavaScript side acts as the client.

### Defining the protobuf File

- protobuf is used to define the request and response formats of the service.
- When the Unity client and the WebView have items to communicate, define the protobuf through discussion.
- The following example is the `HelloWorld.proto` file, defining `HelloRequest`, `HelloResponse`, and the `HelloService` service.
- In this example, the client side (JavaScript) calls the `SayHello` method, and the server side (C#) implements the `SayHello` method to process the request and return a response.

```protobuf
syntax = "proto3";

package helloworld;

// (Can be used as the namespace when generated in C#)
option csharp_namespace = "HelloWorld";

// Request message
message HelloRequest {
  string name = 1;
}

// Response message
message HelloResponse {
  string greeting = 1;
}

// Simple example service
service HelloService {
  // [one-way] Request -> Response
  rpc SayHello (HelloRequest) returns (HelloResponse);
}
```

### Generating C# and JavaScript from protobuf

- We use the `protoc` compiler to convert the protobuf file into C# and JavaScript.
- The `protoc` compiler transforms protobuf files into C# and JavaScript.
- A [customized code generator](https://github.com/kwan3854/ProtocGenWebviewRpc) for WebView RPC is also available.
- Run the following commands to generate C# and JavaScript code from the protobuf file.

#### C# Common Code Generation (used by both server and client)

```bash
protoc -I. --csharp_out=. HelloWorld.proto 

// This produces HelloWorld.cs.
```

#### C# Server Code Generation

```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=cs_server:. \
  -I. HelloWorld.proto
  
// This produces HelloWorld_HelloServiceBase.cs.
```

#### JavaScript Common Code Generation (for both client and server)

> [!IMPORTANT]
> [pbjs library is required.](https://www.npmjs.com/package/pbjs)

```bash
npx pbjs HelloWorld.proto --es6 hello_world.js

// This produces hello_world.js.
// Recommend setting the output filename to the same name as the service defined in the protobuf file.
```

#### JavaScript Client Code Generation

```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=js_client:. \
  -I. HelloWorld.proto
  
// This produces HelloWorld_HelloServiceClient.js.
```

### Adding the Generated Code to Each Project

- Add the generated code to each respective project.
- You can use a GitHub action so that code is automatically generated and added to your project.

### Implementing Bridge Code

- The bridge code mediates communication between C# and JavaScript.
- WebViewRpc is abstracted so it can be used with any WebView library.
- Implement the bridge code according to your chosen WebView library.
- Below is an example using Viewplex's CanvasWebViewPrefab.

```csharp
using System;
using Vuplex.WebView;
using WebViewRPC;

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
```

```javascript
export class VuplexBridge {
    constructor() {
        this._onMessageCallback = null;
        this._isVuplexReady = false;
        this._pendingMessages = [];

        // 1) If window.vuplex already exists, use it immediately
        if (window.vuplex) {
            this._isVuplexReady = true;
        } else {
            // Otherwise, wait for the 'vuplexready' event
            window.addEventListener('vuplexready', () => {
                this._isVuplexReady = true;
                // Send all pending messages
                for (const msg of this._pendingMessages) {
                    window.vuplex.postMessage(msg);
                }
                this._pendingMessages = [];
            });
        }

        // 2) C# -> JS messages: "vuplexmessage" event
        //    event.value contains the string (sent by C# PostMessage)
        window.addEventListener('vuplexmessage', event => {
            const base64Str = event.value; // Typically Base64
            if (this._onMessageCallback) {
                this._onMessageCallback(base64Str);
            }
        });
    }

    /**
     * JS -> C#: sends string (base64Str)
     */
    sendMessage(base64Str) {
        // Vuplex serializes JS objects to JSON,
        // but if we pass a string, it sends the string as is.
        if (this._isVuplexReady && window.vuplex) {
            window.vuplex.postMessage(base64Str);
        } else {
            // If vuplex isn't ready yet, store messages in a queue
            this._pendingMessages.push(base64Str);
        }
    }

    /**
     * onMessage(cb): registers a callback to receive strings from C#
     */
    onMessage(cb) {
        this._onMessageCallback = cb;
    }
}
```

### Writing C# Server and JavaScript Client Code

```csharp
public class WebViewRpcTester : MonoBehaviour
{
    [SerializeField] private CanvasWebViewPrefab webViewPrefab;

    private async void Start()
    {
        await InitializeWebView(webViewPrefab);

        // Create the bridge
        var bridge = new ViewplexWebViewBridge(webViewPrefab);
        // Create the server
        var server = new WebViewRPC.WebViewRpcServer(bridge)
        {
            Services =
            {
                // Bind HelloService
                HelloService.BindService(new HelloWorldService()),
                // Add other services if necessary
            }
        };

        // Start the server
        server.Start();
    }

    private async Task InitializeWebView(CanvasWebViewPrefab webView)
    {
        // Example uses Viewplex's CanvasWebViewPrefab
        await webView.WaitUntilInitialized();
        webView.WebView.LoadUrl("http://localhost:8081");
        await webView.WebView.WaitForNextPageLoadToFinish();
    }
}
```

```csharp
using Cysharp.Threading.Tasks;
using HelloWorld;
using UnityEngine;

namespace SampleRpc
{
    // Inherit HelloServiceBase and implement the SayHello method.
    // HelloServiceBase is generated from HelloWorld.proto.
    public class HelloWorldService : HelloServiceBase
    {
        public override async UniTask<HelloResponse> SayHello(HelloRequest request)
        {
            Debug.Log($"Received request: {request.Name}");
            
            // Example async operation
            await UniTask.Delay(100);
            
            return new HelloResponse()
            {
                // Process the request and return a response
                Greeting = $"Hello, {request.Name}!"
            };
        }
    }
}
```

```javascript
// 1) Create a bridge
const bridge = new VuplexBridge();
// 2) Create an RpcClient
const rpcClient = new WebViewRpcClient(bridge);
// 3) Create a HelloServiceClient
const helloClient = new HelloServiceClient(rpcClient);

document.getElementById('btnSayHello').addEventListener('click', async () => {
    try {
        const reqObj = { name: "Hello World! From WebView" };
        console.log("Request to Unity: ", reqObj);

        const resp = await helloClient.SayHello(reqObj);
        console.log("Response from Unity: ", resp.greeting);
    } catch (err) {
        console.error("Error: ", err);
    }
});
```

### Running the Example

- Run the `WebViewRpcTester` script in Unity, and open the WebView.
- When you click the button in the WebView, Unity processes the request via `HelloService` and returns a response.

### The Opposite Case (C# Client, JavaScript Server)

- The reverse scenario can be implemented in the same way.
- Since the common code is already generated, generate C# client code and JavaScript server code.

#### Generating C# Client Code

```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=cs_client:. \
  -I. HelloWorld.proto
```

#### Generating JavaScript Server Code

```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=js_server:. \
  -I. HelloWorld.proto
```

#### Writing C# Client Code

```csharp
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

        // Create the bridge
        var bridge = new ViewplexWebViewBridge(webViewPrefab);
        // Create an RpcClient
        var rpcClient = new WebViewRPC.WebViewRpcClient(bridge);
        // Create a HelloServiceClient
        var client = new HelloServiceClient(rpcClient);

        // Send a request
        var response = await client.SayHello(new HelloRequest()
        {
            Name = "World"
        });

        // Check the response
        Debug.Log($"Received response: {response.Greeting}");
    }

    private async Task InitializeWebView(CanvasWebViewPrefab webView)
    {
        await webView.WaitUntilInitialized();
        webView.WebView.LoadUrl("http://localhost:8081");
        await webView.WebView.WaitForNextPageLoadToFinish();
    }
}
```

#### Writing JavaScript Server Code

```javascript
// 1) Create a bridge
const bridge = new VuplexBridge();
// 2) Create an RpcServer
const rpcServer = new WebViewRpcServer(bridge);
// 3) Create a service implementation
const impl = new MyHelloServiceImpl();
// 4) Bind the service
const def = HelloService.bindService(impl);
// 5) Register the service
rpcServer.services.push(def);
// 6) Start the server
rpcServer.start();
```

```javascript
import { HelloServiceBase } from "./HelloWorld_HelloServiceBase.js";

// Inherit HelloServiceBase from the auto-generated HelloWorld_HelloServiceBase.js
export class MyHelloServiceImpl extends HelloServiceBase {
    async SayHello(requestObj) {
        // Check the incoming request
        console.log("JS Server received: ", requestObj);
        
        // Example async operation
        await new Promise(resolve => setTimeout(resolve, 100));

        // Process the request and return a response
        return {
            greeting: "Hello from JS! I got your message: " + requestObj.name
        };
    }
}
```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.