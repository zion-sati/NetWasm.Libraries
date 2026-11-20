# NetWasm.Microsoft.Extensions.Logging

Logging factory, filtering, dependency injection registration, and synchronous
stdout text and JSON providers for NetWasm (`net10.0-netwasm`). Console writes stay on the
calling reactor; queued background logging is outside the NetWasm port.
