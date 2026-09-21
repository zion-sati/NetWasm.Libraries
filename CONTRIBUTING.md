# Contributing

Changes should retain ordinary `NetWasm.Sdk` package boundaries, preserve the
documented upstream provenance, and avoid checkout-owned references to NetWasm
CoreLib or sibling implementation projects. Production dependencies belong in
the package DAG; TUnit is test-only.

Use the pinned .NET SDK. Run `eng/build-packages.sh`
before opening a pull request, then run the public test lane described in
[`docs/building-and-testing.md`](docs/building-and-testing.md).

## Maintainer releases

Publish a GitHub Release using a `vVERSION` tag targeted at a signed commit on
`main`. The GitHub Release tag sets the coordinated package version; the
release workflow validates the tagged source and exact package set before
trusted NuGet.org publishing. Publish matching core NetWasm and TUnit packages
first.
