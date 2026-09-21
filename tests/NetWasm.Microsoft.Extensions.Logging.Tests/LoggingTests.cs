using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.Logging.Tests;

public sealed partial class LoggingTests
{
    [Test]
    public async Task SimpleProviderWritesMessageAndActivityIdsToStdout()
    {
#if NETWASM
        var output = new StringWriter();
        using var provider = new ConsoleLoggerProvider(new ConsoleLoggerOptions(), output, output);
        var filters = new LoggerFilterOptions { MinLevel = LogLevel.Trace };
        filters.AddFilter<ConsoleLoggerProvider>(null, LogLevel.Information);
        using var factory = new LoggerFactory(new[] { provider }, new StaticOptionsMonitor<LoggerFilterOptions>(filters),
            Options.Create(new LoggerFactoryOptions
            {
                ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId,
            }));
        ILogger logger = factory.CreateLogger("LoggingTests");
        using var activity = new Activity("logging-test").Start();
        using (logger.BeginScope("request-1"))
        {
            logger.LogInformation(new EventId(7, "Sample"), "hello {Value}", 42);
        }

        string text = output.ToString();
        await Assert.That(logger.IsEnabled(LogLevel.Debug)).IsFalse();
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
        await Assert.That(text.IndexOf("hello 42", StringComparison.Ordinal) >= 0).IsTrue();
        await Assert.That(text.IndexOf("LoggingTests[7]", StringComparison.Ordinal) >= 0).IsTrue();
        await Assert.That(text.IndexOf("TraceId", StringComparison.Ordinal) >= 0).IsTrue();
        await Assert.That(text.IndexOf("SpanId", StringComparison.Ordinal) >= 0).IsTrue();
        await Assert.That(text.IndexOf("request-1", StringComparison.Ordinal) >= 0).IsTrue();
#else
        using var factory = LoggerFactory.Create(builder => builder
            .AddSimpleConsole(options => options.IncludeScopes = true)
            .AddFilter<ConsoleLoggerProvider>(null, LogLevel.Information)
            .Configure(options => options.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId));
        ILogger logger = factory.CreateLogger("LoggingTests");
        using var activity = new Activity("logging-test").Start();
        using (logger.BeginScope("request-1"))
        {
            logger.LogInformation(new EventId(7, "Sample"), "hello {Value}", 42);
        }
        await Assert.That(logger.IsEnabled(LogLevel.Debug)).IsFalse();
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
#endif
    }

    [Test]
    public async Task JsonProviderWritesStructuredStateAndScopes()
    {
#if NETWASM
        var output = new StringWriter();
        var options = new ConsoleLoggerOptions
        {
            FormatterName = ConsoleFormatterNames.Json,
            IncludeScopes = true,
        };
        using var provider = new ConsoleLoggerProvider(options, output, output);
        using var factory = new LoggerFactory(new[] { provider }, new LoggerFilterOptions { MinLevel = LogLevel.Trace, CaptureScopes = true });
        ILogger logger = factory.CreateLogger("JsonCategory");
        using (logger.BeginScope("request-1"))
        {
            logger.LogInformation(new EventId(8), "json {Value}", 42);
        }

        string text = output.ToString();
        await Assert.That(text.IndexOf("\"Category\":\"JsonCategory\"", StringComparison.Ordinal) >= 0).IsTrue();
        await Assert.That(text.IndexOf("\"Value\":42", StringComparison.Ordinal) >= 0).IsTrue();
        await Assert.That(text.IndexOf("\"Scopes\":[\"request-1\"]", StringComparison.Ordinal) >= 0).IsTrue();
#else
        using var factory = LoggerFactory.Create(builder => builder
            .AddJsonConsole(options => options.IncludeScopes = true)
            .SetMinimumLevel(LogLevel.Trace));
        ILogger logger = factory.CreateLogger("JsonCategory");
        using (logger.BeginScope("request-1"))
        {
            logger.LogInformation(new EventId(8), "json {Value}", 42);
        }
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
#endif
    }

    [Test]
    public async Task AddLoggingRegistersFactoryAndGenericLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Debug));
        using var provider = services.BuildServiceProvider();
        ILogger<LoggingTests> logger = provider.GetRequiredService<ILogger<LoggingTests>>();

        await Assert.That(logger.IsEnabled(LogLevel.Debug)).IsTrue();
        await Assert.That(provider.GetRequiredService<ILoggerFactory>()).IsNotNull();
    }

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "generated {Value}")]
    private static partial void Generated(ILogger logger, int value);

    [Test]
    public async Task LoggerMessageGeneratorProducesCallableMethod()
    {
#if NETWASM
        var output = new StringWriter();
        using var provider = new ConsoleLoggerProvider(new ConsoleLoggerOptions(), output, output);
        using var factory = new LoggerFactory(new[] { provider }, new LoggerFilterOptions { MinLevel = LogLevel.Trace });
        Generated(factory.CreateLogger("Generated"), 5);

        await Assert.That(output.ToString().IndexOf("generated 5", StringComparison.Ordinal) >= 0).IsTrue();
#else
        using var factory = LoggerFactory.Create(builder => builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Trace));
        ILogger logger = factory.CreateLogger("Generated");
        Generated(logger, 5);
        await Assert.That(logger.IsEnabled(LogLevel.Warning)).IsTrue();
#endif
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
