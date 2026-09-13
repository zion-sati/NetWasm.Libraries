// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class ConstantCallSite : ServiceCallSite
{
    internal ConstantCallSite(Type serviceType, object? value)
        : base(ResultCache.None(serviceType), serviceType)
    {
        Value = value;
    }

    internal object? Value { get; }

    internal override CallSiteKind Kind => CallSiteKind.Constant;
}
