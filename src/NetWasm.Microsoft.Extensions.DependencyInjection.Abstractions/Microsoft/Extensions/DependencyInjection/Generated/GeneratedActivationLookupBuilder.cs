using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

internal interface IGeneratedActivationLookupBuilder
{
    GeneratedActivationLookup Build(
        IReadOnlyList<GeneratedActivationDefinition> activations,
        IReadOnlyList<GeneratedSequenceDefinition>? sequences = null);
}

/// <summary>Builds one immutable metadata snapshot from generated activation definitions.</summary>
internal sealed class GeneratedActivationLookupBuilder : IGeneratedActivationLookupBuilder
{
    public GeneratedActivationLookup Build(
        IReadOnlyList<GeneratedActivationDefinition> activations,
        IReadOnlyList<GeneratedSequenceDefinition>? sequences = null)
    {
        ArgumentNullException.ThrowIfNull(activations);
        var registrations = new Dictionary<Type, GeneratedFactoryRegistration>();
        var assignability = new Dictionary<Type, HashSet<Type>>();
        var sequenceFactories = new Dictionary<GeneratedSequenceKey, GeneratedSequenceDefinition>();
        for (var index = 0; index < activations.Count; index++)
        {
            var activation = activations[index] ?? throw new ArgumentException("Generated activation metadata cannot contain null entries.", nameof(activations));
            var candidates = new GeneratedFactoryCandidate[1 + activation.Alternatives.Count];
            candidates[0] = CreateCandidate(activation);
            for (var alternativeIndex = 0; alternativeIndex < activation.Alternatives.Count; alternativeIndex++)
            {
                candidates[alternativeIndex + 1] = CreateCandidate(activation.Alternatives[alternativeIndex]);
            }

            registrations[activation.ImplementationType] = new GeneratedFactoryRegistration(Array.AsReadOnly(candidates));
            AddRelationship(assignability, activation.ImplementationType, activation.ImplementationType);
            AddRelationship(assignability, activation.ImplementationType, activation.ServiceType);
            for (var assignableIndex = 0; assignableIndex < activation.AssignableTypes.Count; assignableIndex++)
            {
                AddRelationship(assignability, activation.ImplementationType, activation.AssignableTypes[assignableIndex]);
            }
        }

        if (sequences is not null)
        {
            for (var index = 0; index < sequences.Count; index++)
            {
                var sequence = sequences[index] ?? throw new ArgumentException("Generated sequence metadata cannot contain null entries.", nameof(sequences));
                var key = new GeneratedSequenceKey(sequence.SequenceType, sequence.ServiceKey);
                if (!sequenceFactories.TryAdd(key, sequence))
                {
                    throw new ArgumentException("Generated sequence metadata contains a duplicate sequence identity.", nameof(sequences));
                }
            }
        }

        return new GeneratedActivationLookup(registrations, assignability, sequenceFactories);
    }

    private static GeneratedFactoryCandidate CreateCandidate(GeneratedActivationDefinition activation) =>
        new(activation.Identity, activation.ParameterTypes, activation.IsPreferred);

    private static void AddRelationship(
        Dictionary<Type, HashSet<Type>> assignability,
        Type suppliedType,
        Type expectedType)
    {
        ArgumentNullException.ThrowIfNull(expectedType);
        if (!assignability.TryGetValue(suppliedType, out var existing))
        {
            existing = new HashSet<Type>();
            assignability[suppliedType] = existing;
        }

        existing.Add(expectedType);
    }
}
