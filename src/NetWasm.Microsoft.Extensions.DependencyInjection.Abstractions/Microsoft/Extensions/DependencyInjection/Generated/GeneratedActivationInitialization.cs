using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

internal interface IGeneratedActivationInitializer
{
    GeneratedActivationLookup Initialize(IReadOnlyList<GeneratedActivationDefinition> activations);

    GeneratedActivationLookup Initialize(
        IReadOnlyList<GeneratedActivationDefinition> activations,
        IReadOnlyList<GeneratedSequenceDefinition> sequences);
}

/// <summary>Builds one immutable generated lookup snapshot from generated metadata.</summary>
internal sealed class GeneratedActivationInitializer : IGeneratedActivationInitializer
{
    private readonly IGeneratedActivationLookupBuilder _builder;

    internal GeneratedActivationInitializer(IGeneratedActivationLookupBuilder builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public GeneratedActivationLookup Initialize(IReadOnlyList<GeneratedActivationDefinition> activations)
        => Initialize(activations, Array.Empty<GeneratedSequenceDefinition>());

    public GeneratedActivationLookup Initialize(
        IReadOnlyList<GeneratedActivationDefinition> activations,
        IReadOnlyList<GeneratedSequenceDefinition> sequences)
    {
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentNullException.ThrowIfNull(sequences);
        return _builder.Build(activations, sequences);
    }
}
