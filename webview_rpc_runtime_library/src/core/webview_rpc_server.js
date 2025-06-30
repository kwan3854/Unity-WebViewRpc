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
        this._disposed = false;
        
        // Store the message handler for cleanup
        this._messageHandler = (base64Message) => {
            this._handleMessage(base64Message);
        };
        
        // Listen for messages from Unity
        this._bridge.onMessage(this._messageHandler);
    }

    /**
     * Add a service to the server
     * @param {ServiceDefinition} service 
     */
    addService(service) {
        if (this._disposed) {
            throw new Error('Cannot add service to disposed server');
        }
        this._services.push(service);
    }

    /**
     * Start the server and register all method handlers
     */
    start() {
        if (this._disposed) {
            throw new Error('Cannot start disposed server');
        }
        
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
        if (this._disposed) return;
        
        try {
            const bytes = base64ToUint8Array(base64Message);
            const envelope = decodeRpcEnvelope(bytes);

            // Check if this is a chunked message
            if (envelope.chunkInfo) {
                // Try to reassemble
                const { data: completeData, timedOutRequestIds } = this._chunkAssembler.tryAssemble(envelope);
                
                // Log timed out requests (server doesn't need to handle them specifically)
                for (const requestId of timedOutRequestIds) {
                    console.warn(`Request ${requestId} timed out during chunk reassembly`);
                }
                
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
        if (this._disposed) return;
        
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
        // Check if this is an error response
        if (error) {
            // Error response
            const responseEnvelope = {
                requestId: requestEnvelope.requestId,
                isRequest: false,
                method: requestEnvelope.method,
                error: error
            };
            
            // Include payload if available (even if empty)
            if (responsePayload) {
                responseEnvelope.payload = responsePayload;
            }
            
            const responseBytes = encodeRpcEnvelope(responseEnvelope);
            const responseBase64 = uint8ArrayToBase64(responseBytes);
            this._bridge.sendMessage(responseBase64);
        } else if (responsePayload) {
            // Success response
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
                    null
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
            // No payload and no error - this is an error condition
            const responseEnvelope = {
                requestId: requestEnvelope.requestId,
                isRequest: false,
                method: requestEnvelope.method,
                error: 'Method returned null without error'
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
        }
    }

    /**
     * Legacy property for compatibility
     * @deprecated Use addService() instead
     */
    get services() {
        return this._services;
    }
    
    /**
     * Dispose the server and clean up resources
     */
    dispose() {
        if (this._disposed) return;
        
        this._disposed = true;
        
        // Clear all services and handlers
        this._services = [];
        this._methodHandlers = {};
        
        // Dispose the bridge if it has a dispose method
        if (this._bridge && typeof this._bridge.dispose === 'function') {
            this._bridge.dispose();
        }
        
        console.log('WebViewRpcServer disposed');
    }
}
