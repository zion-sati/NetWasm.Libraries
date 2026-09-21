// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class SR
    {
        internal const string InvalidActivityTrackingOptions = "The activity tracking options value '{0}' is invalid.";
        internal const string MoreThanOneWildcard = "A category name can contain at most one wildcard.";

        internal static string Format(string format, params object?[] args) => string.Format(format, args);
    }
}
