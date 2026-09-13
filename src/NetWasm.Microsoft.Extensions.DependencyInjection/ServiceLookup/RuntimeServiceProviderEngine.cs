// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class RuntimeServiceProviderEngine : ServiceProviderEngine
{
    internal RuntimeServiceProviderEngine(CallSiteRuntimeResolver resolver)
        : base(resolver)
    {
    }

    internal override Func<ServiceProviderEngineScope, object?> RealizeService(ServiceCallSite callSite) =>
        scope => Resolver.Resolve(callSite, scope);
}
