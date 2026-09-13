using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

internal interface IGeneratedFactoryShapeSelector
{
    GeneratedFactorySelection Select(Type implementationType, IReadOnlyList<Type> suppliedTypes);
}

internal interface IGeneratedActivationRegistrationReader
{
    bool TryGetRegistration(Type implementationType, out GeneratedFactoryRegistration registration);
}

internal interface IGeneratedActivationAssignabilityReader
{
    bool IsAssignable(Type suppliedType, Type expectedType);
}

/// <summary>Selects one generated constructor shape from static declared argument types.</summary>
internal sealed class GeneratedFactoryShapeSelector : IGeneratedFactoryShapeSelector
{
    private readonly IGeneratedActivationRegistrationReader _registrationReader;
    private readonly IGeneratedActivationAssignabilityReader _assignabilityReader;

    internal GeneratedFactoryShapeSelector(
        IGeneratedActivationRegistrationReader registrationReader,
        IGeneratedActivationAssignabilityReader assignabilityReader)
    {
        _registrationReader = registrationReader ?? throw new ArgumentNullException(nameof(registrationReader));
        _assignabilityReader = assignabilityReader ?? throw new ArgumentNullException(nameof(assignabilityReader));
    }

    public GeneratedFactorySelection Select(Type implementationType, IReadOnlyList<Type> suppliedTypes)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(suppliedTypes);
        if (!_registrationReader.TryGetRegistration(implementationType, out var registration))
        {
            throw new ArgumentException("No generated activation metadata is initialized for the requested implementation type.", nameof(implementationType));
        }

        string? shapeError = null;
        GeneratedFactoryCandidate? selected = null;
        var selectedParameterCount = -1;
        for (var index = 0; index < registration.Candidates.Count; index++)
        {
            var candidate = registration.Candidates[index];
            if (!TryMatch(_assignabilityReader, candidate.ParameterTypes, suppliedTypes, out var error))
            {
                shapeError ??= error;
                if (candidate.IsPreferred)
                {
                    break;
                }

                continue;
            }

            if (selected is not null)
            {
                if (selectedParameterCount == candidate.ParameterTypes.Count)
                {
                    throw new InvalidOperationException("Multiple generated constructors are applicable for the supplied factory shape.");
                }

                break;
            }

            selected = candidate;
            selectedParameterCount = candidate.ParameterTypes.Count;
            if (candidate.IsPreferred)
            {
                break;
            }
        }

        if (selected is not null)
        {
            return new GeneratedFactorySelection(selected.Identity);
        }

        throw new ArgumentException(shapeError!, nameof(suppliedTypes));
    }

    private static bool TryMatch(
        IGeneratedActivationAssignabilityReader assignabilityReader,
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<Type> suppliedTypes,
        out string error)
    {
        error = string.Empty;
        if (suppliedTypes.Count > parameterTypes.Count)
        {
            error = "The supplied factory shape has more arguments than any generated constructor.";
            return false;
        }

        var used = new bool[parameterTypes.Count];
        for (var suppliedIndex = 0; suppliedIndex < suppliedTypes.Count; suppliedIndex++)
        {
            var suppliedType = suppliedTypes[suppliedIndex];
            var match = -1;
            var matches = 0;
            for (var parameterIndex = 0; parameterIndex < parameterTypes.Count; parameterIndex++)
            {
                if (used[parameterIndex])
                {
                    continue;
                }

                if (assignabilityReader.IsAssignable(suppliedType, parameterTypes[parameterIndex]))
                {
                    match = parameterIndex;
                    matches++;
                }
            }

            if (matches == 0)
            {
                error = $"No generated constructor parameter accepts supplied type '{suppliedType}'.";
                return false;
            }

            if (matches > 1)
            {
                error = $"Supplied type '{suppliedType}' is ambiguous across generated constructor parameter positions.";
                return false;
            }

            used[match] = true;
        }

        return true;
    }
}
