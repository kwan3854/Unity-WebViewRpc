import { decodeEnvelopeFromBase64, encodeEnvelopeToBase64 } from "./webview_rpc_utils.js";
import { ServiceDefinition } from "./service_definition.js";

export class WebViewRpcServer {
  /**
   * @param {IJsBridge} bridge - { sendMessage(str), onMessage(cb) }
   */
  constructor(bridge) {
    this.bridge = bridge;

    this.services = []; // Array<ServiceDefinition>
    this.methodHandlers = {}; // 최종 (methodName -> function(bytes) => bytes)
    this.asyncMethodHandlers = {}; // 최종 (methodName -> async function(bytes) => Promise<bytes>)

    this._started = false;
    this._disposed = false;

    // 수신 이벤트
    this.bridge.onMessage((base64Str) => this._onBridgeMessage(base64Str));
  }

  start() {
    if (this._started) return;
    this._started = true;

    // Services 배열을 쭉 돌면서, 모든 methodHandler를 합침
    for (const svcDef of this.services) {
      for (const [methodName, handlerFn] of Object.entries(svcDef.methodHandlers)) {
        this.methodHandlers[methodName] = handlerFn;
      }
      
      // asyncMethodHandlers도 합침
      for (const [methodName, asyncHandlerFn] of Object.entries(svcDef.asyncMethodHandlers || {})) {
        this.asyncMethodHandlers[methodName] = asyncHandlerFn;
      }
    }
  }

  async _onBridgeMessage(base64Str) {
    if (this._disposed) return;

    let env;
    try {
      env = decodeEnvelopeFromBase64(base64Str);
    } catch (ex) {
      console.warn("Exception while parsing envelope:", ex);
      return;
    }

    if (!env.isRequest) {
      // 응답이면 서버 입장에선 무시
      return;
    }

    // 요청 처리
    const { requestId, method, payload } = env; // payload = Uint8Array
    const respEnv = {
      requestId,
      isRequest: false,
      method,
      payload: new Uint8Array(), // 기본값
      error: ""
    };

    // 먼저 asyncMethodHandlers를 확인
    const asyncHandler = this.asyncMethodHandlers[method];
    if (asyncHandler) {
      try {
        // asyncHandler( requestBytes ) => Promise<responseBytes>
        const responseBytes = await asyncHandler(payload);
        respEnv.payload = responseBytes;
      } catch (ex) {
        respEnv.error = ex.message || String(ex);
      }
    } else {
      // 동기 핸들러 폴백
      const handler = this.methodHandlers[method];
      if (!handler) {
        respEnv.error = `Unknown method: ${method}`;
      } else {
        try {
          // handler( requestBytes ) => responseBytes
          const responseBytes = handler(payload);
          respEnv.payload = responseBytes;
        } catch (ex) {
          respEnv.error = ex.message || String(ex);
        }
      }
    }

    // 응답 전송
    const respBase64 = encodeEnvelopeToBase64(respEnv);
    this.bridge.sendMessage(respBase64);
  }

  dispose() {
    if (!this._disposed) {
      // 실제론 onMessage 해제 등
      this._disposed = true;
    }
  }
}
