# NetWasm ported libraries

[![Publication](https://img.shields.io/github/actions/workflow/status/zion-sati/NetWasm.Libraries/release.yml?label=publish&event=release)](https://github.com/zion-sati/NetWasm.Libraries/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE-MAP.md)

This repository contains `NetWasm.Sdk` library packages adapted from
the MIT-licensed .NET libraries. Each package targets `net10.0-netwasm`;
applications reference only the packages they use, preserving NetWasm's
closed-world, pay-for-what-you-use deployment model. A project that still
targets the deprecated `netwasm0.1` uses the earlier packages built for it.

The libraries that ship with .NET itself (LINQ, Async LINQ, Memory, Pipelines,
Encodings.Web, HTTP and HTTP JSON, regular expressions, JSON, XML, Data.Common,
DataContract serialization and Net.Requests) come with `NetWasm.Sdk` from 0.14
as part of `NetWasm.App`, so applications use them without a package
reference. Their earlier `NetWasm.System.*` packages remain on NuGet for
applications on NetWasm 0.13 and earlier.

A `net10.0-netwasm` application uses Microsoft's own `System.IO.Hashing` and
`System.Formats.Cbor` packages from NuGet, so this repository no longer ports
them.

Try dependency injection in a NetWasm application:

```sh
dotnet new install NetWasm.Templates
dotnet new netwasm-app -n HelloNetWasm -o HelloNetWasm
cd HelloNetWasm
dotnet add package NetWasm.Microsoft.Extensions.DependencyInjection
dotnet publish -c Release
```

The repository contains the out-of-band Microsoft.Extensions libraries that
need NetWasm's own build: the dependency-injection abstractions and container,
generated configuration binding, Options, memory caching, HTTP client factory,
and logging with `LoggerMessage` source generation. Their exact supported
surfaces remain deliberately smaller than desktop .NET where APIs require
reflection, dynamic code generation, managed threads, file I/O, sockets, or
other platform services outside the current NetWasm profile.

Microsoft's own `Microsoft.Extensions.Primitives`,
`Microsoft.Extensions.Configuration` (with its abstractions and the
environment-variable, command-line and JSON providers) and
`Microsoft.Extensions.Caching.Abstractions` packages run on NetWasm unchanged,
so this repository no longer ports them and the libraries here depend on
Microsoft's packages.

Each library carries the identity of the assembly it ports: its name,
upstream's public key and an assembly version above Microsoft's. NuGet
packages compiled against Microsoft's assemblies, such as container extensions
built on `Microsoft.Extensions.DependencyInjection.Abstractions`, therefore use
the ports, even when they bring Microsoft's package into the application.

## Packages

[![NuGet: NetWasm.Microsoft.Extensions.DependencyInjection](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.DependencyInjection-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.DependencyInjection)
[![NuGet: NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration.Binder](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration.Binder-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration.Binder)
[![NuGet: NetWasm.Microsoft.Extensions.Options.ConfigurationExtensions](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Options.ConfigurationExtensions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Options.ConfigurationExtensions)
[![NuGet: NetWasm.Microsoft.Extensions.Caching.Memory](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Caching.Memory-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Caching.Memory)
[![NuGet: NetWasm.Microsoft.Extensions.Http](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Http-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Http)

See [building and testing](docs/building-and-testing.md),
[upstream provenance](UPSTREAM_PROVENANCE.md), and the
[license map](LICENSE-MAP.md).
