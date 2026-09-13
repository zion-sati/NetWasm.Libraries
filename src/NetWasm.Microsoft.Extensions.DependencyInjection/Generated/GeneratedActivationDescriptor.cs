using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Invokes one source-generated constructor or factory with already-resolved arguments.</summary>
public delegate object? GeneratedObjectFactory(IReadOnlyList<object?> arguments);

/// <summary>Describes one immutable, source-generated activation closure.</summary>
public sealed class GeneratedActivationDescriptor
{
    private readonly GeneratedParameter[] _parameters;
    private readonly IReadOnlyList<GeneratedParameter> _readOnlyParameters;

    public GeneratedActivationDescriptor(
        string identity,
        Type serviceType,
        Type implementationType,
        IReadOnlyList<GeneratedParameter> parameters,
        GeneratedObjectFactory activate)
        : this(identity, serviceType, implementationType, parameters, activate, Array.Empty<GeneratedActivationDescriptor>(), isPreferred: false, Array.Empty<Type>())
    {
    }

    public GeneratedActivationDescriptor(
        string identity,
        Type serviceType,
        Type implementationType,
        IReadOnlyList<GeneratedParameter> parameters,
        GeneratedObjectFactory activate,
        IReadOnlyList<GeneratedActivationDescriptor> alternatives,
        bool isPreferred = false)
        : this(identity, serviceType, implementationType, parameters, activate, alternatives, isPreferred, Array.Empty<Type>())
    {
    }

    public GeneratedActivationDescriptor(
        string identity,
        Type serviceType,
        Type implementationType,
        IReadOnlyList<GeneratedParameter> parameters,
        GeneratedObjectFactory activate,
        IReadOnlyList<GeneratedActivationDescriptor> alternatives,
        bool isPreferred,
        IReadOnlyList<Type> assignableTypes,
        object? serviceKey = null,
        string? diagnosticServiceType = null,
        string? diagnosticImplementationType = null,
        Type? templateServiceType = null,
        Type? templateImplementationType = null,
        object? templateServiceKey = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(activate);

        Identity = identity;
        ServiceType = serviceType;
        ImplementationType = implementationType;
        ServiceKey = serviceKey;
        if ((templateServiceType is null) != (templateImplementationType is null))
        {
            throw new ArgumentException("Generated open-generic template types must either both be present or both be absent.");
        }

        if (templateServiceType is null && templateServiceKey is not null)
        {
            throw new ArgumentException("A generated open-generic template key requires template service and implementation types.");
        }

        TemplateServiceType = templateServiceType;
        TemplateImplementationType = templateImplementationType;
        TemplateServiceKey = templateServiceKey;
        DiagnosticServiceType = diagnosticServiceType ?? identity;
        DiagnosticImplementationType = diagnosticImplementationType ?? identity;
        _parameters = new GeneratedParameter[parameters.Count];
        for (var index = 0; index < parameters.Count; index++)
        {
            _parameters[index] = parameters[index] ?? throw new ArgumentException("Generated activation parameters cannot contain null values.", nameof(parameters));
        }

        _readOnlyParameters = Array.AsReadOnly(_parameters);
        Activate = activate;
        IsPreferred = isPreferred;

        ArgumentNullException.ThrowIfNull(alternatives);
        var alternativeCopy = new GeneratedActivationDescriptor[alternatives.Count];
        for (var index = 0; index < alternatives.Count; index++)
        {
            var alternative = alternatives[index] ?? throw new ArgumentException("Generated activation alternatives cannot contain null values.", nameof(alternatives));
            if (alternative.ServiceType != serviceType || alternative.ImplementationType != implementationType)
            {
                throw new ArgumentException("Generated activation alternatives must match the primary service and implementation types.", nameof(alternatives));
            }

            alternativeCopy[index] = alternative;
        }

        Alternatives = Array.AsReadOnly(alternativeCopy);

        ArgumentNullException.ThrowIfNull(assignableTypes);
        var assignableCopy = new Type[assignableTypes.Count];
        for (var index = 0; index < assignableTypes.Count; index++)
        {
            assignableCopy[index] = assignableTypes[index] ?? throw new ArgumentException("Generated assignability metadata cannot contain null values.", nameof(assignableTypes));
        }

        AssignableTypes = Array.AsReadOnly(assignableCopy);
    }

    public string Identity { get; }

    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public object? ServiceKey { get; }

    /// <summary>The open service definition whose runtime descriptor this closed activation realizes.</summary>
    public Type? TemplateServiceType { get; }

    /// <summary>The open implementation definition whose runtime descriptor this closed activation realizes.</summary>
    public Type? TemplateImplementationType { get; }

    /// <summary>The key carried by the open runtime descriptor, which may differ from a concrete request key.</summary>
    public object? TemplateServiceKey { get; }

    /// <summary>Source-generated service type display used by deterministic diagnostics.</summary>
    public string DiagnosticServiceType { get; }

    /// <summary>Source-generated implementation type display used by deterministic diagnostics.</summary>
    public string DiagnosticImplementationType { get; }

    public IReadOnlyList<GeneratedParameter> Parameters => _readOnlyParameters;

    public GeneratedObjectFactory Activate { get; }

    public IReadOnlyList<GeneratedActivationDescriptor> Alternatives { get; }

    public bool IsPreferred { get; }

    public IReadOnlyList<Type> AssignableTypes { get; }
}

/// <summary>Describes one generated typed enumerable materializer.</summary>
public sealed class GeneratedSequenceDescriptor
{
    public GeneratedSequenceDescriptor(
        Type sequenceType,
        GeneratedEnumerableFactory materialize,
        object? serviceKey = null)
        : this(sequenceType, elementType: null, materialize, serviceKey)
    {
    }

