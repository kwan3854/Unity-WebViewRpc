# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-23

### Added
- Full async/await support for both server and client implementations
- `asyncMethodHandlers` dictionary in `ServiceDefinition` for async method registration
- Async method handling in `WebViewRpcServer` with automatic fallback to sync handlers
- Virtual-Virtual pattern in code generation templates for backward compatibility

### Changed
- **BREAKING**: Generated server methods now use async pattern by default
  - C# servers must implement `UniTask<Response> MethodNameAsync(Request request)`
  - JavaScript servers must implement `async MethodNameAsync(request)`
- **BREAKING**: Generated client methods now have `Async` suffix
  - C# clients call `await client.MethodNameAsync(request)`
  - JavaScript clients call `await client.MethodNameAsync(request)`
- WebViewRpcServer now processes async handlers first, then falls back to sync handlers

### Migration Guide

#### For C# Server Implementations

**Before (v0.x):**
```csharp
public class MyService : MyServiceBase
{
    public override Response MyMethod(Request request)
    {
        // Synchronous implementation
        return new Response { ... };
    }
}
```

**After (v1.0):**
```csharp
public class MyService : MyServiceBase
{
    public override async UniTask<Response> MyMethodAsync(Request request)
    {
        // Asynchronous implementation
        await UniTask.Delay(100);
        return new Response { ... };
    }
}
```

#### For JavaScript Server Implementations

**Before (v0.x):**
```javascript
class MyService extends MyServiceBase {
    MyMethod(request) {
        // Synchronous implementation
        return { ... };
    }
}
```

**After (v1.0):**
```javascript
class MyService extends MyServiceBase {
    async MyMethodAsync(request) {
        // Asynchronous implementation
        await someAsyncOperation();
        return { ... };
    }
}
```

#### For Client Code

**Before (v0.x):**
```csharp
var response = await client.MyMethod(request);
```

**After (v1.0):**
```csharp
var response = await client.MyMethodAsync(request);
```

### Backward Compatibility

The library maintains backward compatibility through the Virtual-Virtual pattern:
- Existing synchronous implementations will continue to work
- You can gradually migrate methods to async as needed
- The server automatically handles both sync and async methods

## [0.1.1] - Previous version
- Initial release with basic RPC functionality 