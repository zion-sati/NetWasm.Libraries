// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class FactoryCallSite : ServiceCallSite
{
    internal FactoryCallSite(Type serviceType, Func<IServiceProvider, object> factory, ResultCache cache)
        : base(cache, serviceType)
    {
        Factory = factory;
    }

    internal Func<IServiceProvider, object> Factory { get; }

    internal override CallSiteKind Kind => CallSiteKind.Factory;
}
