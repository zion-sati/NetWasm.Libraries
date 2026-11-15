# Upstream provenance

Unless listed otherwise, the library sources are adapted from
<https://github.com/dotnet/dotnet> (the `src/runtime` subtree) at commit
`811225a482702af7ecc35d817966bc70b88a3a23`, under the MIT License.

| NetWasm package | Upstream source area | Adaptation boundary |
| --- | --- | --- |
| `NetWasm.System.IO.Hashing` | `System.IO.Hashing` | Scalar portable implementation and available stream contracts |
| `NetWasm.System.Formats.Cbor` | `System.Formats.Cbor` | Pinned .NETCoreApp reader/writer sources; English resource fallback |

The dependency-injection packages are adapted from
<https://github.com/dotnet/dotnet> commit
`b0f34d51fccc69fd334253924abd8d6853fad7aa`, from
`src/runtime/src/libraries/Microsoft.Extensions.DependencyInjection*`, under
the MIT License. Runtime constructor discovery and delegate creation are
replaced by deterministic source generation. The generator under `tools/` is
NetWasm-authored MIT-licensed code and is shipped only as a build-time analyzer.
