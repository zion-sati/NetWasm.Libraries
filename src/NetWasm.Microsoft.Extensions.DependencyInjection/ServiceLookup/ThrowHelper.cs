// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal static class ThrowHelper
{
    internal static void ThrowNoService() => throw new InvalidOperationException("No service is registered for the requested service type.");

}
