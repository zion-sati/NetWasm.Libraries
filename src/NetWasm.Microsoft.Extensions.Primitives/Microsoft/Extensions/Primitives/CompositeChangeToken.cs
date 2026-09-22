// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.Primitives;

public class CompositeChangeToken : IChangeToken
{
    private readonly object _callbackLock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private List<IDisposable>? _registrations;
    private bool _callbacksInitialized;

    public CompositeChangeToken(IReadOnlyList<IChangeToken> changeTokens)
    {
        ArgumentNullException.ThrowIfNull(changeTokens);
        ChangeTokens = changeTokens;
        for (var index = 0; index < changeTokens.Count; index++)
        {
            if (changeTokens[index].ActiveChangeCallbacks)
            {
                ActiveChangeCallbacks = true;
                break;
            }
        }
    }

    public IReadOnlyList<IChangeToken> ChangeTokens { get; }

    public bool ActiveChangeCallbacks { get; }

    public bool HasChanged
    {
        get
        {
            if (_cancellationTokenSource?.IsCancellationRequested == true)
            {
                return true;
            }

            for (var index = 0; index < ChangeTokens.Count; index++)
            {
                if (ChangeTokens[index].HasChanged)
                {
                    OnChange(this);
                    return true;
                }
            }

            return false;
        }
    }

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        EnsureCallbacksInitialized();
        return _cancellationTokenSource!.Token.Register(callback, state);
    }

    private void EnsureCallbacksInitialized()
    {
        if (_callbacksInitialized)
        {
            return;
        }

        lock (_callbackLock)
        {
            if (_callbacksInitialized)
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _registrations = new List<IDisposable>();
            for (var index = 0; index < ChangeTokens.Count; index++)
            {
                var token = ChangeTokens[index];
                if (!token.ActiveChangeCallbacks)
                {
                    continue;
                }

                var registration = token.RegisterChangeCallback(static state => OnChange(state), this);
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    registration.Dispose();
                    break;
                }

                _registrations.Add(registration);
            }

            _callbacksInitialized = true;
        }
    }

    private static void OnChange(object? state)
    {
        if (state is not CompositeChangeToken composite || composite._cancellationTokenSource is null)
        {
            return;
        }

        lock (composite._callbackLock)
        {
            if (composite._cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            try
            {
                composite._cancellationTokenSource.Cancel();
            }
            catch
            {
            }
        }

        if (composite._registrations is not null)
        {
            for (var index = 0; index < composite._registrations.Count; index++)
            {
                composite._registrations[index].Dispose();
            }
        }
    }
}
