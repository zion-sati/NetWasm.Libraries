// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Logging
{
    internal static class ProviderAliasUtilities
    {
        public const string ConsoleProviderName = "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider";

        // NetWasm deliberately has no runtime reflection. Built-in providers
        // therefore publish their stable upstream identity through ordinary
        // managed type tests. Generic provider filters retain their identity
        // through a typed predicate on LoggerFilterRule.
        public static string? GetName(ILoggerProvider provider)
            => provider is ConsoleLoggerProvider ? ConsoleProviderName : null;

        public static string? GetAlias(ILoggerProvider provider)
            => provider is ConsoleLoggerProvider ? "Console" : null;
    }
}
