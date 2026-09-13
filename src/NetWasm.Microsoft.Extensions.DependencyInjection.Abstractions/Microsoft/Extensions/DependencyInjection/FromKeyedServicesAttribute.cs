// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Marks a generated constructor parameter as a keyed service dependency.</summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class FromKeyedServicesAttribute : Attribute
{
    public FromKeyedServicesAttribute(object serviceKey)
    {
        Key = serviceKey;
        LookupMode = ServiceKeyLookupMode.ExplicitKey;
    }

    public FromKeyedServicesAttribute()
    {
        LookupMode = ServiceKeyLookupMode.InheritKey;
    }

    public object? Key { get; }

    public ServiceKeyLookupMode LookupMode { get; }
}
