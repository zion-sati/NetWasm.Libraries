// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal abstract class ServiceCallSite
{
    protected ServiceCallSite(ResultCache cache, Type serviceType)
    {
        Cache = cache;
        ServiceType = serviceType;
    }

    internal ResultCache Cache { get; }

    internal Type ServiceType { get; }

    internal abstract CallSiteKind Kind { get; }
}
