# NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions

`NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions` supplies the
`Microsoft.Extensions.DependencyInjection.Abstractions` assembly for NetWasm
(`net10.0-netwasm`). It is an independently versioned ported-library package
for applications that opt into the NetWasm dependency-injection contracts.

The package carries `net10.0-netwasm` assets and preserves the standard
assembly identity. The package targets only NetWasm (`net10.0-netwasm`).

Keyed service extension methods are declared on
`Microsoft.Extensions.DependencyInjection.ServiceProviderKeyedServiceExtensions`,
matching the desktop API used by generated activation consumers. The six static
keyed methods exposed on `ServiceProviderServiceExtensions` in NetWasm 0.4.1
remain as forwarding methods for existing binaries and explicit static calls.
Only the canonical declaring type exposes them as extension methods, so ordinary
extension calls remain unambiguous. Dynamic keyed enumeration by `Type` retains
the profile's existing `NotSupportedException` boundary.
