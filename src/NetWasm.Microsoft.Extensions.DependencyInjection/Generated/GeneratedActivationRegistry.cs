using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>
/// Receives immutable activation catalogs emitted by the package-owned source
/// generator before application code begins executing.
/// </summary>
public static class GeneratedActivationRegistry
{
    private static readonly List<GeneratedActivationManifest> Manifests = new();

    /// <summary>Registers one generated assembly catalog.</summary>
    public static void Register(GeneratedActivationManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        for (var index = 0; index < Manifests.Count; index++)
        {
            if (ReferenceEquals(Manifests[index], manifest))
            {
                return;
            }
        }

        Manifests.Add(manifest);
        PublishActivatorUtilitiesMetadata();
    }

    internal static IReadOnlyDictionary<(Type ServiceType, Type ImplementationType, object? ServiceKey), GeneratedActivationDescriptor>
        CaptureActivations()
    {
        var activations = new Dictionary<(Type, Type, object?), GeneratedActivationDescriptor>();
        for (var manifestIndex = 0; manifestIndex < Manifests.Count; manifestIndex++)
        {
            var manifest = Manifests[manifestIndex];
            for (var activationIndex = 0; activationIndex < manifest.Activations.Count; activationIndex++)
            {
                var activation = manifest.Activations[activationIndex];
                activations[(activation.ServiceType, activation.ImplementationType, activation.ServiceKey)] = activation;
            }
        }

        return activations;
    }

    internal static IReadOnlyDictionary<(Type ServiceType, Type ImplementationType, object? ServiceKey), IReadOnlyList<GeneratedActivationDescriptor>>
        CaptureOpenGenericActivations()
    {
        var grouped = new Dictionary<(Type, Type, object?), List<GeneratedActivationDescriptor>>();
        for (var manifestIndex = 0; manifestIndex < Manifests.Count; manifestIndex++)
        {
            var manifest = Manifests[manifestIndex];
            for (var activationIndex = 0; activationIndex < manifest.Activations.Count; activationIndex++)
            {
                var activation = manifest.Activations[activationIndex];
                if (activation.TemplateServiceType is not Type serviceType ||
                    activation.TemplateImplementationType is not Type implementationType)
                {
                    continue;
                }

                var key = (serviceType, implementationType, activation.TemplateServiceKey);
                if (!grouped.TryGetValue(key, out var activations))
                {
                    activations = new List<GeneratedActivationDescriptor>();
                    grouped[key] = activations;
                }

                activations.Add(activation);
            }
        }

        var result = new Dictionary<(Type, Type, object?), IReadOnlyList<GeneratedActivationDescriptor>>();
        foreach (var pair in grouped)
        {
            result[pair.Key] = pair.Value.AsReadOnly();
        }

        return result;
    }

    internal static HashSet<(
        Type ServiceType,
        Type ImplementationType,
        bool IsKeyedService,
        object? ServiceKey)> CaptureOpenGenericRegistrations()
    {
        var result = new HashSet<(
            Type ServiceType,
            Type ImplementationType,
            bool IsKeyedService,
            object? ServiceKey)>();
        for (var manifestIndex = 0; manifestIndex < Manifests.Count; manifestIndex++)
        {
            var registrations = Manifests[manifestIndex].OpenGenericRegistrations;
            for (var registrationIndex = 0;
                 registrationIndex < registrations.Count;
                 registrationIndex++)
            {
                var registration = registrations[registrationIndex];
                result.Add((
                    registration.ServiceType,
                    registration.ImplementationType,
                    registration.IsKeyedService,
                    registration.ServiceKey));
            }
        }

        return result;
    }

    private static void PublishActivatorUtilitiesMetadata()
    {
        var activations = new List<GeneratedActivationDefinition>();
        var sequences = new Dictionary<(Type SequenceType, object? ServiceKey), GeneratedSequenceDefinition>();
        for (var manifestIndex = 0; manifestIndex < Manifests.Count; manifestIndex++)
        {
            var manifest = Manifests[manifestIndex];
            for (var activationIndex = 0; activationIndex < manifest.Activations.Count; activationIndex++)
            {
                activations.Add(ToDefinition(manifest.Activations[activationIndex]));
            }

            for (var sequenceIndex = 0; sequenceIndex < manifest.Sequences.Count; sequenceIndex++)
            {
                var sequence = manifest.Sequences[sequenceIndex];
                sequences[(sequence.SequenceType, sequence.ServiceKey)] = new GeneratedSequenceDefinition(
                    sequence.SequenceType,
                    sequence.ElementType,
                    sequence.ServiceKey,
                    sequence.Materialize);
            }
        }

        GeneratedActivationMetadataSource.Register(activations, new List<GeneratedSequenceDefinition>(sequences.Values));
    }

    private static GeneratedActivationDefinition ToDefinition(GeneratedActivationDescriptor activation)
    {
        var parameterTypes = new Type[activation.Parameters.Count];
        var parameterKeys = new object?[activation.Parameters.Count];
        var lookupModes = new ServiceKeyLookupMode[activation.Parameters.Count];
        for (var index = 0; index < activation.Parameters.Count; index++)
        {
            var parameter = activation.Parameters[index];
            parameterTypes[index] = parameter.ParameterType;
            parameterKeys[index] = parameter.ServiceKey;
            lookupModes[index] = parameter.LookupMode;
        }

        var alternatives = new GeneratedActivationDefinition[activation.Alternatives.Count];
        for (var index = 0; index < activation.Alternatives.Count; index++)
        {
            alternatives[index] = ToDefinition(activation.Alternatives[index]);
        }

        return new GeneratedActivationDefinition(
            activation.Identity,
            activation.ServiceType,
            activation.ImplementationType,
            parameterTypes,
            alternatives,
            activation.IsPreferred,
            activation.AssignableTypes,
            parameterKeys,
            lookupModes);
    }
}
