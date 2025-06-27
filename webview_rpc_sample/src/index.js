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
            log(`Response from Unity:`);
            log(`  Greeting: ${resp.greeting}`);
            log(`  Echoed Message Length: ${resp.echoedMessage.length}`);
            log(`  Processed At: ${resp.processedAt}`);
            log(`  Original Message Size: ${resp.originalMessageSize}`);
            log("--------------------------");

        } catch (err) {
            log(`Error: ${err.message}`);
            console.error("Error: ", err);
        }
    });
});