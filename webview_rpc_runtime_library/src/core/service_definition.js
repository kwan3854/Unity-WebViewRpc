export class ServiceDefinition {
    constructor() {
      // methodName -> handlerFn
      // handlerFn: (requestBytes: Uint8Array) => Uint8Array
      this.methodHandlers = {};
      
      // methodName -> asyncHandlerFn
      // asyncHandlerFn: (requestBytes: Uint8Array) => Promise<Uint8Array>
      this.asyncMethodHandlers = {};
    }
  }
  