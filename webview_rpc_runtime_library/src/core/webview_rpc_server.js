import { decodeRpcEnvelope, encodeRpcEnvelope } from './rpc_envelope.js';
import { base64ToUint8Array, uint8ArrayToBase64 } from './webview_rpc_utils.js';

/**
 * WebView RPC Server
 * Handles incoming RPC requests from Unity
 */
export class WebViewRpcServer {
    constructor(bridge) {
        this._bridge = bridge;
        this._services = [];
        this._methodHandlers = {};
        
        // Listen for messages from Unity
        this._bridge.onMessage((base64Message) => {
            this._handleMessage(base64Message);
        });
    }

    /**
     * Add a service to the server
     * @param {ServiceDefinition} service 
     */
    addService(service) {
        this._services.push(service);
    }

    /**
     * Start the server and register all method handlers
     */
    start() {
        // Merge all service handlers into one map
        for (const service of this._services) {
            for (const [methodName, handler] of Object.entries(service.methodHandlers)) {
                this._methodHandlers[methodName] = handler;
            }
        }
    }

    /**
     * Handle incoming message from Unity
     * @param {string} base64Message 
     */
    async _handleMessage(base64Message) {
        try {
            const bytes = base64ToUint8Array(base64Message);
            const envelope = decodeRpcEnvelope(bytes);

            if (envelope.isRequest) {
                await this._handleRequest(envelope);
            }
        } catch (error) {
            console.error('Error handling message:', error);
        }
    }

    /**
     * Handle RPC request
     * @param {Object} requestEnvelope 
     */
    async _handleRequest(requestEnvelope) {
        const responseEnvelope = {
            requestId: requestEnvelope.requestId,
            isRequest: false,
            method: requestEnvelope.method,
            payload: null,
            error: null
        };

        try {
            const handler = this._methodHandlers[requestEnvelope.method];
            if (handler) {
                const responsePayload = await handler(requestEnvelope.payload);
                responseEnvelope.payload = responsePayload;
            } else {
                responseEnvelope.error = `Unknown method: ${requestEnvelope.method}`;
            }
        } catch (error) {
            responseEnvelope.error = error.message || 'Internal server error';
        }

        // Send response back to Unity
        const responseBytes = encodeRpcEnvelope(responseEnvelope);
        const responseBase64 = uint8ArrayToBase64(responseBytes);
        this._bridge.sendMessage(responseBase64);
    }

    /**
     * Legacy property for compatibility
     * @deprecated Use addService() instead
     */
    get services() {
        return this._services;
    }
}
