# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.8] - 2025-01-22

### Added
- Proper dispose pattern for JavaScript components
- `dispose()` method to VuplexBridge to remove event listeners
- `dispose()` method to WebViewRpcServer
- Automatic cleanup on page unload
- Disposal state checking to prevent operations on disposed objects
- Sample code demonstrating proper cleanup

### Changed
- WebViewRpcClient now disposes the bridge when disposed
- WebViewRpcServer now disposes the bridge when disposed
- Event handlers are now stored for proper removal

### Fixed
- Memory leaks from unremoved event listeners
- Potential resource leaks when page navigates away

## [2.0.7] - 2025-01-22

### Fixed
- Improved error handling: errors now take precedence over payload
- Fixed issue where methods returning null without error were not properly handled
- Removed unnecessary 1ms delay between chunks (chunk order is already guaranteed by index)

### Changed
- Error responses can now include payload data if available
- Clearer error messages when methods return null without setting an error

## [2.0.6] - 2025-01-22

### Added
- Smart chunk size calculation that accounts for RPC envelope and Base64 encoding overhead
- `getEffectivePayloadSize()` method to calculate actual usable payload size
- `getMinimumSafeChunkSize()` method to determine minimum viable chunk size
- `isChunkSizeValid()` method to validate configuration
- Dynamic minimum chunk size validation based on overhead calculations
- Detailed logging when chunk size results in small effective payload

### Changed
- **BREAKING**: `maxChunkSize` now represents the final Base64-encoded message size (not the raw payload size)
- Improved chunk size validation with informative error messages
- Minimum chunk size is now dynamically calculated (~335 bytes) instead of fixed 1KB
- Added warnings when effective payload size is less than 1KB

### Fixed
- Fixed issue where actual transmitted size could exceed configured `maxChunkSize` due to overhead
- Fixed inefficient chunking when using small chunk sizes

## [1.0.4] - 2025-06-24

### Fixed
- Fixed null handling issue in RPC envelope encoding
  - WebViewRpcClient and WebViewRpcServer were explicitly setting `error` field to `null`
  - This caused `Cannot read properties of null (reading 'length')` error
  - Now follows Proto3 spec: optional fields are omitted instead of set to null
  - Proto3 string fields default to empty string automatically when not set

## [1.0.3] - 2025-06-23

### Fixed
- Fixed npm package build issue where wrong function names were in the published package
  - The source code used `base64ToUint8Array` and `uint8ArrayToBase64`
  - But the published npm package incorrectly had `base64ToBytes` and `base64FromBytes`
  - This caused import errors when using the package

## [1.0.2] - 2025-06-23

### Changed
- **BREAKING**: Complete redesign for async-only architecture
- **BREAKING**: Removed all synchronous method support
- **BREAKING**: Removed `Async` suffix from all method names
- **BREAKING**: Changed from Virtual-Virtual pattern to Abstract pattern
  - Server implementations must now use `abstract` methods (mandatory override)
  - Cleaner, more explicit async-only design
- Simplified codebase by removing dual sync/async handlers
- Improved type safety with abstract methods

### Removed
- Synchronous method handlers
- `AsyncMethodHandlers` dictionary (merged into single `MethodHandlers`)
- Virtual-Virtual pattern fallback mechanism
- `Async` suffix from all generated methods

### Migration Guide from v1.0.0

#### For C# Server Implementations

**v1.0.0:**
```csharp
public class MyService : MyServiceBase
{
    public override async UniTask<Response> MyMethodAsync(Request request)
    {
        // Implementation
    }
}
```

**v1.0.1:**
```csharp
public class MyService : MyServiceBase
{
    public override async UniTask<Response> MyMethod(Request request)
    {
        // Implementation (remove Async suffix)
    }
}
```

#### For Client Code (C# and JavaScript)

**v1.0.0:**
```csharp
var response = await client.MyMethodAsync(request);
```

**v1.0.1:**
```csharp
var response = await client.MyMethod(request);
```

### Notes
- This version prioritizes clean, maintainable code over backward compatibility
- All RPC methods are now async-only by design
- Simpler mental model: one method = one async implementation

## [1.0.0] - 2025-06-23

### Added
- Initial async/await support with Virtual-Virtual pattern
- Backward compatibility with synchronous methods

## [0.1.1] - Previous version
- Initial release with basic RPC functionality 