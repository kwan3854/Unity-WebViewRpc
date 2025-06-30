import { decodeRpcEnvelope, encodeRpcEnvelope } from './rpc_envelope.js';
import { base64ToUint8Array, uint8ArrayToBase64 } from './webview_rpc_utils.js';
import { WebViewRpcConfiguration } from './webview_rpc_configuration.js';
import { ChunkAssembler } from './chunk_assembler.js';

/**
 * WebView RPC Server
 * Handles incoming RPC requests from Unity
 */
export class WebViewRpcServer {
    constructor(bridge) {
        this._bridge = bridge;
        this._services = [];
        this._methodHandlers = {};
        this._chunkAssembler = new ChunkAssembler();
        
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

            // Check if this is a chunked message
            if (envelope.chunkInfo) {
                // Try to reassemble
                const completeData = this._chunkAssembler.tryAssemble(envelope);
                if (completeData) {
                    // Create a new envelope with the complete data
                    const completeEnvelope = {
                        requestId: envelope.requestId,
                        isRequest: envelope.isRequest,
                        method: envelope.method,
                        payload: completeData,
                        error: envelope.error
                    };
                    
                    if (completeEnvelope.isRequest) {
                        await this._handleRequest(completeEnvelope);
                    }
                }
                // else: waiting for more chunks
            } else {
                // Process as regular message
                if (envelope.isRequest) {
                    await this._handleRequest(envelope);
                }
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
        let responsePayload = null;
        let error = null;

        try {
            const handler = this._methodHandlers[requestEnvelope.method];
            if (handler) {
                responsePayload = await handler(requestEnvelope.payload);
            } else {
                error = `Unknown method: ${requestEnvelope.method}`;
            }
        } catch (err) {
            error = err.message || 'Internal server error';
        }

        // Send response
        if (responsePayload && !error) {
            // Check if chunking is needed
            const effectivePayloadSize = WebViewRpcConfiguration.getEffectivePayloadSize();
            if (WebViewRpcConfiguration.enableChunking && 
                responsePayload.length > effectivePayloadSize) {
                // Send as chunks
                await this._sendChunkedMessage(
                    requestEnvelope.requestId, 
                    requestEnvelope.method, 
                    responsePayload, 
                    false, 
                    error
                );
            } else {
                // Send as single message
                const responseEnvelope = {
                    requestId: requestEnvelope.requestId,
                    isRequest: false,
                    method: requestEnvelope.method,
                    payload: responsePayload
                };
                
                const responseBytes = encodeRpcEnvelope(responseEnvelope);
                const responseBase64 = uint8ArrayToBase64(responseBytes);
                this._bridge.sendMessage(responseBase64);
            }
        } else {
            // Error response
            const responseEnvelope = {
                requestId: requestEnvelope.requestId,
                isRequest: false,
                method: requestEnvelope.method,
                error: error || 'Unknown error'
            };
            
            const responseBytes = encodeRpcEnvelope(responseEnvelope);
            const responseBase64 = uint8ArrayToBase64(responseBytes);
            this._bridge.sendMessage(responseBase64);
        }
    }

    /**
     * Send a message in chunks
     * @private
     */
    async _sendChunkedMessage(requestId, method, data, isRequest, error = null) {
        const chunkSetId = `${requestId}_${crypto.randomUUID()}`;
        const effectivePayloadSize = WebViewRpcConfiguration.getEffectivePayloadSize();
        const totalChunks = Math.ceil(data.length / effectivePayloadSize);

        for (let i = 1; i <= totalChunks; i++) {
            const offset = (i - 1) * effectivePayloadSize;
            const length = Math.min(effectivePayloadSize, data.length - offset);
            const chunkData = data.slice(offset, offset + length);

            const envelope = {
                requestId,
                isRequest,
                method,
                payload: chunkData,
                chunkInfo: {
                    chunkSetId,
                    chunkIndex: i,
                    totalChunks,
                    originalSize: data.length
                }
            };

            // Only set error on the first chunk
            if (i === 1 && error) {
                envelope.error = error;
            }

            const bytes = encodeRpcEnvelope(envelope);
            const base64 = uint8ArrayToBase64(bytes);
            this._bridge.sendMessage(base64);

            // Optional: Add small delay between chunks
            if (i < totalChunks) {
                await new Promise(resolve => setTimeout(resolve, 1));
            }
        }
    }

    /**
     * Legacy property for compatibility
     * @deprecated Use addService() instead
     */
    get services() {
        return this._services;
    }
}
