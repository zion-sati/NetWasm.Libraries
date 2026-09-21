// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>Names of the built-in synchronous console formatters.</summary>
    public static class ConsoleFormatterNames
    {
        public const string Simple = "simple";
        public const string Json = "json";
    }

    /// <summary>Controls how simple console output handles ANSI colors.</summary>
    public enum LoggerColorBehavior
    {
        Default,
        Enabled,
        Disabled
    }

    /// <summary>Legacy console format names retained for source compatibility.</summary>
    [Obsolete("ConsoleLoggerFormat is obsolete. Use FormatterName instead.")]
    public enum ConsoleLoggerFormat
    {
        Default,
        Systemd
    }

    /// <summary>Shared options for console formatters.</summary>
    public class ConsoleFormatterOptions
    {
        public bool IncludeScopes { get; set; }
        public string? TimestampFormat { get; set; }
        public bool UseUtcTimestamp { get; set; }
    }

    /// <summary>Options for the text formatter.</summary>
    public class SimpleConsoleFormatterOptions : ConsoleFormatterOptions
    {
        public bool SingleLine { get; set; }
        public LoggerColorBehavior ColorBehavior { get; set; }
    }

    /// <summary>Options for the line-delimited JSON formatter.</summary>
    public class JsonConsoleFormatterOptions : ConsoleFormatterOptions
    {
    }

    /// <summary>Options shared by the built-in console logger provider.</summary>
    public class ConsoleLoggerOptions
    {
        public string? FormatterName { get; set; } = ConsoleFormatterNames.Simple;
        public bool IncludeScopes { get; set; }
        public LogLevel LogToStandardErrorThreshold { get; set; } = LogLevel.None;
        public ActivityTrackingOptions ActivityTrackingOptions { get; set; }

        // These formatter-specific instances let AddSimpleConsole/AddJsonConsole
        // keep their upstream configuration shape without a reloadable options
        // monitor or a formatter queue.
        internal SimpleConsoleFormatterOptions SimpleFormatterOptions { get; } = new SimpleConsoleFormatterOptions();
        internal JsonConsoleFormatterOptions JsonFormatterOptions { get; } = new JsonConsoleFormatterOptions();

        [Obsolete("ConsoleLoggerOptions.DisableColors is obsolete. Use SimpleConsoleFormatterOptions.ColorBehavior instead.")]
        public bool DisableColors { get; set; }

        [Obsolete("ConsoleLoggerOptions.TimestampFormat is obsolete. Use formatter options instead.")]
        public string? TimestampFormat { get; set; }

        [Obsolete("ConsoleLoggerOptions.UseUtcTimestamp is obsolete. Use formatter options instead.")]
        public bool UseUtcTimestamp { get; set; }
    }
}
