export class ServiceDefinition {
    constructor() {
      // methodName -> handlerFn
      // handlerFn: (requestBytes: Uint8Array) => Uint8Array
      this.methodHandlers = {};
    }
  }
  