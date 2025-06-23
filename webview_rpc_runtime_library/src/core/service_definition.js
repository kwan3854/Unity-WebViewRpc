/**
 * Service definition for WebView RPC
 * All methods are async by default
 */
export class ServiceDefinition {
    constructor() {
        /**
         * Dictionary mapping method names to their async handlers
         * @type {Object.<string, function(Uint8Array): Promise<Uint8Array>>}
         */
        this.methodHandlers = {};
    }
}
  