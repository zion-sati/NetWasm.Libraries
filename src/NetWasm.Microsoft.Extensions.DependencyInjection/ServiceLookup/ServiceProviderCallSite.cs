// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class ServiceProviderCallSite : ServiceCallSite
{
    internal ServiceProviderCallSite(Type serviceType)
        : base(ResultCache.None(serviceType), serviceType)
    {
    }

    internal override CallSiteKind Kind => CallSiteKind.ServiceProvider;
}
