// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal abstract class ServiceProviderEngine
{
    protected ServiceProviderEngine(CallSiteRuntimeResolver resolver)
    {
        Resolver = resolver;
    }

    protected CallSiteRuntimeResolver Resolver { get; }

    internal abstract Func<ServiceProviderEngineScope, object?> RealizeService(ServiceCallSite callSite);
}
