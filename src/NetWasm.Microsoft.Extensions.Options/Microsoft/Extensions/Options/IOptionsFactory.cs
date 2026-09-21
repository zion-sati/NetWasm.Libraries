// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>Creates configured options instances.</summary>
    public interface IOptionsFactory<TOptions> where TOptions : class
    {
        TOptions Create(string name);
    }
}
