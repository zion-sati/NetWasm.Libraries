// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Generated;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class ConstructorCallSite : ServiceCallSite
{
    internal ConstructorCallSite(
        Type serviceType,
        Type implementationType,
        GeneratedActivationDescriptor activation,
        ServiceCallSite?[] parameterCallSites,
        int[] suppliedParameterIndexes,
        ResultCache cache)
        : base(cache, serviceType)
    {
        ImplementationType = implementationType;
        Activation = activation;
        ParameterCallSites = parameterCallSites;
        SuppliedParameterIndexes = suppliedParameterIndexes;
    }

    internal Type ImplementationType { get; }

    internal GeneratedActivationDescriptor Activation { get; }

    internal ServiceCallSite?[] ParameterCallSites { get; }

    internal int[] SuppliedParameterIndexes { get; }

    internal override CallSiteKind Kind => CallSiteKind.Constructor;
}
