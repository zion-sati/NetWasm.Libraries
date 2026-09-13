# Contributing

Changes should retain ordinary `NetWasm.Sdk` package boundaries, preserve the
documented upstream provenance, and avoid checkout-owned references to NetWasm
CoreLib or sibling implementation projects. Production dependencies belong in
the package DAG; TUnit is test-only.

Use the pinned .NET SDK and Node.js 24 or later. Run `eng/build-packages.sh`
before opening a pull request, then run the public test lane described in
[`docs/building-and-testing.md`](docs/building-and-testing.md).
