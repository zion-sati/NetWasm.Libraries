# Upstream provenance

Unless listed otherwise, the library sources are adapted from
<https://github.com/dotnet/dotnet> (the `src/runtime` subtree) at commit
`811225a482702af7ecc35d817966bc70b88a3a23`, under the MIT License.

| NetWasm package | Upstream source area | Adaptation boundary |
| --- | --- | --- |
| `NetWasm.System.Linq` | `System.Linq` and selected `System.Collections.Immutable` helpers | Single-reactor iteration and reflection-free specializations |
| `NetWasm.System.Linq.AsyncEnumerable` | `System.Linq.AsyncEnumerable` | Single-reactor async iteration and reflection-free specializations |
| `NetWasm.System.Memory` | `System.Memory` | NetWasm CoreLib contracts and synchronous selected stream paths |
| `NetWasm.System.Text.Encodings.Web` | `System.Text.Encodings.Web` | Standalone runtime primitives and invariant resources |
| `NetWasm.System.IO.Pipelines` | `System.IO.Pipelines` | Single-reactor scheduling and available memory contracts |
| `NetWasm.System.Net.Http` | `System.Net.Http` | Host transport injection; no copied socket or browser handler |
| `NetWasm.System.Text.RegularExpressions` | `System.Text.RegularExpressions` | AOT/source-generated and interpreted paths without Reflection.Emit |
| `NetWasm.System.Text.Json` | `System.Text.Json` | Reflection-free generated metadata, synchronous streams, and mutable DOM |
| `NetWasm.System.Xml` | `System.Private.Xml` and `System.Xml.ReaderWriter` | Reflection-free in-memory parser/writer/DOM profile |
| `NetWasm.System.IO.Hashing` | `System.IO.Hashing` | Scalar portable implementation and available stream contracts |

The dependency-injection packages are adapted from
<https://github.com/dotnet/dotnet> commit
`b0f34d51fccc69fd334253924abd8d6853fad7aa`, from
`src/runtime/src/libraries/Microsoft.Extensions.DependencyInjection*`, under
the MIT License. Runtime constructor discovery and delegate creation are
replaced by deterministic source generation. The generator under `tools/` is
NetWasm-authored MIT-licensed code and is shipped only as a build-time analyzer.
