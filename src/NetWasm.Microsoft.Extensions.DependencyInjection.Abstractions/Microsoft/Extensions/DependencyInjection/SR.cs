// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.DependencyInjection
{
    // NetWasm carries only the fixed resource strings reachable from the selected
    // abstraction closure. Localized resource lookup is not a payload dependency.
    internal static class SR
    {
        internal const string ServiceCollectionReadOnly = "The service collection cannot be modified because it is read-only.";
        internal const string NoServiceRegistered = "No service for type '{0}' has been registered.";
        internal const string TryAddIndistinguishableTypeToEnumerable = "Implementation type cannot be '{0}' because it is indistinguishable from other services registered for '{1}'.";
        internal const string NonKeyedDescriptorMisuse = "This service descriptor is not keyed.";

        internal static string Format(string resourceFormat, object? argument) =>
            string.Format(CultureInfo.InvariantCulture, resourceFormat, argument);

        internal static string Format(string resourceFormat, object? firstArgument, object? secondArgument) =>
            string.Format(CultureInfo.InvariantCulture, resourceFormat, firstArgument, secondArgument);
    }
}
