// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>Wraps an options instance in <see cref="IOptions{TOptions}"/>.</summary>
    public sealed class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class
    {
        public OptionsWrapper(TOptions options) => Value = options ?? throw new ArgumentNullException(nameof(options));

        public TOptions Value { get; }
    }
}
