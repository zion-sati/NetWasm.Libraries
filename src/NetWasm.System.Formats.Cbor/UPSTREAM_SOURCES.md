# CBOR source manifest

Production source baseline: `dotnet/dotnet` commit `811225a482702af7ecc35d817966bc70b88a3a23`.
License: MIT. Upstream file headers are retained.

The package compiles the pinned `.NETCoreApp` implementation set. The
upstream `.netstandard` alternatives and generated resource infrastructure
are not included. English resource values are represented by the local `SR.cs`
fallback because the NetWasm runtime profile does not use ResourceManager.
NetWasm also omits `CborContentException`'s protected legacy formatter
serialization constructor: the API is obsolete and its serialization types
are not present in the closed-world runtime profile. The desktop source oracle
retains that constructor.

## Pinned Cbor sources

- `System/Formats/Cbor/CborConformanceLevel.cs`
- `System/Formats/Cbor/CborContentException.cs`
- `System/Formats/Cbor/CborHelpers.netcoreapp.cs`
- `System/Formats/Cbor/CborInitialByte.cs`
- `System/Formats/Cbor/CborTag.cs`
- `System/Formats/Cbor/HalfHelpers.netcoreapp.cs`
- `System/Formats/Cbor/Reader/CborReader.Array.cs`
- `System/Formats/Cbor/Reader/CborReader.Integer.cs`
- `System/Formats/Cbor/Reader/CborReader.Map.cs`
- `System/Formats/Cbor/Reader/CborReader.PeekState.cs`
- `System/Formats/Cbor/Reader/CborReader.Simple.cs`
- `System/Formats/Cbor/Reader/CborReader.Simple.netcoreapp.cs`
- `System/Formats/Cbor/Reader/CborReader.SkipValue.cs`
- `System/Formats/Cbor/Reader/CborReader.String.cs`
- `System/Formats/Cbor/Reader/CborReader.Tag.cs`
- `System/Formats/Cbor/Reader/CborReader.cs`
- `System/Formats/Cbor/Reader/CborReaderOptions.cs`
- `System/Formats/Cbor/Reader/CborReaderState.cs`
- `System/Formats/Cbor/Writer/CborWriter.Array.cs`
- `System/Formats/Cbor/Writer/CborWriter.Integer.cs`
- `System/Formats/Cbor/Writer/CborWriter.Map.cs`
- `System/Formats/Cbor/Writer/CborWriter.Simple.cs`
- `System/Formats/Cbor/Writer/CborWriter.Simple.netcoreapp.cs`
- `System/Formats/Cbor/Writer/CborWriter.String.cs`
- `System/Formats/Cbor/Writer/CborWriter.Tag.cs`
- `System/Formats/Cbor/Writer/CborWriter.cs`
- `System/Formats/Cbor/Writer/CborWriterOptions.cs`

## Pinned shared helpers

- `src/libraries/Common/src/System/Memory/PointerMemoryManager.cs`

## Local adaptation

- `System/Formats/Cbor/SR.cs` contains the pinned English strings from
  `src/libraries/System.Formats.Cbor/src/Resources/Strings.resx` as constants and invariant formatting.
