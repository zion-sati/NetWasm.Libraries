# NetWasm.Microsoft.Extensions.Configuration.Binder

Closed-world generated configuration binding for NetWasm (`net10.0-netwasm`). The bundled
generator discovers closed `ConfigurationBinder.Get<T>`, `Bind<T>`, and options
configuration calls and emits direct constructor/property assignments for
scalar, nullable, nested-object, array/list and dictionary shapes. A value
converts as upstream's binder converts it through the type's `TypeConverter`:
numbers (also hexadecimal integers), `bool`, `char`, enums by name, number or
flag list, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`,
`Guid`, `Uri`, `Version` and base64 `byte[]`; a failure is upstream's
`InvalidOperationException` naming the value, its path and the type.
`GetValue<T>` converts the same types, and enums the generator sees it read.
Dynamic reflection binding is rejected explicitly.
