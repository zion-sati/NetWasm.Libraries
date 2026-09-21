// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>Retrieves the default configured options instance.</summary>
    public interface IOptions<out TOptions> where TOptions : class
    {
        TOptions Value { get; }
    }

    /// <summary>Retrieves a configured options instance for the current scope.</summary>
    public interface IOptionsSnapshot<out TOptions> : IOptions<TOptions> where TOptions : class
    {
        TOptions Get(string? name);
    }

    /// <summary>Provides the current options instance.</summary>
    public interface IOptionsMonitor<out TOptions> where TOptions : class
    {
        TOptions CurrentValue { get; }
        TOptions Get(string? name);
        IDisposable? OnChange(Action<TOptions, string?> listener);
    }
}
