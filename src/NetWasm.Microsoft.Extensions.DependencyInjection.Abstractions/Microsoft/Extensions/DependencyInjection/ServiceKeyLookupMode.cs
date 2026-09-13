// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Controls how a generated keyed constructor parameter obtains its key.</summary>
public enum ServiceKeyLookupMode
{
    ExplicitKey,
    NullKey,
    InheritKey,
}
