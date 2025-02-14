import { VuplexBridge, WebViewRpcClient, WebViewRpcServer } from 'app-webview-rpc';
import { HelloService } from './HelloWorld_HelloServiceBase.js';
import { HelloServiceClient } from './HelloWorld_HelloServiceClient.js';
import { MyHelloServiceImpl } from './MyHelloServiceImpl.js';

// DOM이 로드된 후 실행
document.addEventListener('DOMContentLoaded', () => {
    // 1) 브리지 생성
    const bridge = new VuplexBridge();

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

    rpcServer.services.push(def);
    rpcServer.start();

    const logsEl = document.getElementById('logs');

    document.getElementById('btnSayHello').addEventListener('click', async () => {
        try {
            const reqObj = {name: "Hello World! From WebView"};
            console.log("Request to Unity: ", reqObj);
            
            const resp = await helloClient.SayHello(reqObj);
            console.log("Response from Unity: ", resp.greeting);
        } catch (err) {
            console.error("Error: ", err);
        }
    });
});