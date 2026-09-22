// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Primitives;

public class CancellationChangeToken : IChangeToken
{
    private readonly CancellationToken _token;

    public CancellationChangeToken(CancellationToken cancellationToken)
    {
        _token = cancellationToken;
    }

    public bool ActiveChangeCallbacks { get; private set; } = true;

    public bool HasChanged => _token.IsCancellationRequested;

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        try
        {
            return _token.Register(callback, state);
        }
        catch (ObjectDisposedException)
        {
            ActiveChangeCallbacks = false;
            return EmptyDisposable.Instance;
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        internal static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
