# Building and testing

Install the .NET SDK pinned by `global.json`, Node.js 24 or later, and the
prerequisites documented by the core NetWasm quickstart. Normal package and SDK
restore uses NuGet.org.

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
