# NetWasm.Microsoft.Extensions.DependencyInjection

`NetWasm.Microsoft.Extensions.DependencyInjection` supplies the
`Microsoft.Extensions.DependencyInjection` assembly for NetWasm (`net10.0-netwasm`). It is an independently versioned ported-library package for
applications that opt into the NetWasm dependency-injection container.

The package carries `net10.0-netwasm` assets and depends on the exact
matching `NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions`
package. The package targets only NetWasm (`net10.0-netwasm`).

Applications use `IServiceCollection` registration and
`IServiceProvider` resolution APIs. The package-owned source generator closes
constructor activation, keyed services, sequences, and statically requested
open generics at build time. Referenced libraries publish only compile-time
closure facts, so internal implementations can participate without reflection,
`MakeGenericType`, runtime assembly discovery, or a NetWasm-specific compiler
hook.
