namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class ActivationDescriptorLocator : IActivationDescriptorLocator
{
    public ActivationModel? Locate(IReadOnlyList<ActivationModel> activations, string identity)
    {
        Guard.NotNull(activations, nameof(activations));
        Guard.NotNull(identity, nameof(identity));
        var byIdentity = new Dictionary<string, ActivationModel>(StringComparer.Ordinal);
        for (var index = 0; index < activations.Count; index++)
        {
            var activation = activations[index] ?? throw new ArgumentException("Generated activations cannot contain null entries.", nameof(activations));
            if (byIdentity.ContainsKey(activation.Facts.Identity))
            {
                throw new GeneratorDiagnosticException("NWDI005", "Generated activation identities must be unique.");
            }

            byIdentity.Add(activation.Facts.Identity, activation);
        }

        return byIdentity.TryGetValue(identity, out var located) ? located : null;
    }
}
