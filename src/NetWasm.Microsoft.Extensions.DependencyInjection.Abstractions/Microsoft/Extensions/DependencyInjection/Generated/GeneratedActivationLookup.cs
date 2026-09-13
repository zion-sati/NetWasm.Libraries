using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Immutable generated type/constructor metadata captured at application type initialization.</summary>
internal sealed class GeneratedActivationLookup
{
    private readonly IReadOnlyDictionary<Type, GeneratedFactoryRegistration> _registrations;
    private readonly IReadOnlyDictionary<Type, IReadOnlySet<Type>> _assignability;
    private readonly IReadOnlyDictionary<GeneratedSequenceKey, GeneratedSequenceDefinition> _sequences;

    internal GeneratedActivationLookup(
        IReadOnlyDictionary<Type, GeneratedFactoryRegistration> registrations,
        IReadOnlyDictionary<Type, HashSet<Type>> assignability,
        IReadOnlyDictionary<GeneratedSequenceKey, GeneratedSequenceDefinition>? sequences = null)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(assignability);
        sequences ??= new Dictionary<GeneratedSequenceKey, GeneratedSequenceDefinition>();
        var registrationCopy = new Dictionary<Type, GeneratedFactoryRegistration>(registrations);
        var assignabilityCopy = new Dictionary<Type, IReadOnlySet<Type>>();
        foreach (var pair in assignability)
        {
            assignabilityCopy.Add(pair.Key, new HashSet<Type>(pair.Value));
        }

        _registrations = new ReadOnlyDictionary<Type, GeneratedFactoryRegistration>(registrationCopy);
        _assignability = new ReadOnlyDictionary<Type, IReadOnlySet<Type>>(assignabilityCopy);
        _sequences = new ReadOnlyDictionary<GeneratedSequenceKey, GeneratedSequenceDefinition>(
            new Dictionary<GeneratedSequenceKey, GeneratedSequenceDefinition>(sequences));
    }

    internal IReadOnlyDictionary<Type, GeneratedFactoryRegistration> Registrations => _registrations;

    internal IReadOnlyDictionary<Type, IReadOnlySet<Type>> Assignability => _assignability;

    internal IReadOnlyDictionary<GeneratedSequenceKey, GeneratedSequenceDefinition> Sequences => _sequences;

}
