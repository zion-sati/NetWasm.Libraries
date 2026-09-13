// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal readonly struct ResultCache
{
    internal static ResultCache None(Type serviceType) =>
        new(CallSiteResultCacheLocation.None, new ServiceCacheKey(ServiceIdentifier.FromServiceType(serviceType), 0));

    private ResultCache(CallSiteResultCacheLocation location, ServiceCacheKey key)
    {
        Location = location;
        Key = key;
    }

    internal ResultCache(ServiceLifetime lifetime, ServiceCacheKey key)
    {
        Key = key;
        Location = lifetime switch
        {
            ServiceLifetime.Singleton => CallSiteResultCacheLocation.Root,
            ServiceLifetime.Scoped => CallSiteResultCacheLocation.Scope,
            ServiceLifetime.Transient => CallSiteResultCacheLocation.Dispose,
            _ => CallSiteResultCacheLocation.None,
        };
    }

    internal CallSiteResultCacheLocation Location { get; }

    internal ServiceCacheKey Key { get; }
}
