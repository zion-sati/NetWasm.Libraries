// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>Applies configuration to an options instance.</summary>
    public interface IConfigureOptions<in TOptions> where TOptions : class
    {
        void Configure(TOptions options);
    }

    /// <summary>Applies configuration to a named options instance.</summary>
    public interface IConfigureNamedOptions<in TOptions> : IConfigureOptions<TOptions> where TOptions : class
    {
        void Configure(string? name, TOptions options);
    }
}
