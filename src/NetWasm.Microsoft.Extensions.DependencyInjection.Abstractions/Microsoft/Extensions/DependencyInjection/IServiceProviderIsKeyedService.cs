// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Reports whether a generated keyed service is available.</summary>
public interface IServiceProviderIsKeyedService
{
    bool IsKeyedService(Type serviceType, object? serviceKey);
}
