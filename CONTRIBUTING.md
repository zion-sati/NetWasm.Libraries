# Contributing

Changes should retain ordinary `NetWasm.Sdk` package boundaries, preserve the
documented upstream provenance, and avoid checkout-owned references to NetWasm
CoreLib or sibling implementation projects. Production dependencies belong in
the package DAG; TUnit is test-only.

Use the pinned .NET SDK and Node.js 24 or later. Run `eng/build-packages.sh`
before opening a pull request, then run the public test lane described in
[`docs/building-and-testing.md`](docs/building-and-testing.md).

## Maintainer releases

From a clean `main` checkout, with the public Git author and an approved signing
key configured, run `python3 eng/prepare-release.py --version VERSION`, replacing
`VERSION` with the next semantic version. It updates coordinated package versions,
creates a signed source commit/tag, and commits the matching release manifest.
No manifest editing is needed. Existing tags and unapproved author metadata are
rejected.

Review the output, push `main` and the printed tag atomically, then publish the
GitHub Release. Preparation does not build, test, push, or publish. The release
workflow validates the tagged source and exact package set before trusted
NuGet.org publishing. Publish the matching core NetWasm packages first.
