# NetWasm.Microsoft.Extensions.Options

The source-generation-friendly options surface used by NetWasm extensions.

Options creation is compile-time: the closed public parameterless constructor is
rewritten by the NetWasm compiler, with no runtime constructor discovery.
Configuration, post-configuration, validation, named snapshots and change-token
monitor invalidation are supported; runtime reflection binding and filesystem
reload hosting remain outside this package.
