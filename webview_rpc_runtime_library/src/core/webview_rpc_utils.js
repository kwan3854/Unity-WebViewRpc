import { encodeRpcEnvelope } from "./rpc_envelope.js";
import { decodeRpcEnvelope } from "./rpc_envelope.js";

export function encodeEnvelopeToBase64(envObj) {
    // envObj = { requestId, isRequest, method, payload, error }
    // encodeRpcEnvelope(...) expects a plain JS object with those fields
  
    const rawBytes = encodeRpcEnvelope(envObj);
    // rawBytes is Uint8Array
    return base64FromBytes(rawBytes);
  }

  export function decodeEnvelopeFromBase64(base64Str) {
    const bytes = base64ToBytes(base64Str);
    // decodeRpcEnvelope(bytes) -> plain object { requestId, isRequest, ...}
    const messageObj = decodeRpcEnvelope(bytes);
    return messageObj;
  }

/** Uint8Array -> Base64 (browser JS) */
let isNode = typeof window === 'undefined';

export function base64FromBytes(u8arr) {
  if (isNode) {
    return Buffer.from(u8arr).toString('base64');
  } else {
    let binary = "";
    for (let i = 0; i < u8arr.length; i++) {
      binary += String.fromCharCode(u8arr[i]); 
    }
    return btoa(binary);
  }
}

export function base64ToBytes(b64) {
  if (isNode) {
    return Buffer.from(b64, 'base64');
  } else {
    const bin = atob(b64);
    const u8 = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) {
      u8[i] = bin.charCodeAt(i);
    }
    return u8;
  }
}
