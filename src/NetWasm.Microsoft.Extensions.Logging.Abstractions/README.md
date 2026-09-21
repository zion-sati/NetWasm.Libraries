# NetWasm.Microsoft.Extensions.Logging.Abstractions

The NetWasm `netwasm0.1` port of the released .NET 10 logging abstractions.
It includes the `ILogger` contracts, structured message helpers, null logger
implementations, and the `[LoggerMessage]` source generator shipped as a build
analyzer. Runtime logging is synchronous and single reactor friendly.

`ILogger<T>` resolved through NetWasm dependency injection receives its category
name as generated code. For manual construction, use
`ILoggerFactory.CreateLogger(string)`; the reflection-based
`new Logger<T>(factory)` and `factory.CreateLogger<T>()` paths are rejected.
