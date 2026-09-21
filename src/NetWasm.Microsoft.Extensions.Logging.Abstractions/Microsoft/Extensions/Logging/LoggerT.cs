// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Delegates to a new <see cref="ILogger"/> instance using the full name of the given type, created by the
    /// provided <see cref="ILoggerFactory"/>.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class Logger<T> : ILogger<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="Logger{T}"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public Logger(ILoggerFactory factory) : this(factory, nameof(T))
        {
        }

        /// <summary>
        /// Creates a new <see cref="Logger{T}"/> with a category name supplied by
        /// NetWasm's closed-world dependency-injection generator.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public Logger(ILoggerFactory factory, [LoggerCategoryName] string categoryName)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(categoryName);

            _logger = factory.CreateLogger(categoryName);
        }

        /// <inheritdoc />
        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        /// <inheritdoc />
        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        /// <inheritdoc />
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        private static string GetCategoryName() => nameof(T);

        internal string DebuggerToString()
        {
            return DebuggerDisplayFormatting.DebuggerToString(GetCategoryName(), this);
        }
    }
}
