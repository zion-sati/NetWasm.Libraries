// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Logging
{
    /// <summary>Registers the synchronous NetWasm console providers.</summary>
    public static class ConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddConsole(this ILoggingBuilder builder)
            => builder.AddProvider(new ConsoleLoggerProvider(new ConsoleLoggerOptions()));

        public static ILoggingBuilder AddConsole(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var options = new ConsoleLoggerOptions();
            configure(options);
            return builder.AddProvider(new ConsoleLoggerProvider(options));
        }

        public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder)
            => builder.AddSimpleConsole(_ => { });

        public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder, Action<SimpleConsoleFormatterOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var options = new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Simple };
            configure(options.SimpleFormatterOptionsForConfiguration());
            options.IncludeScopes = options.SimpleFormatterOptionsForConfiguration().IncludeScopes;
            return builder.AddProvider(new ConsoleLoggerProvider(options));
        }

        public static ILoggingBuilder AddJsonConsole(this ILoggingBuilder builder)
            => builder.AddJsonConsole(_ => { });

        public static ILoggingBuilder AddJsonConsole(this ILoggingBuilder builder, Action<JsonConsoleFormatterOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var options = new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json };
            configure(options.JsonFormatterOptionsForConfiguration());
            options.IncludeScopes = options.JsonFormatterOptionsForConfiguration().IncludeScopes;
            return builder.AddProvider(new ConsoleLoggerProvider(options));
        }

        private static SimpleConsoleFormatterOptions SimpleFormatterOptionsForConfiguration(this ConsoleLoggerOptions options)
            => options.SimpleFormatterOptions;

        private static JsonConsoleFormatterOptions JsonFormatterOptionsForConfiguration(this ConsoleLoggerOptions options)
            => options.JsonFormatterOptions;
    }
}
