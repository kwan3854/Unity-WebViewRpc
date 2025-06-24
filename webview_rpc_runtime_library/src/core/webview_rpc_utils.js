import { encodeRpcEnvelope } from "./rpc_envelope.js";
import { decodeRpcEnvelope } from "./rpc_envelope.js";

export function encodeEnvelopeToBase64(envObj) {
    // envObj = { requestId, isRequest, method, payload, error }
    // encodeRpcEnvelope(...) expects a plain JS object with those fields
  
    const rawBytes = encodeRpcEnvelope(envObj);
    // rawBytes is Uint8Array
    return uint8ArrayToBase64(rawBytes);
  }

  export function decodeEnvelopeFromBase64(base64Str) {
    const bytes = base64ToUint8Array(base64Str);
    // decodeRpcEnvelope(bytes) -> plain object { requestId, isRequest, ...}
    const messageObj = decodeRpcEnvelope(bytes);
    return messageObj;
  }

/**
 * Base64 <-> Uint8Array conversion utilities
 * (Browser and Node.js compatible)
 */

/**
 * Converts a Uint8Array to a Base64 string.
 * @param {Uint8Array} bytes - The byte array to convert.
 * @returns {string} The Base64 encoded string.
 */
export function uint8ArrayToBase64(bytes) {
  if (typeof window !== 'undefined' && typeof window.btoa === 'function') {
    // Browser environment
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  } else if (typeof Buffer === 'function') {
    // Node.js environment
    return Buffer.from(bytes).toString('base64');
  } else {
    throw new Error('Unsupported environment for Base64 encoding');
  }
}

/**
 * Converts a Base64 string to a Uint8Array.
 * @param {string} base64 - The Base64 string to convert.
 * @returns {Uint8Array} The decoded byte array.
 */
export function base64ToUint8Array(base64) {
  if (typeof window !== 'undefined' && typeof window.atob === 'function') {
    // Browser environment
    const binary_string = window.atob(base64);
    const len = binary_string.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = binary_string.charCodeAt(i);
    }
    return bytes;
  } else if (typeof Buffer === 'function') {
    // Node.js environment
    return new Uint8Array(Buffer.from(base64, 'base64'));
  } else {
    throw new Error('Unsupported environment for Base64 decoding');
  }
}
