namespace WebViewRPC
{
    /// <summary>
    /// WebView RPC configuration settings
    /// </summary>
    public static class WebViewRpcConfiguration
    {
        /// <summary>
        /// Maximum chunk size in bytes (default: 256KB)
        /// JavaScript bridge bandwidth varies by platform
        /// </summary>
        public static int MaxChunkSize { get; set; } = 256 * 1024;

        /// <summary>
        /// Enable/disable chunking (default: true)
        /// </summary>
        public static bool EnableChunking { get; set; } = true;
    }
} 