using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.Options.Tests;

public sealed class OptionsTests
{
    [Test]
    public async Task ConfigurePostConfigureAndValidationRunInOrder()
    {
        var services = new ServiceCollection();
        services.AddOptions<CounterOptions>()
            .Configure(options => options.Value = 4)
            .PostConfigure(options => options.Value *= 2)
            .Validate(options => options.Value == 8, "value must be eight");

        using var provider = services.BuildServiceProvider();
        CounterOptions value = provider.GetRequiredService<IOptions<CounterOptions>>().Value;

        await Assert.That(value.Value).IsEqualTo(8);
    }

    [Test]
    public async Task NamedOptionsAndSnapshotsUseIndependentNames()
    {
        var services = new ServiceCollection();
        services.AddOptions<NamedOptions>("blue").Configure(options => options.Value = "blue");
        services.AddOptions<NamedOptions>("green").Configure(options => options.Value = "green");

        using var provider = services.BuildServiceProvider();
        IOptionsSnapshot<NamedOptions> snapshot = provider.GetRequiredService<IOptionsSnapshot<NamedOptions>>();

        await Assert.That(snapshot.Get("blue").Value).IsEqualTo("blue");
        await Assert.That(snapshot.Get("green").Value).IsEqualTo("green");
    }

    [Test]
    public async Task ConfigureAllAppliesToEveryNamedOption()
    {
        var services = new ServiceCollection();
        services.ConfigureAll<NamedOptions>(options => options.Value = "all");

        using var provider = services.BuildServiceProvider();
        IOptionsSnapshot<NamedOptions> snapshot = provider.GetRequiredService<IOptionsSnapshot<NamedOptions>>();

        await Assert.That(snapshot.Get("blue").Value).IsEqualTo("all");
        await Assert.That(snapshot.Get("green").Value).IsEqualTo("all");
    }

    [Test]
    public async Task MonitorInvalidatesCacheAndNotifiesListeners()
    {
        var source = new TestChangeSource();
        var services = new ServiceCollection();
        services.AddSingleton<IOptionsChangeTokenSource<CounterOptions>>(source);
        services.AddOptions<CounterOptions>().Configure(options => options.Value = source.Generation);

        using var provider = services.BuildServiceProvider();
        IOptionsMonitor<CounterOptions> monitor = provider.GetRequiredService<IOptionsMonitor<CounterOptions>>();
        CounterOptions first = monitor.CurrentValue;
        CounterOptions? changed = null;
        using IDisposable? subscription = monitor.OnChange((options, _) => changed = options);

        source.Generation = 2;
        source.Signal();

        await Assert.That(changed).IsNotNull();
        await Assert.That(changed!.Value).IsEqualTo(2);
        await Assert.That(ReferenceEquals(first, changed)).IsFalse();
    }

    [Test]
    public async Task GeneratedValidatorFailureIncludesNameAndMessages()
    {
        var services = new ServiceCollection();
        services.AddOptions<CounterOptions>()
            .Configure(options => options.Value = 0)
#if NETWASM
            .Validate(new CounterValidator());
#else
            .Validate(options => options.Value > 0, "value must be positive");
#endif
        using var provider = services.BuildServiceProvider();

        bool failed = false;
        try
        {
            _ = provider.GetRequiredService<IOptions<CounterOptions>>().Value;
        }
        catch (OptionsValidationException exception)
        {
            failed = exception.OptionsName == global::Microsoft.Extensions.Options.Options.DefaultName &&
                exception.OptionsType == typeof(CounterOptions) &&
                exception.Message.IndexOf("positive", StringComparison.Ordinal) >= 0;
        }

        await Assert.That(failed).IsTrue();
    }

    public sealed class CounterOptions
    {
        public int Value { get; set; }
    }

    public sealed class NamedOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class CounterValidator : IValidateOptions<CounterOptions>
    {
        public ValidateOptionsResult Validate(string? name, CounterOptions options) =>
            options.Value > 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail("value must be positive");
    }

    private sealed class TestChangeSource : IOptionsChangeTokenSource<CounterOptions>
    {
        private CancellationTokenSource _cancellation = new();

        public int Generation { get; set; } = 1;

        public string? Name => global::Microsoft.Extensions.Options.Options.DefaultName;

        public IChangeToken GetChangeToken() => new CancellationChangeToken(_cancellation.Token);

        public void Signal()
        {
            var previous = _cancellation;
            _cancellation = new CancellationTokenSource();
            previous.Cancel();
            previous.Dispose();
        }
    }

}
