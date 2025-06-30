import { VuplexBridge, WebViewRpcClient, WebViewRpcServer, WebViewRpcConfiguration } from 'app-webview-rpc';
import { HelloService } from './HelloWorld_HelloServiceBase.js';
import { HelloServiceClient } from './HelloWorld_HelloServiceClient.js';
import { MyHelloServiceImpl } from './MyHelloServiceImpl.js';

// Global references for cleanup
let bridge, rpcClient, rpcServer;

// DOM이 로드된 후 실행
document.addEventListener('DOMContentLoaded', () => {
    // 1) 브리지 생성
    bridge = new VuplexBridge();

    // 청킹 설정 (테스트를 위해 작은 값으로 설정)
    try {
        WebViewRpcConfiguration.maxChunkSize = 900; // 900 bytes for testing
        WebViewRpcConfiguration.enableChunking = true;
        WebViewRpcConfiguration.chunkTimeoutSeconds = 10; // 10 seconds for testing
        
        console.log('[Configuration] maxChunkSize:', WebViewRpcConfiguration.maxChunkSize, 'bytes');
        console.log('[Configuration] enableChunking:', WebViewRpcConfiguration.enableChunking);
        console.log('[Configuration] chunkTimeoutSeconds:', WebViewRpcConfiguration.chunkTimeoutSeconds, 'seconds');
        console.log('[Configuration] effectivePayloadSize:', WebViewRpcConfiguration.getEffectivePayloadSize(), 'bytes');
        console.log('[Configuration] minimumSafeChunkSize:', WebViewRpcConfiguration.getMinimumSafeChunkSize(), 'bytes');
        console.log('[Configuration] isChunkSizeValid:', WebViewRpcConfiguration.isChunkSizeValid());
    } catch (error) {
        console.error('[Configuration Error]', error.message);
        // Use default values
        WebViewRpcConfiguration.maxChunkSize = 256 * 1024;
    }
    
    // 2) RpcClient 생성
    rpcClient = new WebViewRpcClient(bridge);
    // 3) HelloServiceClient
    const helloClient = new HelloServiceClient(rpcClient);

    // 1) RpcServer 생성
    rpcServer = new WebViewRpcServer(bridge);
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
    
    // Test configuration validation button
    const configTestBtn = document.createElement('button');
    configTestBtn.textContent = 'Test Configuration Validation';
    configTestBtn.addEventListener('click', () => {
        log("--- Testing Configuration Validation ---");
        
        // Test 1: Too small chunk size
        const minSize = WebViewRpcConfiguration.getMinimumSafeChunkSize();
        try {
            WebViewRpcConfiguration.maxChunkSize = minSize - 50; // Should fail
            log(`ERROR: Setting maxChunkSize to ${minSize - 50} bytes should have failed!`);
        } catch (error) {
            log(`✓ Correctly rejected small chunk size: ${error.message}`);
        }
        
        // Test 2: Too large chunk size
        try {
            WebViewRpcConfiguration.maxChunkSize = 11 * 1024 * 1024; // Should fail (> 10MB)
            log("ERROR: Setting maxChunkSize to 11MB should have failed!");
        } catch (error) {
            log(`✓ Correctly rejected large chunk size: ${error.message}`);
        }
        
        // Test 3: Too small timeout
        try {
            WebViewRpcConfiguration.chunkTimeoutSeconds = 3; // Should fail (< 5 seconds)
            log("ERROR: Setting chunkTimeoutSeconds to 3 should have failed!");
        } catch (error) {
            log(`✓ Correctly rejected small timeout: ${error.message}`);
        }
        
        // Test 4: Too large timeout
        try {
            WebViewRpcConfiguration.chunkTimeoutSeconds = 400; // Should fail (> 300 seconds)
            log("ERROR: Setting chunkTimeoutSeconds to 400 should have failed!");
        } catch (error) {
            log(`✓ Correctly rejected large timeout: ${error.message}`);
        }
        
        // Restore valid configuration
        WebViewRpcConfiguration.maxChunkSize = 900;
        WebViewRpcConfiguration.chunkTimeoutSeconds = 10;
        log("Configuration restored to valid values");
        
        // Show actual effective sizes
        log(`\nWith maxChunkSize=900 bytes:`);
        log(`  - Effective payload size: ${WebViewRpcConfiguration.getEffectivePayloadSize()} bytes`);
        log(`  - Envelope overhead: ~${WebViewRpcConfiguration.estimatedEnvelopeOverhead} bytes`);
        log(`  - Base64 overhead: ${Math.round((WebViewRpcConfiguration.base64OverheadRatio - 1) * 100)}%`);
        log(`  - This means actual payload will be chunked at ${WebViewRpcConfiguration.getEffectivePayloadSize()} bytes`);
        log("--------------------------");
    });
    
    // Add config test button to the page
    document.querySelector('body').insertBefore(configTestBtn, document.getElementById('logs'));
    
    // Add dispose test button
    const disposeBtn = document.createElement('button');
    disposeBtn.textContent = 'Test Dispose';
    disposeBtn.addEventListener('click', () => {
        log("--- Testing Dispose Functionality ---");
        
        // Dispose all resources
        if (rpcClient) {
            rpcClient.dispose();
            rpcClient = null;
            log("✓ RpcClient disposed");
        }
        
        if (rpcServer) {
            rpcServer.dispose();
            rpcServer = null;
            log("✓ RpcServer disposed");
        }
        
        // Note: Bridge is disposed by client/server, so we don't need to dispose it separately
        bridge = null;
        
        log("All resources cleaned up. Further RPC calls will fail.");
        log("--------------------------");
    });
    
    // Add dispose test button to the page
    document.querySelector('body').insertBefore(disposeBtn, document.getElementById('logs'));
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    console.log('Page unloading - cleaning up resources...');
    
    // Dispose RPC resources
    if (rpcClient) {
        rpcClient.dispose();
    }
    
    if (rpcServer) {
        rpcServer.dispose();
    }
});

// Also cleanup on visibility change (mobile browser background)
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        console.log('Page hidden - consider cleanup if needed');
        // Note: We don't dispose here because the page might become visible again
        // But you could implement a timeout-based cleanup if needed
    }
});