    public GeneratedSequenceDescriptor(
        Type sequenceType,
        Type? elementType,
        GeneratedEnumerableFactory materialize,
        object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(sequenceType);
        if (elementType is not null)
        {
            ArgumentNullException.ThrowIfNull(elementType);
        }

        ArgumentNullException.ThrowIfNull(materialize);
        SequenceType = sequenceType;
        ElementType = elementType;
        Materialize = materialize;
        ServiceKey = serviceKey;
    }

    public Type SequenceType { get; }

    public object? ServiceKey { get; }

    public Type? ElementType { get; }

    public GeneratedEnumerableFactory Materialize { get; }
}

/// <summary>One generated constructor parameter and its static default policy.</summary>
public sealed class GeneratedParameter
{
    public GeneratedParameter(Type parameterType, bool hasDefaultValue = false, object? defaultValue = null)
        : this(parameterType, serviceKey: null, hasDefaultValue, defaultValue, ServiceKeyLookupMode.NullKey)
    {
    }

    public GeneratedParameter(
        Type parameterType,
        object? serviceKey,
        bool hasDefaultValue = false,
        object? defaultValue = null,
        ServiceKeyLookupMode lookupMode = ServiceKeyLookupMode.ExplicitKey)
    {
        ArgumentNullException.ThrowIfNull(parameterType);
        ParameterType = parameterType;
        ServiceKey = serviceKey;
        LookupMode = lookupMode;
        HasDefaultValue = hasDefaultValue;
        DefaultValue = defaultValue;
    }

    public Type ParameterType { get; }

    public object? ServiceKey { get; }

    public ServiceKeyLookupMode LookupMode { get; }

    public bool HasDefaultValue { get; }

    public object? DefaultValue { get; }
}

/// <summary>Immutable generated activation descriptors emitted for one closed program.</summary>
public sealed class GeneratedActivationManifest
{
    private readonly ReadOnlyCollection<GeneratedActivationDescriptor> _activations;
    private readonly ReadOnlyCollection<GeneratedSequenceDescriptor> _sequences;
    private readonly ReadOnlyCollection<GeneratedOpenGenericRegistrationDescriptor>
        _openGenericRegistrations;

    public GeneratedActivationManifest(IReadOnlyList<GeneratedActivationDescriptor> activations)
        : this(
            activations,
            Array.Empty<GeneratedSequenceDescriptor>(),
            Array.Empty<GeneratedOpenGenericRegistrationDescriptor>())
    {
    }

    public GeneratedActivationManifest(
        IReadOnlyList<GeneratedActivationDescriptor> activations,
        IReadOnlyList<GeneratedSequenceDescriptor> sequences)
        : this(
            activations,
            sequences,
            Array.Empty<GeneratedOpenGenericRegistrationDescriptor>())
    {
    }

    public GeneratedActivationManifest(
        IReadOnlyList<GeneratedActivationDescriptor> activations,
        IReadOnlyList<GeneratedSequenceDescriptor> sequences,
        IReadOnlyList<GeneratedOpenGenericRegistrationDescriptor>
            openGenericRegistrations)
    {
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentNullException.ThrowIfNull(sequences);
        ArgumentNullException.ThrowIfNull(openGenericRegistrations);
        var copy = new GeneratedActivationDescriptor[activations.Count];
        for (var index = 0; index < activations.Count; index++)
        {
            copy[index] = activations[index] ?? throw new ArgumentException("Generated activation manifests cannot contain null entries.", nameof(activations));
        }

        _activations = Array.AsReadOnly(copy);

        var sequenceCopy = new GeneratedSequenceDescriptor[sequences.Count];
        for (var index = 0; index < sequences.Count; index++)
        {
            sequenceCopy[index] = sequences[index] ?? throw new ArgumentException("Generated sequence manifests cannot contain null entries.", nameof(sequences));
        }

        _sequences = Array.AsReadOnly(sequenceCopy);

        var openGenericCopy = new GeneratedOpenGenericRegistrationDescriptor[
            openGenericRegistrations.Count];
        for (var index = 0; index < openGenericRegistrations.Count; index++)
        {
            openGenericCopy[index] = openGenericRegistrations[index]
                ?? throw new ArgumentException(
                    "Generated open-generic registration manifests cannot contain null entries.",
                    nameof(openGenericRegistrations));
        }

        _openGenericRegistrations = Array.AsReadOnly(openGenericCopy);
    }

    public IReadOnlyList<GeneratedActivationDescriptor> Activations => _activations;

    public IReadOnlyList<GeneratedSequenceDescriptor> Sequences => _sequences;

    public IReadOnlyList<GeneratedOpenGenericRegistrationDescriptor>
        OpenGenericRegistrations => _openGenericRegistrations;

}

/// <summary>
/// Identifies one source-generated open-generic registration template without
/// requiring runtime generic reflection.
/// </summary>
public sealed class GeneratedOpenGenericRegistrationDescriptor
{
    public GeneratedOpenGenericRegistrationDescriptor(
        Type serviceType,
        Type implementationType,
        bool isKeyedService,
        object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        ServiceType = serviceType;
        ImplementationType = implementationType;
        IsKeyedService = isKeyedService;
        ServiceKey = serviceKey;
    }

    public Type ServiceType { get; }

    public Type ImplementationType { get; }

    public bool IsKeyedService { get; }

    public object? ServiceKey { get; }
}
