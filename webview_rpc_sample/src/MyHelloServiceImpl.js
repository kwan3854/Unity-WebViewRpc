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
            longMessageLength: requestObj.longMessage?.length || 0,
            repeatCount: requestObj.repeatCount
        });

        // Simulate some async work
        await new Promise(resolve => setTimeout(resolve, 50));

        // Example: Return error for certain names
        if (requestObj.name === 'error') {
            const errorResponse = {
                error: {
                    code: 400,
                    message: "Invalid name: 'error' is not allowed"
                }
            };
            
            console.log("JS Server sending error response:", errorResponse);
            return errorResponse;
        }

        // Normal response
        const response = {
            data: {
                greeting: `Hello from JS, ${requestObj.name}!`,
                echoedMessage: requestObj.longMessage || '', // Echo back for bidirectional chunking test
                processedAt: new Date().toISOString(),
                originalMessageSize: requestObj.longMessage?.length || 0
            }
        };
        
        console.log("JS Server sending response:", {
            greeting: response.data.greeting,
            echoedMessageLength: response.data.echoedMessage.length,
            processedAt: response.data.processedAt
        });
        
        return response;
    }
}
