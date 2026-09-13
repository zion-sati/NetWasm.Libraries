// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides access to services selected by an explicit generated key.</summary>
public interface IKeyedServiceProvider
{
    object? GetKeyedService(Type serviceType, object? serviceKey);

    object GetRequiredKeyedService(Type serviceType, object? serviceKey);
}
