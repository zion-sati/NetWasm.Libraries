using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Closed generated constructor metadata crossing the runtime bridge.</summary>
internal sealed class GeneratedActivationDefinition
{
    internal GeneratedActivationDefinition(
        string identity,
        Type serviceType,
        Type implementationType,
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<GeneratedActivationDefinition> alternatives,
        bool isPreferred,
        IReadOnlyList<Type> assignableTypes,
        IReadOnlyList<object?>? parameterKeys = null,
        IReadOnlyList<ServiceKeyLookupMode>? parameterLookupModes = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(alternatives);
        ArgumentNullException.ThrowIfNull(assignableTypes);
        Identity = identity;
        ServiceType = serviceType;
        ImplementationType = implementationType;
        ParameterTypes = CopyTypes(parameterTypes, nameof(parameterTypes));
        ParameterKeys = CopyObjects(parameterKeys ?? new object?[ParameterTypes.Count], nameof(parameterKeys));
        ParameterLookupModes = CopyLookupModes(
            parameterLookupModes ?? new ServiceKeyLookupMode[ParameterTypes.Count],
            nameof(parameterLookupModes));
        if (ParameterKeys.Count != ParameterTypes.Count || ParameterLookupModes.Count != ParameterTypes.Count)
        {
            throw new ArgumentException("Generated parameter key metadata must match the generated parameter types.", nameof(parameterKeys));
        }

        Alternatives = CopyDefinitions(alternatives);
        IsPreferred = isPreferred;
        AssignableTypes = CopyTypes(assignableTypes, nameof(assignableTypes));
    }

    internal string Identity { get; }

    internal Type ServiceType { get; }

    internal Type ImplementationType { get; }

    internal IReadOnlyList<Type> ParameterTypes { get; }

    internal IReadOnlyList<object?> ParameterKeys { get; }

    internal IReadOnlyList<ServiceKeyLookupMode> ParameterLookupModes { get; }

    internal IReadOnlyList<GeneratedActivationDefinition> Alternatives { get; }

    internal bool IsPreferred { get; }

    internal IReadOnlyList<Type> AssignableTypes { get; }

    private static ReadOnlyCollection<Type> CopyTypes(IReadOnlyList<Type> types, string parameterName)
    {
        var copy = new Type[types.Count];
        for (var index = 0; index < types.Count; index++)
        {
            copy[index] = types[index] ?? throw new ArgumentException("Generated metadata type lists cannot contain null values.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }

    private static ReadOnlyCollection<object?> CopyObjects(IReadOnlyList<object?> values, string parameterName)
    {
        var copy = new object?[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            copy[index] = values[index];
        }

        return Array.AsReadOnly(copy);
    }

    private static ReadOnlyCollection<ServiceKeyLookupMode> CopyLookupModes(
        IReadOnlyList<ServiceKeyLookupMode> modes,
        string parameterName)
    {
        var copy = new ServiceKeyLookupMode[modes.Count];
        for (var index = 0; index < modes.Count; index++)
        {
            if (modes[index] is not (ServiceKeyLookupMode.ExplicitKey or ServiceKeyLookupMode.NullKey or ServiceKeyLookupMode.InheritKey))
            {
                throw new ArgumentException("Generated parameter key metadata contains an unsupported lookup mode.", parameterName);
            }

            copy[index] = modes[index];
        }

        return Array.AsReadOnly(copy);
    }

    private static ReadOnlyCollection<GeneratedActivationDefinition> CopyDefinitions(
        IReadOnlyList<GeneratedActivationDefinition> definitions)
    {
        var copy = new GeneratedActivationDefinition[definitions.Count];
        for (var index = 0; index < definitions.Count; index++)
        {
            copy[index] = definitions[index] ?? throw new ArgumentException("Generated metadata alternatives cannot contain null values.", nameof(definitions));
        }

        return Array.AsReadOnly(copy);
    }
}
