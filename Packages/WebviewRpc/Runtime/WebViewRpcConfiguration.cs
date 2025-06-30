namespace WebViewRPC
{
    /// <summary>
    /// WebView RPC configuration settings
    /// </summary>
    public static class WebViewRpcConfiguration
    {
        private static int _maxChunkSize = 256 * 1024;
        private static int _chunkTimeoutSeconds = 30;
        
        /// <summary>
        /// Estimated RPC envelope overhead in bytes when chunking
        /// Includes: requestId, method, chunkInfo, protobuf tags
        /// </summary>
        private const int EstimatedEnvelopeOverhead = 150;
        
        /// <summary>
        /// Base64 encoding overhead ratio (4/3 ≈ 1.33)
        /// </summary>
        private const double Base64OverheadRatio = 1.34; // Slightly higher for safety
        
        /// <summary>
        /// Maximum chunk size in bytes (default: 256KB)
        /// This is the maximum size of the final Base64-encoded message
        /// JavaScript bridge bandwidth varies by platform
        /// Minimum: 1KB, Maximum: 10MB
        /// </summary>
        public static int MaxChunkSize 
        { 
            get => _maxChunkSize;
            set
            {
                int minimumSize = GetMinimumSafeChunkSize();
                if (value < minimumSize)
                {
                    throw new System.ArgumentException(
                        $"MaxChunkSize must be at least {minimumSize} bytes to accommodate RPC overhead. " +
                        $"Provided: {value} bytes. " +
                        $"(Envelope overhead: ~{EstimatedEnvelopeOverhead} bytes, Base64 overhead: {(Base64OverheadRatio - 1) * 100:F0}%)");
                }
                if (value > 10 * 1024 * 1024) // 10MB maximum
                {
                    throw new System.ArgumentException($"MaxChunkSize cannot exceed 10MB. Provided: {value} bytes");
                }
                _maxChunkSize = value;
                
                // Log effective payload size for debugging
                int effectiveSize = GetEffectivePayloadSize();
                if (effectiveSize < 1024)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[WebViewRPC] With MaxChunkSize={value} bytes, effective payload size is only {effectiveSize} bytes. " +
                        $"Consider increasing MaxChunkSize for better efficiency.");
                }
            }
        }

        /// <summary>
        /// Enable/disable chunking (default: true)
        /// </summary>
        public static bool EnableChunking { get; set; } = true;
        
        /// <summary>
        /// Timeout for incomplete chunk sets in seconds (default: 30 seconds)
        /// Minimum: 5 seconds, Maximum: 300 seconds (5 minutes)
        /// </summary>
        public static int ChunkTimeoutSeconds
        {
            get => _chunkTimeoutSeconds;
            set
            {
                if (value < 5)
                {
                    throw new System.ArgumentException($"ChunkTimeoutSeconds must be at least 5 seconds. Provided: {value}");
                }
                if (value > 300)
                {
                    throw new System.ArgumentException($"ChunkTimeoutSeconds cannot exceed 300 seconds. Provided: {value}");
                }
                _chunkTimeoutSeconds = value;
            }
        }
        
        /// <summary>
        /// Calculate the effective payload size that should be used for chunking
        /// This accounts for both RPC envelope overhead and Base64 encoding
        /// </summary>
        public static int GetEffectivePayloadSize()
        {
            // Work backwards from the desired final size:
            // 1. Remove Base64 overhead: MaxChunkSize / 1.34
            // 2. Remove envelope overhead
            int beforeBase64 = (int)(MaxChunkSize / Base64OverheadRatio);
            int effectivePayloadSize = beforeBase64 - EstimatedEnvelopeOverhead;
            
            // Ensure we don't go negative
            return System.Math.Max(effectivePayloadSize, 100);
        }
        
        /// <summary>
        /// Calculate the minimum safe MaxChunkSize considering all overheads
        /// </summary>
        public static int GetMinimumSafeChunkSize()
        {
            // We need at least 100 bytes of payload + overhead + base64
            return (int)((100 + EstimatedEnvelopeOverhead) * Base64OverheadRatio);
        }
        
        /// <summary>
        /// Validate if the current MaxChunkSize can accommodate meaningful payload
        /// </summary>
        public static bool IsChunkSizeValid()
        {
            return GetEffectivePayloadSize() >= 100;
        }
    }
} 