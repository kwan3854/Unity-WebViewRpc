/**
 * WebView RPC configuration settings
 */
export const WebViewRpcConfiguration = {
    /**
     * Maximum chunk size in bytes (default: 256KB)
     * JavaScript bridge bandwidth varies by platform
     */
    maxChunkSize: 256 * 1024,

    /**
     * Enable/disable chunking (default: true)
     */
    enableChunking: true
}; 