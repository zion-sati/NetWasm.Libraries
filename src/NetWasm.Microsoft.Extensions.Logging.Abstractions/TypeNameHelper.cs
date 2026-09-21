// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Internal
{
    // NetWasm CoreLib intentionally exposes a smaller reflection surface than
    // desktop .NET. Logging only needs a stable category name, so use the
    // metadata name and normalize nested types without generic reflection.
    internal static class TypeNameHelper
    {
        public static string GetTypeDisplayName(
            Type type,
            bool fullName = true,
            bool includeGenericParameterNames = false,
            bool includeGenericParameters = true,
            char nestedTypeDelimiter = '+')
        {
            ArgumentNullException.ThrowIfNull(type);
            string name = fullName ? (type.FullName ?? type.Name) : type.Name;
            return nestedTypeDelimiter == '+' ? name : name.Replace('+', nestedTypeDelimiter);
        }

        public static string? GetTypeDisplayName(object? item, bool fullName = true)
            => item is null ? null : GetTypeDisplayName(item.GetType(), fullName);
    }
}
