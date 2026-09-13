// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Xml.Serialization;

public readonly struct XmlSerializerContractKey : IEquatable<XmlSerializerContractKey>
{
    public XmlSerializerContractKey(string typeName, string? namespaceUri = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        TypeName = typeName;
        Namespace = namespaceUri ?? string.Empty;
    }

    public string TypeName { get; }
    public string Namespace { get; }

    public bool Equals(XmlSerializerContractKey other) =>
        string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) && string.Equals(Namespace, other.Namespace, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is XmlSerializerContractKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(TypeName, Namespace);
}

public interface IXmlSerializerGeneratedContract
{
    XmlSerializer CreateSerializer();
}

public readonly struct XmlSerializerContractRegistration
{
    public XmlSerializerContractRegistration(XmlSerializerContractKey key, Func<XmlSerializer> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Key = key;
        Factory = factory;
    }

    public XmlSerializerContractKey Key { get; }
    public Func<XmlSerializer> Factory { get; }
}

public interface IXmlSerializerContractCatalog
{
    XmlSerializerContractRegistration? Find(XmlSerializerContractKey key);
}

public sealed class XmlSerializerContractCatalog : IXmlSerializerContractCatalog
{
    private readonly Dictionary<XmlSerializerContractKey, XmlSerializerContractRegistration> _registrations;

    public XmlSerializerContractCatalog(IReadOnlyList<XmlSerializerContractRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        _registrations = new Dictionary<XmlSerializerContractKey, XmlSerializerContractRegistration>();
        for (var index = 0; index < registrations.Count; index++)
        {
            var registration = registrations[index];
            if (!_registrations.TryAdd(registration.Key, registration))
            {
                throw new InvalidOperationException("A generated XML serializer contract key is registered more than once.");
            }
        }
    }

    public XmlSerializerContractRegistration? Find(XmlSerializerContractKey key) =>
        _registrations.TryGetValue(key, out var registration) ? registration : null;
}

public readonly struct XmlSerializerGenerationRequest
{
    public XmlSerializerGenerationRequest(IReadOnlyList<XmlSerializerContractKey> contracts, string generatorVersion)
    {
        ArgumentNullException.ThrowIfNull(contracts);
        ArgumentException.ThrowIfNullOrEmpty(generatorVersion);
        Contracts = contracts;
        GeneratorVersion = generatorVersion;
    }

    public IReadOnlyList<XmlSerializerContractKey> Contracts { get; }
    public string GeneratorVersion { get; }
}

public sealed class XmlSerializerGenerationManifest
{
    public XmlSerializerGenerationManifest(string generatorVersion, IReadOnlyList<XmlSerializerContractKey> contracts)
    {
        GeneratorVersion = generatorVersion;
        Contracts = contracts;
    }

    public string GeneratorVersion { get; }
    public IReadOnlyList<XmlSerializerContractKey> Contracts { get; }
}

public interface IXmlSerializerGenerationStep
{
    XmlSerializerGenerationManifest Generate(XmlSerializerGenerationRequest request);
}

public sealed class XmlSerializerGenerationStep : IXmlSerializerGenerationStep
{
    public XmlSerializerGenerationManifest Generate(XmlSerializerGenerationRequest request)
    {
        if (!string.Equals(request.GeneratorVersion, "10.0.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The XML serializer generator pin does not match the qualified development tool.");
        }

        if (request.Contracts.Count == 0)
        {
            throw new InvalidOperationException("A generated XML serializer manifest must contain at least one closed contract.");
        }

        var contracts = new List<XmlSerializerContractKey>(request.Contracts.Count);
        for (var index = 0; index < request.Contracts.Count; index++)
        {
            var contract = request.Contracts[index];
            if (contracts.Contains(contract))
            {
                throw new InvalidOperationException("A generated XML serializer manifest contains a duplicate contract key.");
            }

            contracts.Add(contract);
        }

        return new XmlSerializerGenerationManifest(request.GeneratorVersion, contracts.ToArray());
    }
}
