import { WebViewRpcConfiguration } from './webview_rpc_configuration.js';

/**
 * Manages chunking and reassembly of messages
 */
export class ChunkAssembler {
    constructor() {
        this._chunkSetsByRequest = new Map();
    }

    /**
     * Try to reassemble chunks. Returns null if not all chunks received yet.
     * @param {Object} envelope - RPC envelope with potential chunk info
     * @returns {{data: Uint8Array|null, timedOutRequestIds: string[]}} - Complete data if all chunks received, null otherwise, and list of timed out request IDs
     */
    tryAssemble(envelope) {
        const timedOutRequestIds = [];
        
        if (!envelope.chunkInfo) {
            // Not a chunked message
            return { data: envelope.payload, timedOutRequestIds };
        }

        const chunkInfo = envelope.chunkInfo;
        const requestId = envelope.requestId;

        // Check if we've reached the maximum number of concurrent requests
        if (!this._chunkSetsByRequest.has(requestId) && 
            this._chunkSetsByRequest.size >= WebViewRpcConfiguration.maxConcurrentChunkSets) {
            // Remove the oldest request
            let oldestKey = null;
            let oldestTime = Date.now();
            
            for (const [key, value] of this._chunkSetsByRequest) {
                if (value.lastActivity < oldestTime) {
                    oldestTime = value.lastActivity;
                    oldestKey = key;
                }
            }
            
            if (oldestKey) {
                this._chunkSetsByRequest.delete(oldestKey);
                console.warn(`Maximum concurrent requests reached (${WebViewRpcConfiguration.maxConcurrentChunkSets}). Removed oldest request ${oldestKey}`);
            }
        }

        if (!this._chunkSetsByRequest.has(requestId)) {
            this._chunkSetsByRequest.set(requestId, {
                chunks: new Map(),
                totalChunks: chunkInfo.totalChunks,
                originalSize: chunkInfo.originalSize,
                lastActivity: Date.now()
            });
        }

        const chunkSet = this._chunkSetsByRequest.get(requestId);
        
        // Add chunk
        const chunkIndex = Number(chunkInfo.chunkIndex);
        chunkSet.chunks.set(chunkIndex, envelope.payload);
        chunkSet.lastActivity = Date.now();

        // Check if all chunks received
        if (chunkSet.chunks.size === chunkSet.totalChunks) {
            // Assemble
            const result = new Uint8Array(chunkSet.originalSize);
            let offset = 0;

            for (let i = 1; i <= chunkSet.totalChunks; i++) {
                const chunk = chunkSet.chunks.get(i);
                if (!chunk) {
                    console.error(`Missing chunk ${i} for request ${requestId}`);
                    console.error(`Available chunks:`, Array.from(chunkSet.chunks.keys()));
                    return { data: null, timedOutRequestIds };
                }

                result.set(chunk, offset);
                offset += chunk.length;
            }

            // Clean up
            this._chunkSetsByRequest.delete(requestId);

            // Verify size
            if (offset !== chunkSet.originalSize) {
                console.error(`Assembled size mismatch: expected ${chunkSet.originalSize}, got ${offset}`);
                return { data: null, timedOutRequestIds };
            }

            return { data: result, timedOutRequestIds };
        }

        // Cleanup old requests (older than configured timeout)
        const cutoff = Date.now() - (WebViewRpcConfiguration.chunkTimeoutSeconds * 1000);
        const toRemove = [];
        
        for (const [key, value] of this._chunkSetsByRequest) {
            if (value.lastActivity < cutoff) {
                toRemove.push(key);
            }
        }

        for (const requestId of toRemove) {
            timedOutRequestIds.push(requestId);
            this._chunkSetsByRequest.delete(requestId);
            console.warn(`Removed incomplete chunks for request ${requestId} due to timeout`);
        }

        return { data: null, timedOutRequestIds }; // Not all chunks received yet
    }
} 