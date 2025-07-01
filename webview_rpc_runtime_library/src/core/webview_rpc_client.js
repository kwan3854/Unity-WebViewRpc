import { decodeRpcEnvelope, encodeRpcEnvelope } from './rpc_envelope.js';
import { base64ToUint8Array, uint8ArrayToBase64 } from './webview_rpc_utils.js';
import { WebViewRpcConfiguration } from './webview_rpc_configuration.js';
import { ChunkAssembler } from './chunk_assembler.js';

/**
 * WebView RPC Client
 * Makes RPC calls to Unity server
 */
export class WebViewRpcClient {
    constructor(bridge) {
        this._bridge = bridge;
        this._pendingRequests = new Map();
        this._disposed = false;
        this._chunkAssembler = new ChunkAssembler();

        // Listen for responses from Unity
        this._bridge.onMessage((base64Message) => {
            this._handleMessage(base64Message);
        });
    }

    /**
     * Call a remote method
     * @param {string} method - Method name
     * @param {Uint8Array} requestPayload - Request payload
     * @returns {Promise<Uint8Array>} Response payload
     */
    async callMethod(method, requestPayload) {
        if (this._disposed) {
            throw new Error('RpcClient is disposed');
        }

        const requestId = crypto.randomUUID();
        
        // Create promise for this request
        const promise = new Promise((resolve, reject) => {
            this._pendingRequests.set(requestId, { resolve, reject });
        });

        // Check if chunking is needed
        const effectivePayloadSize = WebViewRpcConfiguration.getEffectivePayloadSize();
        if (WebViewRpcConfiguration.enableChunking && 
            requestPayload.length > effectivePayloadSize) {
            // Send as chunks
            await this._sendChunkedMessage(requestId, method, requestPayload, true);
        } else {
            // Send as single message
            const envelope = {
                requestId,
                isRequest: true,
                method,
                payload: requestPayload
            };

            const bytes = encodeRpcEnvelope(envelope);
            const base64 = uint8ArrayToBase64(bytes);
            this._bridge.sendMessage(base64);
        }

        return promise;
    }

    /**
     * Send a message in chunks
     * @private
     */
    async _sendChunkedMessage(requestId, method, data, isRequest) {
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
                    chunkSetId: '',
                    chunkIndex: i,
                    totalChunks,
                    originalSize: data.length
                }
            };

            const bytes = encodeRpcEnvelope(envelope);
            const base64 = uint8ArrayToBase64(bytes);
            this._bridge.sendMessage(base64);
        }
    }

    /**
     * Handle incoming message from Unity
     * @param {string} base64Message 
     */
    _handleMessage(base64Message) {
        if (this._disposed) return;

        try {
            const bytes = base64ToUint8Array(base64Message);
            const envelope = decodeRpcEnvelope(bytes);

            // Check if this is a chunked message
            if (envelope.chunkInfo) {
                // Try to reassemble
                const { data: completeData, timedOutRequestIds } = this._chunkAssembler.tryAssemble(envelope);
                
                // Handle timed out requests
                for (const requestId of timedOutRequestIds) {
                    const pending = this._pendingRequests.get(requestId);
                    if (pending) {
                        this._pendingRequests.delete(requestId);
                        pending.reject(new Error(`Chunk reassembly timeout for request ${requestId} after ${WebViewRpcConfiguration.chunkTimeoutSeconds} seconds`));
                    }
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
                    
                    this._processCompleteEnvelope(completeEnvelope);
                }
                // else: waiting for more chunks
            } else {
                // Process as regular message
                this._processCompleteEnvelope(envelope);
            }
        } catch (error) {
            console.error('Error handling message:', error);
        }
    }

    /**
     * Process a complete envelope
     * @private
     */
    _processCompleteEnvelope(envelope) {
        if (!envelope.isRequest) {
            // Handle response
            const pending = this._pendingRequests.get(envelope.requestId);
            if (pending) {
                this._pendingRequests.delete(envelope.requestId);
                
                if (envelope.error) {
                    pending.reject(new Error(envelope.error));
                } else {
                    pending.resolve(envelope.payload);
                }
            }
        }
    }

    /**
     * Dispose the client
     */
    dispose() {
        if (!this._disposed) {
            this._disposed = true;
            
            // Reject all pending requests
            for (const pending of this._pendingRequests.values()) {
                pending.reject(new Error('Client disposed'));
            }
            this._pendingRequests.clear();
            
            // Dispose the bridge if it has a dispose method
            if (this._bridge && typeof this._bridge.dispose === 'function') {
                this._bridge.dispose();
            }
            
            console.log('WebViewRpcClient disposed');
        }
    }
}
