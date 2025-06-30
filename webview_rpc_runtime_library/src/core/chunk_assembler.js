import { WebViewRpcConfiguration } from './webview_rpc_configuration.js';

/**
 * Manages chunking and reassembly of messages
 */
export class ChunkAssembler {
    constructor() {
        this._chunkSets = new Map();
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
        const chunkSetId = chunkInfo.chunkSetId;

        // Check if we've reached the maximum number of chunk sets
        if (!this._chunkSets.has(chunkSetId) && 
            this._chunkSets.size >= WebViewRpcConfiguration.maxConcurrentChunkSets) {
            // Remove the oldest chunk set
            let oldestKey = null;
            let oldestTime = Date.now();
            
            for (const [key, value] of this._chunkSets) {
                if (value.lastActivity < oldestTime) {
                    oldestTime = value.lastActivity;
                    oldestKey = key;
                }
            }
            
            if (oldestKey) {
                this._chunkSets.delete(oldestKey);
                console.warn(`Maximum chunk sets reached (${WebViewRpcConfiguration.maxConcurrentChunkSets}). Removed oldest chunk set ${oldestKey}`);
            }
        }

        if (!this._chunkSets.has(chunkSetId)) {
            this._chunkSets.set(chunkSetId, {
                chunks: new Map(),
                totalChunks: chunkInfo.totalChunks,
                originalSize: chunkInfo.originalSize,
                lastActivity: Date.now(),
                requestId: envelope.requestId
            });
        }

        const chunkSet = this._chunkSets.get(chunkSetId);
        
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
                    console.error(`Missing chunk ${i} in set ${chunkSetId}`);
                    console.error(`Available chunks:`, Array.from(chunkSet.chunks.keys()));
                    return { data: null, timedOutRequestIds };
                }

                result.set(chunk, offset);
                offset += chunk.length;
            }

            // Clean up
            this._chunkSets.delete(chunkSetId);

            // Verify size
            if (offset !== chunkSet.originalSize) {
                console.error(`Assembled size mismatch: expected ${chunkSet.originalSize}, got ${offset}`);
                return { data: null, timedOutRequestIds };
            }

            return { data: result, timedOutRequestIds };
        }

        // Cleanup old chunk sets (older than configured timeout)
        const cutoff = Date.now() - (WebViewRpcConfiguration.chunkTimeoutSeconds * 1000);
        const toRemove = [];
        
        for (const [key, value] of this._chunkSets) {
            if (value.lastActivity < cutoff) {
                toRemove.push({ key, requestId: value.requestId });
            }
        }

        for (const { key, requestId } of toRemove) {
            timedOutRequestIds.push(requestId);
            this._chunkSets.delete(key);
            console.warn(`Removed incomplete chunk set ${key} for request ${requestId} due to timeout`);
        }

        return { data: null, timedOutRequestIds }; // Not all chunks received yet
    }
} 