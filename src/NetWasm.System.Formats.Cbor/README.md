# NetWasm.System.Formats.Cbor

`NetWasm.System.Formats.Cbor` provides the pinned .NET `System.Formats.Cbor`
reader and writer APIs for the `netwasm0.1` profile. Use `CborReader` and
`CborWriter` to parse and produce CBOR without reflection-based object mapping.

```csharp
using System.Formats.Cbor;

var writer = new CborWriter();
writer.WriteStartMap(1);
writer.WriteTextString("answer");
writer.WriteInt32(42);
writer.WriteEndMap();
byte[] encoded = writer.Encode();

var reader = new CborReader(encoded);
reader.ReadStartMap();
string key = reader.ReadTextString();
int value = reader.ReadInt32();
reader.ReadEndMap();
```

The implementation is adapted from the MIT-licensed .NET source revision
recorded in [`UPSTREAM_SOURCES.md`](UPSTREAM_SOURCES.md). The package targets
`netwasm0.1`; reference it only when an application uses this API.

The package tests cover date-tag formatting and parsing, nested `SkipValue`
traversal, and exact `ReadEncodedValue` extraction on both desktop .NET and
`netwasm0.1`.
