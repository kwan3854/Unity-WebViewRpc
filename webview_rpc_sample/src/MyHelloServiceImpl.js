import { HelloServiceBase } from "./HelloWorld_HelloServiceBase.js";

/**
 * Implementation for the HelloWorld service.
 * Handles RPC calls from the Unity client.
 */
export class MyHelloServiceImpl extends HelloServiceBase {
    /**
     * @param {HelloRequest} requestObj
     * @returns {Promise<HelloResponse>}
     */
    async SayHello(requestObj) {
        console.log("JS Server received request:", {
            name: requestObj.name,
            longMessageLength: requestObj.longMessage.length,
            repeatCount: requestObj.repeatCount
        });

        // Simulate some async work
        await new Promise(resolve => setTimeout(resolve, 50));

        // Create a response
        const response = {
            greeting: `Hello from JS, ${requestObj.name}!`,
            echoedMessage: requestObj.longMessage, // Echo back for bidirectional chunking test
            processedAt: new Date().toISOString(),
            originalMessageSize: requestObj.longMessage.length
        };
        
        console.log("JS Server sending response:", {
            greeting: response.greeting,
            echoedMessageLength: response.echoedMessage.length,
            processedAt: response.processedAt
        });
        
        return response;
    }
}
