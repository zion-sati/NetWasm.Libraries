namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal interface IActivationDescriptorLocator
{
    ActivationModel? Locate(IReadOnlyList<ActivationModel> activations, string identity);
}
