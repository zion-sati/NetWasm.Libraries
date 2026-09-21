# NetWasm.Microsoft.Extensions.Logging

Logging factory, filtering, dependency injection registration, and synchronous
stdout text and JSON providers for `netwasm0.1`. Console writes stay on the
calling reactor; queued background logging is outside the NetWasm port.
