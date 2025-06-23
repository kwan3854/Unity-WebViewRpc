# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-06-23

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