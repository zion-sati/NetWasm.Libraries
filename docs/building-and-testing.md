# Building and testing

Install the .NET SDK pinned by `global.json`. Restoring `NetWasm.Sdk`
supplies the matching native build tools.

Build the source package graph in dependency order with:

```sh
eng/build-packages.sh
```

The script builds root packages first, then builds dependent packages against
the exact outputs produced earlier in the same run.

After the matching NetWasm and TUnit packages are available on NuGet.org, run:

```sh
eng/test.sh
```

The tests compare desktop behavior where applicable and execute the same suites
through the `netwasm0.1` target.

Set `NETWASM_LIBRARIES_TEST_SHARD=<index>/<count>` to run one part of the suite:
`2/3` runs every third test project starting with the second. CI runs three
shards on each supported host (linux-x64, linux-arm64, osx-arm64, win-x64 and
win-arm64).
