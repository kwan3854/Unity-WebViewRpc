import { decodeRpcEnvelope, encodeRpcEnvelope } from './rpc_envelope.js';
import { base64ToUint8Array, uint8ArrayToBase64 } from './webview_rpc_utils.js';

/**
 * WebView RPC Client
 * Makes RPC calls to Unity server
 */
export class WebViewRpcClient {
    constructor(bridge) {
        this._bridge = bridge;
        this._pendingRequests = new Map();
        this._requestIdCounter = 1;
        this._disposed = false;

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

        const requestId = String(this._requestIdCounter++);
        
        const envelope = {
            requestId,
            isRequest: true,
            method,
            payload: requestPayload
        };

        // Create promise for this request
        const promise = new Promise((resolve, reject) => {
            this._pendingRequests.set(requestId, { resolve, reject });
        });

        // Send request to Unity
        const bytes = encodeRpcEnvelope(envelope);
        const base64 = uint8ArrayToBase64(bytes);
        this._bridge.sendMessage(base64);

        return promise;
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
        } catch (error) {
            console.error('Error handling message:', error);
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
        }
    }
}
