// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Creates one generated activation instance from a provider and supplied arguments.</summary>
public delegate object ObjectFactory(System.IServiceProvider serviceProvider, object[] arguments);
