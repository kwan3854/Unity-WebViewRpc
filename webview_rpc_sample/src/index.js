import { VuplexBridge, WebViewRpcClient, WebViewRpcServer, WebViewRpcConfiguration } from 'app-webview-rpc';
import { HelloService } from './HelloWorld_HelloServiceBase.js';
import { HelloServiceClient } from './HelloWorld_HelloServiceClient.js';
import { MyHelloServiceImpl } from './MyHelloServiceImpl.js';

// DOM이 로드된 후 실행
document.addEventListener('DOMContentLoaded', () => {
    // 1) 브리지 생성
    const bridge = new VuplexBridge();

    // 청킹 설정 (테스트를 위해 작은 값으로 설정)
    WebViewRpcConfiguration.maxChunkSize = 900;
    WebViewRpcConfiguration.enableChunking = true;
    
    // 2) RpcClient 생성
    const rpcClient = new WebViewRpcClient(bridge);
    // 3) HelloServiceClient
    const helloClient = new HelloServiceClient(rpcClient);

    // 1) RpcServer 생성
    const rpcServer = new WebViewRpcServer(bridge);
    // 2) Bind your service
    const impl = new MyHelloServiceImpl();
    // 3) Generate Method Handlers
    const def = HelloService.bindService(impl);

    rpcServer.addService(def);
    rpcServer.start();

    const logsEl = document.getElementById('logs');
    const log = (message) => {
        const p = document.createElement('p');
        p.textContent = message;
        logsEl.appendChild(p);
        console.log(message);
    };

    document.getElementById('btnSayHello').addEventListener('click', async () => {
        try {
            log("--- Sending Hello Request ---");
            
            // Create a very long message for chunking test
            const longMessage = "A".repeat(2000) + "B".repeat(2000) + "C".repeat(2000);
            
            const reqObj = {
                name: "Chunking Test from WebView",
                longMessage: longMessage,
                repeatCount: 1
            };
            
            log(`Request to Unity: { name: "${reqObj.name}", longMessage length: ${reqObj.longMessage.length} }`);
            
            const resp = await helloClient.SayHello(reqObj);
            
            log("--- Received Hello Response ---");
            
            // Check if response is error or data
            if (resp.error) {
                log(`Error Response from Unity:`);
                log(`  Code: ${resp.error.code}`);
                log(`  Message: ${resp.error.message}`);
            } else if (resp.data) {
                log(`Success Response from Unity:`);
                log(`  Greeting: ${resp.data.greeting}`);
                log(`  Echoed Message Length: ${resp.data.echoedMessage.length}`);
                log(`  Processed At: ${resp.data.processedAt}`);
                log(`  Original Message Size: ${resp.data.originalMessageSize}`);
            } else {
                log(`Unknown response format`);
            }
            log("--------------------------");

        } catch (err) {
            log(`Error: ${err.message}`);
            console.error("Error: ", err);
        }
    });

    // Test error response button
    const errorBtn = document.createElement('button');
    errorBtn.textContent = 'Test Error Response';
    errorBtn.addEventListener('click', async () => {
        try {
            log("--- Sending Error Test Request ---");
            
            const reqObj = {
                name: "error", // This will trigger error response
                longMessage: "test",
                repeatCount: 1
            };
            
            log(`Request to Unity: { name: "${reqObj.name}" }`);
            
            const resp = await helloClient.SayHello(reqObj);
            
            log("--- Received Hello Response ---");
            
            // Check if response is error or data
            if (resp.error) {
                log(`Error Response from Unity:`);
                log(`  Code: ${resp.error.code}`);
                log(`  Message: ${resp.error.message}`);
            } else if (resp.data) {
                log(`Success Response from Unity:`);
                log(`  Greeting: ${resp.data.greeting}`);
            }
            log("--------------------------");

        } catch (err) {
            log(`Error: ${err.message}`);
            console.error("Error: ", err);
        }
    });
    
    // Add error test button to the page
    document.querySelector('body').insertBefore(errorBtn, document.getElementById('logs'));
});