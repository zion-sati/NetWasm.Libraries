namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal interface IGeneratedCatalogRegistrationEmitter
{
    string Emit();
}

/// <summary>Emits the ordinary module initializer that publishes one immutable assembly catalog.</summary>
internal sealed class GeneratedCatalogRegistrationEmitter : IGeneratedCatalogRegistrationEmitter
{
    public string Emit() =>
        """
        #nullable enable
        namespace Generated;
        internal static class NetWasmGeneratedActivationCatalogRegistration
        {
            [global::System.Runtime.CompilerServices.ModuleInitializer]
            internal static void Register()
            {
                global::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistry.Register(
                    GeneratedActivationManifestSource.Manifest);
            }
        }
        """.Replace("\r\n", "\n").Replace("\r", "\n");
}
