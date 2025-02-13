import { decodeEnvelopeFromBase64, encodeEnvelopeToBase64 } from "./webview_rpc_utils.js";

export class WebViewRpcClient {
  /**
   * @param {IJsBridge} bridge
   */
  constructor(bridge) {
    this.bridge = bridge;
    this._disposed = false;

    // requestId -> { resolve, reject }
    this.pendingMap = new Map();

    this.bridge.onMessage((base64Str) => this._onBridgeMessage(base64Str));
  }

  /**
   * JS -> 서버 RPC 호출
   * @param {string} methodName
   * @param {Uint8Array} requestBytes
   * @returns {Promise<Uint8Array>} responseBytes
   */
  callMethod(methodName, requestBytes) {
    if (this._disposed) {
      return Promise.reject(new Error("RpcClient disposed"));
    }

    return new Promise((resolve, reject) => {
      const requestId = generateRequestId();
      this.pendingMap.set(requestId, { resolve, reject });

      const envelopeObj = {
        requestId,
        isRequest: true,
        method: methodName,
        payload: requestBytes,
        error: ""
      };

      const base64Str = encodeEnvelopeToBase64(envelopeObj);
      this.bridge.sendMessage(base64Str);
    });
  }

  _onBridgeMessage(base64Str) {
    if (this._disposed) return;

    let env;
    try {
      env = decodeEnvelopeFromBase64(base64Str);
    } catch (ex) {
      console.warn("[WebViewRpcClient] decode error:", ex);
      return;
    }

    if (env.isRequest) {
      // 클라이언트 입장 -> Request는 무시
      return;
    }

    // 응답 처리
    const { requestId, payload, error } = env;
    const pending = this.pendingMap.get(requestId);
    if (!pending) return;
    this.pendingMap.delete(requestId);

    if (error) {
      pending.reject(new Error(error));
    } else {
      pending.resolve(payload);
    }
  }

  dispose() {
    if (!this._disposed) {
      this._disposed = true;
      // onMessage 해제 등
    }
  }
}

function generateRequestId() {
  return Math.random().toString(36).slice(2) + Date.now().toString(36);
}
