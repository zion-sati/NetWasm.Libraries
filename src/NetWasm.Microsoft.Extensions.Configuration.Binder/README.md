# NetWasm.Microsoft.Extensions.Configuration.Binder

Closed-world generated configuration binding for `netwasm0.1`. The bundled
generator discovers closed `ConfigurationBinder.Get<T>`, `Bind<T>`, and options
configuration calls and emits direct constructor/property assignments for
scalar, nullable, nested-object, and array/list shapes. Dynamic reflection
binding is rejected explicitly.
