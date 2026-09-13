// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Generated;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class IEnumerableCallSite : ServiceCallSite
{
    internal IEnumerableCallSite(
        Type serviceType,
        ServiceCallSite[] serviceCallSites,
        ResultCache cache,
        GeneratedEnumerableFactory materialize)
        : base(cache, serviceType)
    {
        ServiceCallSites = serviceCallSites;
        Materialize = materialize;
    }

    internal ServiceCallSite[] ServiceCallSites { get; }

    internal GeneratedEnumerableFactory Materialize { get; }

    internal override CallSiteKind Kind => CallSiteKind.Enumerable;
}
