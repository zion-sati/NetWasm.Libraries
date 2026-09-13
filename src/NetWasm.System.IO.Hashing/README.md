# NetWasm.System.IO.Hashing

`NetWasm.System.IO.Hashing` supplies the `System.IO.Hashing` assembly for the
`netwasm0.1` profile. It is an independently versioned ported-library package
for applications that opt into the NetWasm hashing surface.

The package carries the `NetWasm,Version=v0.1` asset and transitively supplies
the exact `NetWasm.System.Memory` support package required by the hashing
implementation. It supplies no desktop target asset; desktop consumers retain
their normal framework or package selection.
