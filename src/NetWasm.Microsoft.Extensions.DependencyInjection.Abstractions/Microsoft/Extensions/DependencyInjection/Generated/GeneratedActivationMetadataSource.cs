using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Provides the closed, source-generated metadata snapshot for this application.</summary>
internal static partial class GeneratedActivationMetadataSource
{
    private static readonly List<GeneratedActivationDefinition> Activations = new();
    private static readonly List<GeneratedSequenceDefinition> Sequences = new();
    private static GeneratedActivationLookup _lookup = CreateLookup();

    internal static GeneratedActivationLookup Lookup => _lookup;

    internal static void Register(
        IReadOnlyList<GeneratedActivationDefinition> activations,
        IReadOnlyList<GeneratedSequenceDefinition> sequences)
    {
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentNullException.ThrowIfNull(sequences);
#pragma warning disable CA1859 // The runtime bridge is intentionally composed through its narrow capability interface.
        for (var index = 0; index < activations.Count; index++)
        {
            var activation = activations[index];
            var exists = false;
            for (var existingIndex = 0; existingIndex < Activations.Count; existingIndex++)
            {
                if (Activations[existingIndex].Identity == activation.Identity)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                Activations.Add(activation);
            }
        }

        for (var index = 0; index < sequences.Count; index++)
        {
            var sequence = sequences[index];
            var exists = false;
            for (var existingIndex = 0; existingIndex < Sequences.Count; existingIndex++)
            {
                var existing = Sequences[existingIndex];
                if (existing.SequenceType == sequence.SequenceType &&
                    Equals(existing.ServiceKey, sequence.ServiceKey))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                Sequences.Add(sequence);
            }
        }

        IGeneratedActivationInitializer initializer =
            new GeneratedActivationInitializer(new GeneratedActivationLookupBuilder());
#pragma warning restore CA1859
        _lookup = initializer.Initialize(Activations, Sequences);
    }

    private static GeneratedActivationLookup CreateLookup()
    {
        Populate(Activations, Sequences);
#pragma warning disable CA1859 // The runtime bridge is intentionally composed through its narrow capability interface.
        IGeneratedActivationInitializer initializer =
            new GeneratedActivationInitializer(new GeneratedActivationLookupBuilder());
#pragma warning restore CA1859
        return initializer.Initialize(Activations, Sequences);
    }

    static partial void Populate(
        List<GeneratedActivationDefinition> activations,
        List<GeneratedSequenceDefinition> sequences);
}
