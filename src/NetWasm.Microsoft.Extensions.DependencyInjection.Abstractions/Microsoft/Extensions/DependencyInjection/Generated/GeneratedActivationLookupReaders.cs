using System;

namespace Microsoft.Extensions.DependencyInjection.Generated;

internal sealed class GeneratedActivationRegistrationReader : IGeneratedActivationRegistrationReader
{
    private readonly GeneratedActivationLookup _lookup;

    internal GeneratedActivationRegistrationReader(GeneratedActivationLookup lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public bool TryGetRegistration(Type implementationType, out GeneratedFactoryRegistration registration) =>
        _lookup.Registrations.TryGetValue(implementationType, out registration!);
}

internal sealed class GeneratedActivationAssignabilityReader : IGeneratedActivationAssignabilityReader
{
    private readonly GeneratedActivationLookup _lookup;

    internal GeneratedActivationAssignabilityReader(GeneratedActivationLookup lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public bool IsAssignable(Type suppliedType, Type expectedType)
    {
        ArgumentNullException.ThrowIfNull(suppliedType);
        ArgumentNullException.ThrowIfNull(expectedType);
        if (suppliedType == expectedType)
        {
            return true;
        }

        return _lookup.Assignability.TryGetValue(suppliedType, out var expectedTypes) && expectedTypes.Contains(expectedType);
    }
}
