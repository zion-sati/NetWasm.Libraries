# NetWasm.System.Text.RegularExpressions

`NetWasm.System.Text.RegularExpressions` supplies the
`System.Text.RegularExpressions` assembly for the `netwasm0.1` profile. It is
an independently versioned ported-library package for applications that opt
into regular-expression matching.

The package carries the `NetWasm,Version=v0.1` asset and preserves the public
`System.Text.RegularExpressions` assembly identity. It also carries the
matching ordinary Roslyn source generator under `analyzers/dotnet/cs`, so a
consumer needs only the package reference and its `[GeneratedRegex]`
declarations; no manual analyzer path or NetWasm-specific generator setup is
required. It does not replace the desktop framework's built-in
`System.Text.RegularExpressions` reference.
