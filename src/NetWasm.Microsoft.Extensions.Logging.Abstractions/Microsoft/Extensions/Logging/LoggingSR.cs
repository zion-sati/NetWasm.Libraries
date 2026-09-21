// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class SR
    {
        internal const string UnexpectedNumberOfNamedParameters = "The format string contains {1} named parameters, but {2} values were provided: {0}";

        internal static string Format(string format, params object?[] args) => string.Format(format, args);
    }
}
