/**
 * WebView RPC configuration settings
 */
export const WebViewRpcConfiguration = {
    _maxChunkSize: 256 * 1024,
    _chunkTimeoutSeconds: 30,
    
    /**
     * Estimated RPC envelope overhead in bytes when chunking
     * Includes: requestId, method, chunkInfo, protobuf tags
     */
    estimatedEnvelopeOverhead: 150,
    
    /**
     * Base64 encoding overhead ratio (4/3 ≈ 1.33)
     */
    base64OverheadRatio: 1.34, // Slightly higher for safety
    
    /**
     * Maximum chunk size in bytes (default: 256KB)
     * This is the maximum size of the final Base64-encoded message
     * JavaScript bridge bandwidth varies by platform
     * Minimum: calculated dynamically, Maximum: 10MB
     */
    get maxChunkSize() {
        return this._maxChunkSize;
    },
    
    set maxChunkSize(value) {
        const minimumSize = this.getMinimumSafeChunkSize();
        if (value < minimumSize) {
            throw new Error(
                `maxChunkSize must be at least ${minimumSize} bytes to accommodate RPC overhead. ` +
                `Provided: ${value} bytes. ` +
                `(Envelope overhead: ~${this.estimatedEnvelopeOverhead} bytes, Base64 overhead: ${Math.round((this.base64OverheadRatio - 1) * 100)}%)`
            );
        }
        if (value > 10 * 1024 * 1024) { // 10MB maximum
            throw new Error(`maxChunkSize cannot exceed 10MB. Provided: ${value} bytes`);
        }
        this._maxChunkSize = value;
        
        // Log effective payload size for debugging
        const effectiveSize = this.getEffectivePayloadSize();
        if (effectiveSize < 1024) {
            console.warn(
                `[WebViewRPC] With maxChunkSize=${value} bytes, effective payload size is only ${effectiveSize} bytes. ` +
                `Consider increasing maxChunkSize for better efficiency.`
            );
        }
    },

    /**
     * Enable/disable chunking (default: true)
     */
    enableChunking: true,
    
    /**
     * Timeout for incomplete chunk sets in seconds (default: 30 seconds)
     * Minimum: 5 seconds, Maximum: 300 seconds (5 minutes)
     */
    get chunkTimeoutSeconds() {
        return this._chunkTimeoutSeconds;
    },
    
    set chunkTimeoutSeconds(value) {
        if (value < 5) {
            throw new Error(`chunkTimeoutSeconds must be at least 5 seconds. Provided: ${value}`);
        }
        if (value > 300) {
            throw new Error(`chunkTimeoutSeconds cannot exceed 300 seconds. Provided: ${value}`);
        }
        this._chunkTimeoutSeconds = value;
    },
    
    /**
     * Calculate the effective payload size that should be used for chunking
     * This accounts for both RPC envelope overhead and Base64 encoding
     */
    getEffectivePayloadSize() {
        // Work backwards from the desired final size:
        // 1. Remove Base64 overhead: maxChunkSize / 1.34
        // 2. Remove envelope overhead
        const beforeBase64 = Math.floor(this.maxChunkSize / this.base64OverheadRatio);
        const effectivePayloadSize = beforeBase64 - this.estimatedEnvelopeOverhead;
        
        // Ensure we don't go negative
        return Math.max(effectivePayloadSize, 100);
    },
    
    /**
     * Calculate the minimum safe maxChunkSize considering all overheads
     */
    getMinimumSafeChunkSize() {
        // We need at least 100 bytes of payload + overhead + base64
        return Math.ceil((100 + this.estimatedEnvelopeOverhead) * this.base64OverheadRatio);
    },
    
    /**
     * Validate if the current maxChunkSize can accommodate meaningful payload
     */
    isChunkSizeValid() {
        return this.getEffectivePayloadSize() >= 100;
    }
}; 