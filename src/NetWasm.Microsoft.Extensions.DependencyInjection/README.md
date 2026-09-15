# NetWasm.Microsoft.Extensions.DependencyInjection

`NetWasm.Microsoft.Extensions.DependencyInjection` supplies the
`Microsoft.Extensions.DependencyInjection` assembly for the `netwasm0.1`
profile. It is an independently versioned ported-library package for
applications that opt into the NetWasm dependency-injection container.

The package carries the `NetWasm,Version=v0.1` asset and depends on the exact
matching `NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions`
package. The package targets only `netwasm0.1`.

Applications use `IServiceCollection` registration and
`IServiceProvider` resolution APIs. The package-owned source generator closes
constructor activation, keyed services, sequences, and statically requested
open generics at build time. Referenced libraries publish only compile-time
closure facts, so internal implementations can participate without reflection,
`MakeGenericType`, runtime assembly discovery, or a NetWasm-specific compiler
hook.
