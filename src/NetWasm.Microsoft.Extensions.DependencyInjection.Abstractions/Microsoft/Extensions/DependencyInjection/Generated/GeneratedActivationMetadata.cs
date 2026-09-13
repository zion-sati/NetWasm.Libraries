using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>One immutable generated constructor shape.</summary>
internal sealed class GeneratedFactoryCandidate
{
    internal GeneratedFactoryCandidate(string identity, IReadOnlyList<Type> parameterTypes, bool isPreferred)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        Identity = identity;
        var copy = new Type[parameterTypes.Count];
        for (var index = 0; index < parameterTypes.Count; index++)
        {
            copy[index] = parameterTypes[index] ?? throw new ArgumentException("Generated factory parameter types cannot contain null values.", nameof(parameterTypes));
        }

        ParameterTypes = Array.AsReadOnly(copy);
        IsPreferred = isPreferred;
    }

    internal string Identity { get; }

    internal IReadOnlyList<Type> ParameterTypes { get; }

    internal bool IsPreferred { get; }
}

internal sealed class GeneratedFactoryRegistration
{
    internal GeneratedFactoryRegistration(IReadOnlyList<GeneratedFactoryCandidate> candidates)
    {
        Candidates = candidates;
    }

    internal IReadOnlyList<GeneratedFactoryCandidate> Candidates { get; }
}

internal sealed class GeneratedFactorySelection
{
    internal GeneratedFactorySelection(string identity)
    {
        Identity = identity;
    }

    internal string Identity { get; }
}

internal readonly struct GeneratedSequenceKey : IEquatable<GeneratedSequenceKey>
{
    internal GeneratedSequenceKey(Type sequenceType, object? serviceKey)
    {
        SequenceType = sequenceType;
        ServiceKey = serviceKey;
    }

    internal Type SequenceType { get; }

    internal object? ServiceKey { get; }

    public bool Equals(GeneratedSequenceKey other) =>
        SequenceType == other.SequenceType && object.Equals(ServiceKey, other.ServiceKey);

    public override bool Equals(object? obj) => obj is GeneratedSequenceKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SequenceType, ServiceKey);
}

internal sealed class GeneratedSequenceDefinition
{
    internal GeneratedSequenceDefinition(Type sequenceType, Type? elementType, object? serviceKey, GeneratedEnumerableFactory materialize)
    {
        ArgumentNullException.ThrowIfNull(sequenceType);
        ArgumentNullException.ThrowIfNull(materialize);
        SequenceType = sequenceType;
        ElementType = elementType;
        ServiceKey = serviceKey;
        Materialize = materialize;
    }

    internal Type SequenceType { get; }

    internal Type? ElementType { get; }

    internal object? ServiceKey { get; }

    internal GeneratedEnumerableFactory Materialize { get; }
}
