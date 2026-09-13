// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal readonly struct ServiceIdentifier : IEquatable<ServiceIdentifier>
{
    internal ServiceIdentifier(Type serviceType, object? serviceKey)
    {
        ServiceType = serviceType;
        ServiceKey = serviceKey;
    }

    internal Type ServiceType { get; }

    internal object? ServiceKey { get; }

    internal bool IsKeyed => ServiceKey is not null;

    internal static ServiceIdentifier FromServiceType(Type serviceType) => new(serviceType, null);

    internal static ServiceIdentifier FromKeyedServiceType(Type serviceType, object? serviceKey) =>
        new(serviceType, serviceKey);

    public bool Equals(ServiceIdentifier other) =>
        ServiceType == other.ServiceType && object.Equals(ServiceKey, other.ServiceKey);

    public override bool Equals(object? obj) => obj is ServiceIdentifier other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ServiceType, ServiceKey);
}
