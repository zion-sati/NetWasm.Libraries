# NetWasm.System.Net.Http

`NetWasm.System.Net.Http` supplies the `System.Net.Http` assembly for the
`netwasm0.1` profile. It is an independently versioned ported-library package
for applications that opt into the supported managed HTTP surface.

The package carries the `NetWasm,Version=v0.1` asset and preserves the public
`System.Net.Http` assembly identity. Its default transport uses the versioned
WASI HTTP Preview 2 capability boundary supplied by the NetWasm host; it does
not add a JavaScript HTTP fallback or replace desktop .NET's built-in
`System.Net.Http` reference.
