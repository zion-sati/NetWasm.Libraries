// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Writes logging entries synchronously to stdout (or stderr at the configured threshold).
    /// </summary>
    [ProviderAlias("Console")]
    public sealed class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, ConsoleLogger> _loggers = new Dictionary<string, ConsoleLogger>(StringComparer.Ordinal);
        private readonly ConsoleLoggerOptions _options;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;
        private IExternalScopeProvider? _scopeProvider;
        private bool _disposed;

        public ConsoleLoggerProvider(ConsoleLoggerOptions options)
            : this(options, System.Console.Out, System.Console.Error)
        {
        }

        /// <summary>
        /// Creates a provider using the supplied output writers. The overload is useful for hosts
        /// that own their output streams; the parameterless writer behavior remains stdout/stderr.
        /// </summary>
        public ConsoleLoggerProvider(ConsoleLoggerOptions options, TextWriter stdout, TextWriter stderr)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _stdout = stdout ?? throw new ArgumentNullException(nameof(stdout));
            _stderr = stderr ?? throw new ArgumentNullException(nameof(stderr));
            if (_options.IncludeScopes)
            {
                _options.SimpleFormatterOptions.IncludeScopes = true;
                _options.JsonFormatterOptions.IncludeScopes = true;
            }
        }

        public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options)
            : this(options?.CurrentValue ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            ArgumentNullException.ThrowIfNull(categoryName);
            lock (_sync)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ConsoleLoggerProvider));
                }

                if (!_loggers.TryGetValue(categoryName, out ConsoleLogger? logger))
                {
                    logger = new ConsoleLogger(categoryName, _options, _scopeProvider, _stdout, _stderr);
                    _loggers.Add(categoryName, logger);
                }

                return logger;
            }
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            ArgumentNullException.ThrowIfNull(scopeProvider);
            lock (_sync)
            {
                _scopeProvider = scopeProvider;
                foreach (ConsoleLogger logger in _loggers.Values)
                {
                    logger.SetScopeProvider(scopeProvider);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                _loggers.Clear();
            }
        }
    }

    internal sealed class ConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConsoleLoggerOptions _options;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;
        private IExternalScopeProvider? _scopeProvider;

        public ConsoleLogger(string categoryName, ConsoleLoggerOptions options, IExternalScopeProvider? scopeProvider, TextWriter stdout, TextWriter stderr)
        {
            _categoryName = categoryName;
            _options = options;
            _stdout = stdout;
            _stderr = stderr;
            _scopeProvider = scopeProvider;
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _scopeProvider?.Push(state) ?? NoopDisposable.Instance;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);
            string message = formatter(state, exception) ?? string.Empty;
            string formatterName = _options.FormatterName ?? ConsoleFormatterNames.Simple;
            var scopes = new List<object?>();
            if ((_options.IncludeScopes || IsFormatterScopesEnabled(formatterName)) && _scopeProvider != null)
            {
                _scopeProvider.ForEachScope(static (scope, values) => values.Add(scope), scopes);
            }

            TextWriter writer = _options.LogToStandardErrorThreshold != LogLevel.None &&
                logLevel >= _options.LogToStandardErrorThreshold ? _stderr : _stdout;

            if (string.Equals(formatterName, ConsoleFormatterNames.Json, StringComparison.OrdinalIgnoreCase))
            {
                ConsoleOutput.WriteJson(writer, _categoryName, logLevel, eventId, state, message, exception, scopes, _options.JsonFormatterOptions);
            }
            else
            {
                ConsoleOutput.WriteSimple(writer, _categoryName, logLevel, eventId, state, message, exception, scopes, _options);
            }
        }

        private bool IsFormatterScopesEnabled(string formatterName)
            => string.Equals(formatterName, ConsoleFormatterNames.Json, StringComparison.OrdinalIgnoreCase)
                ? _options.JsonFormatterOptions.IncludeScopes
                : _options.SimpleFormatterOptions.IncludeScopes;
    }

    internal sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new NoopDisposable();
        private NoopDisposable()
        {
        }
        public void Dispose()
        {
        }
    }
}
