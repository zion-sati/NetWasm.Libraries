// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Primitives;

public interface IChangeToken
{
    bool HasChanged { get; }

    bool ActiveChangeCallbacks { get; }

    IDisposable RegisterChangeCallback(Action<object?> callback, object? state);
}
