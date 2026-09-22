# NetWasm.Microsoft.Extensions.Http

This package provides the useful `IHttpClientFactory` surface for
`netwasm0.1`: named clients, typed clients, `DelegatingHandler` pipelines, and
host-owned HTTP transport composition.

Handler lifetime expiry is checked when a client or handler is requested. The
package does not start a background rotation task or reproduce a socket pool;
WASI HTTP owns connection pooling. Sockets-handler configuration, logging
handlers, keyed-client registration, and reflection-only typed activation are
outside this first profile. Generic handler registrations follow the normal DI
contract (`AddTransient`/`AddScoped` before `AddHttpMessageHandler<T>`); typed
client constructors are closed by the NetWasm DI generator.
