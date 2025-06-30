export class VuplexBridge {
    constructor() {
        if (typeof window === 'undefined') {
            throw new Error('VuplexBridge requires browser environment');
        }
        
        this._onMessageCallback = null;
        this._isVuplexReady = false;
        this._pendingMessages = [];
        this._disposed = false;

        // Store bound event handlers for cleanup
        this._handleVuplexReady = this._handleVuplexReady.bind(this);
        this._handleVuplexMessage = this._handleVuplexMessage.bind(this);

        // 1) 만약 window.vuplex가 이미 있으면 바로 사용
        if (window.vuplex) {
            this._isVuplexReady = true;
        } else {
            // 아직 window.vuplex가 없으므로 'vuplexready' 이벤트 대기
            window.addEventListener('vuplexready', this._handleVuplexReady);
        }

        // 2) C# -> JS 메시지: "vuplexmessage" 이벤트
        //    event.value 에 문자열이 들어있음 (C# PostMessage로 보낸)
        window.addEventListener('vuplexmessage', this._handleVuplexMessage);
    }

    _handleVuplexReady() {
        if (this._disposed) return;
        
        this._isVuplexReady = true;
        // 대기중이던 메시지들을 모두 보냄
        for (const msg of this._pendingMessages) {
            window.vuplex.postMessage(msg);
        }
        this._pendingMessages = [];
    }

    _handleVuplexMessage(event) {
        if (this._disposed) return;
        
        const base64Str = event.value; // 보통 Base64
        if (this._onMessageCallback) {
            this._onMessageCallback(base64Str);
        }
    }

    /**
     * JS -> C# 문자열 전송 (base64Str)
     */
    sendMessage(base64Str) {
        if (this._disposed) {
            console.warn('VuplexBridge is disposed. Cannot send message.');
            return;
        }
        
        // Vuplex는 전달된 인자가 JS object이면 JSON 직렬화해 보내지만,
        // 우리가 'string'을 넘기면 'string' 그대로 보냄.
        if (this._isVuplexReady && window.vuplex) {
            window.vuplex.postMessage(base64Str);
        } else {
            // 아직 vuplex가 준비 안됐으면 대기열에 저장
            this._pendingMessages.push(base64Str);
        }
    }

    /**
     * onMessage(cb): JS가 C#으로부터 문자열을 받을 때 콜백 등록
     */
    onMessage(cb) {
        this._onMessageCallback = cb;
    }

    /**
     * Cleanup resources and remove event listeners
     */
    dispose() {
        if (this._disposed) return;
        
        this._disposed = true;
        
        // Remove event listeners
        window.removeEventListener('vuplexready', this._handleVuplexReady);
        window.removeEventListener('vuplexmessage', this._handleVuplexMessage);
        
        // Clear callbacks and pending messages
        this._onMessageCallback = null;
        this._pendingMessages = [];
        
        console.log('VuplexBridge disposed');
    }
}
