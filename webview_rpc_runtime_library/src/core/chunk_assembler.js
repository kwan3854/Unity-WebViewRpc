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
     * @returns {Uint8Array|null} - Complete data if all chunks received, null otherwise
     */
    tryAssemble(envelope) {
        if (!envelope.chunkInfo) {
            // Not a chunked message
            return envelope.payload;
        }

        const chunkInfo = envelope.chunkInfo;
        const chunkSetId = chunkInfo.chunkSetId;

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

        // Cleanup old chunk sets (older than 30 seconds)
        const cutoff = Date.now() - 30000;
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