# NetWasm ported libraries

[![Publication](https://img.shields.io/github/actions/workflow/status/zion-sati/NetWasm.Libraries/release.yml?label=publish&event=release)](https://github.com/zion-sati/NetWasm.Libraries/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE-MAP.md)

This repository contains `NetWasm.Sdk` library packages adapted from
the MIT-licensed .NET libraries. Each package targets `netwasm0.1`; applications
reference only the packages they use, preserving NetWasm's closed-world,
pay-for-what-you-use deployment model.

Try LINQ in a NetWasm application:

```sh
dotnet new install NetWasm.Templates
dotnet new netwasm-app -n HelloNetWasm -o HelloNetWasm
cd HelloNetWasm
dotnet add package NetWasm.System.Linq
dotnet publish -c Release
```

The repository currently contains LINQ, Async LINQ, Memory, Pipelines,
Encodings.Web, HTTP/HTTP JSON, primitives, configuration providers and
generated binding, Options, memory caching, logging, and the
dependency-injection abstractions and container. Their exact supported
surfaces remain deliberately smaller than desktop .NET where APIs require
reflection, dynamic code generation, managed threads, file I/O, sockets, or
other platform services outside the current NetWasm profile.

## Packages

[![NuGet: NetWasm.Microsoft.Extensions.DependencyInjection](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.DependencyInjection-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.DependencyInjection)
[![NuGet: NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions)
[![NuGet: NetWasm.System.IO.Hashing](https://img.shields.io/badge/NuGet-NetWasm.System.IO.Hashing-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.IO.Hashing)
[![NuGet: NetWasm.System.IO.Pipelines](https://img.shields.io/badge/NuGet-NetWasm.System.IO.Pipelines-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.IO.Pipelines)
[![NuGet: NetWasm.System.Linq](https://img.shields.io/badge/NuGet-NetWasm.System.Linq-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Linq)
[![NuGet: NetWasm.System.Linq.AsyncEnumerable](https://img.shields.io/badge/NuGet-NetWasm.System.Linq.AsyncEnumerable-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Linq.AsyncEnumerable)
[![NuGet: NetWasm.System.Memory](https://img.shields.io/badge/NuGet-NetWasm.System.Memory-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Memory)
[![NuGet: NetWasm.System.Net.Http](https://img.shields.io/badge/NuGet-NetWasm.System.Net.Http-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Net.Http)
[![NuGet: NetWasm.System.Net.Http.Json](https://img.shields.io/badge/NuGet-NetWasm.System.Net.Http.Json-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Net.Http.Json)
[![NuGet: NetWasm.Microsoft.Extensions.Primitives](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Primitives-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Primitives)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration.Abstractions](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration.Abstractions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration.Abstractions)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration.EnvironmentVariables](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration.EnvironmentVariables-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration.EnvironmentVariables)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration.CommandLine](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration.CommandLine-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration.CommandLine)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration.Json](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration.Json-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration.Json)
[![NuGet: NetWasm.Microsoft.Extensions.Configuration.Binder](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Configuration.Binder-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Configuration.Binder)
[![NuGet: NetWasm.Microsoft.Extensions.Options.ConfigurationExtensions](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Options.ConfigurationExtensions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Options.ConfigurationExtensions)
[![NuGet: NetWasm.Microsoft.Extensions.Caching.Abstractions](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Caching.Abstractions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Caching.Abstractions)
[![NuGet: NetWasm.Microsoft.Extensions.Caching.Memory](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Caching.Memory-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Caching.Memory)
[![NuGet: NetWasm.Microsoft.Extensions.Http](https://img.shields.io/badge/NuGet-NetWasm.Microsoft.Extensions.Http-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.Microsoft.Extensions.Http)
[![NuGet: NetWasm.System.Text.Encodings.Web](https://img.shields.io/badge/NuGet-NetWasm.System.Text.Encodings.Web-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Text.Encodings.Web)
[![NuGet: NetWasm.System.Text.Json](https://img.shields.io/badge/NuGet-NetWasm.System.Text.Json-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Text.Json)
[![NuGet: NetWasm.System.Text.RegularExpressions](https://img.shields.io/badge/NuGet-NetWasm.System.Text.RegularExpressions-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Text.RegularExpressions)
[![NuGet: NetWasm.System.Xml](https://img.shields.io/badge/NuGet-NetWasm.System.Xml-004880?logo=nuget)](https://www.nuget.org/packages/NetWasm.System.Xml)

See [building and testing](docs/building-and-testing.md),
[upstream provenance](UPSTREAM_PROVENANCE.md), and the
[license map](LICENSE-MAP.md).
