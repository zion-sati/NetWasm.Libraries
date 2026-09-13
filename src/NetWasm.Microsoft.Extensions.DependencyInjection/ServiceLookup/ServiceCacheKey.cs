// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal readonly struct ServiceCacheKey : IEquatable<ServiceCacheKey>
{
    internal ServiceCacheKey(ServiceIdentifier serviceIdentifier, int slot)
    {
        ServiceIdentifier = serviceIdentifier;
        Slot = slot;
    }

    internal ServiceIdentifier ServiceIdentifier { get; }

    internal int Slot { get; }

    public bool Equals(ServiceCacheKey other) => ServiceIdentifier.Equals(other.ServiceIdentifier) && Slot == other.Slot;

    public override bool Equals(object? obj) => obj is ServiceCacheKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ServiceIdentifier, Slot);
}
