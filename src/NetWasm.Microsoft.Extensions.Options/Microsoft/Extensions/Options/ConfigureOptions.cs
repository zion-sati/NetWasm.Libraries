// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>Configures an unnamed options instance with an action.</summary>
    public class ConfigureOptions<TOptions> : IConfigureOptions<TOptions> where TOptions : class
    {
        public ConfigureOptions(Action<TOptions>? action) => Action = action;

        public Action<TOptions>? Action { get; }

        public virtual void Configure(TOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            Action?.Invoke(options);
        }
    }

    /// <summary>Configures a named options instance with an action.</summary>
    public class ConfigureNamedOptions<TOptions> : IConfigureNamedOptions<TOptions> where TOptions : class
    {
        public ConfigureNamedOptions(string? name, Action<TOptions>? action)
        {
            Name = name;
            Action = action;
        }

        public string? Name { get; }
        public Action<TOptions>? Action { get; }

        public virtual void Configure(string? name, TOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (Name is null || Name == name)
            {
                Action?.Invoke(options);
            }
        }

        public void Configure(TOptions options) => Configure(Options.DefaultName, options);
    }
}
