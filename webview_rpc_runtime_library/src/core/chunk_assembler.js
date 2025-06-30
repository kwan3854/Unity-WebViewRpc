import { WebViewRpcConfiguration } from './webview_rpc_configuration.js';

/**
 * Manages chunking and reassembly of messages
 */
export class ChunkAssembler {
    constructor() {
        this._chunkSets = new Map();
        // Maximum number of concurrent chunk sets to prevent memory issues
        this._maxConcurrentChunkSets = 100;
    }

    /**
     * Try to reassemble chunks. Returns null if not all chunks received yet.
     * @param {Object} envelope - RPC envelope with potential chunk info
     * @returns {Uint8Array|null} - Complete data if all chunks received, null otherwise
     */
    tryAssemble(envelope) {
        if (!envelope.chunkInfo) {
            // Not a chunked message
            return envelope.payload;
        }

        const chunkInfo = envelope.chunkInfo;
        const chunkSetId = chunkInfo.chunkSetId;

        // Check if we've reached the maximum number of chunk sets
        if (!this._chunkSets.has(chunkSetId) && 
            this._chunkSets.size >= this._maxConcurrentChunkSets) {
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
                console.warn(`Maximum chunk sets reached. Removed oldest chunk set ${oldestKey}`);
            }
        }

        if (!this._chunkSets.has(chunkSetId)) {
            this._chunkSets.set(chunkSetId, {
                chunks: new Map(),
                totalChunks: chunkInfo.totalChunks,
                originalSize: chunkInfo.originalSize,
                lastActivity: Date.now()
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
                    return null;
                }

                result.set(chunk, offset);
                offset += chunk.length;
            }

            // Clean up
            this._chunkSets.delete(chunkSetId);

            // Verify size
            if (offset !== chunkSet.originalSize) {
                console.error(`Assembled size mismatch: expected ${chunkSet.originalSize}, got ${offset}`);
                return null;
            }

            return result;
        }

        // Cleanup old chunk sets (older than configured timeout)
        const cutoff = Date.now() - (WebViewRpcConfiguration.chunkTimeoutSeconds * 1000);
        const toRemove = [];
        
        for (const [key, value] of this._chunkSets) {
            if (value.lastActivity < cutoff) {
                toRemove.push(key);
            }
        }

        for (const key of toRemove) {
            this._chunkSets.delete(key);
            console.warn(`Removed incomplete chunk set ${key} due to timeout`);
        }

        return null; // Not all chunks received yet
    }
} 