// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Defines generated keyed-service sentinel values.</summary>
public static class KeyedService
{
    private static readonly object AnyKeyValue = new();

    /// <summary>Matches any explicit key when used by a generated registration.</summary>
    public static object AnyKey => AnyKeyValue;
}
