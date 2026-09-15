# NetWasm ported libraries

This repository contains `NetWasm.Sdk` library packages adapted from
the MIT-licensed .NET libraries. Each package targets `netwasm0.1`; applications
reference only the packages they use, preserving NetWasm's closed-world,
pay-for-what-you-use deployment model.

Try LINQ in a NetWasm application:

```sh
dotnet new install "NetWasm.Templates@*-*"
dotnet new netwasm-app -n HelloNetWasm -o HelloNetWasm
cd HelloNetWasm
dotnet add package NetWasm.System.Linq --prerelease
dotnet publish -c Release
```

`@*-*` selects the latest templates, including prereleases. `--prerelease`
includes experimental library releases.

The repository currently contains LINQ, Async LINQ, Memory, Pipelines,
Encodings.Web, HTTP, regular expressions, JSON, XML, hashing, and the
dependency-injection abstractions and container. Their exact supported
surfaces remain deliberately smaller than desktop .NET where APIs require
reflection, dynamic code generation, managed threads, file I/O, sockets, or
other platform services outside the current NetWasm profile.

See [building and testing](docs/building-and-testing.md),
[upstream provenance](UPSTREAM_PROVENANCE.md), and the
[license map](LICENSE-MAP.md).
