// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>Helpers for creating and naming options.</summary>
    public static class Options
    {
        public static readonly string DefaultName = string.Empty;

        public static IOptions<TOptions> Create<TOptions>(TOptions options) where TOptions : class
            => new OptionsWrapper<TOptions>(options);
    }
}
