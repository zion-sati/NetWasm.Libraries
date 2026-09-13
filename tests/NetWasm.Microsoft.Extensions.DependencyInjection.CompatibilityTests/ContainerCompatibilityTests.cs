using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Generated;
using static Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationExtensions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.CompatibilityTests;

public sealed class ContainerCompatibilityTests
{
    [Test]
    public async Task GeneratedActivationResolvesServicesAndOptionalDefaults()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(Activation<IClock, Clock>(
            _ => new Clock("UTC"),
            "clock"));
        services.AddGeneratedTransient(Activation<IClockConsumer, ClockConsumer>(
            values => new ClockConsumer((IClock)values[0]!, (string)values[1]!),
            "consumer",
            new GeneratedParameter(typeof(IClock)),
            new GeneratedParameter(typeof(string), hasDefaultValue: true, defaultValue: "local")));

        using var provider = services.BuildServiceProvider();
        var consumer = provider.GetRequiredService<IClockConsumer>();

        await Assert.That(consumer.Clock.Name).IsEqualTo("UTC");
        await Assert.That(consumer.Zone).IsEqualTo("local");
        await Assert.That(provider.IsService(typeof(IClockConsumer))).IsTrue();
    }

    [Test]
    public async Task GeneratedDescriptorsRetainImmutableMetadataAndSequenceContracts()
    {
        var parameters = new[]
        {
            new GeneratedParameter(
                typeof(string),
                "zone",
                hasDefaultValue: true,
                defaultValue: "UTC",
                lookupMode: ServiceKeyLookupMode.ExplicitKey),
        };
        var activation = new GeneratedActivationDescriptor(
            "clock",
            typeof(IClock),
            typeof(Clock),
            parameters,
            _ => new Clock("UTC"),
            Array.Empty<GeneratedActivationDescriptor>(),
            isPreferred: true,
            new[] { typeof(IClock), typeof(Clock) },
            serviceKey: "blue",
            diagnosticServiceType: "IClock",
            diagnosticImplementationType: "Clock");
        var sequence = new GeneratedSequenceDescriptor(
            typeof(IEnumerable<IClock>),
            typeof(IClock),
            values => values);
        var manifest = new GeneratedActivationManifest(new[] { activation }, new[] { sequence });
        var suppliedValues = Array.Empty<object?>();
        var materializedValues = manifest.Sequences[0].Materialize(suppliedValues);
        parameters[0] = new GeneratedParameter(typeof(int));

        await Assert.That(activation.Identity).IsEqualTo("clock");
        await Assert.That(activation.ServiceKey).IsEqualTo("blue");
        await Assert.That(activation.DiagnosticServiceType).IsEqualTo("IClock");
        await Assert.That(activation.DiagnosticImplementationType).IsEqualTo("Clock");
        await Assert.That(activation.IsPreferred).IsTrue();
        await Assert.That(activation.AssignableTypes.Count).IsEqualTo(2);
        await Assert.That(activation.Parameters[0].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(activation.Parameters[0].DefaultValue).IsEqualTo("UTC");
        await Assert.That(sequence.SequenceType).IsEqualTo(typeof(IEnumerable<IClock>));
        await Assert.That(sequence.ElementType).IsEqualTo(typeof(IClock));
        await Assert.That(sequence.ServiceKey is null).IsTrue();
        await Assert.That(manifest.Activations.Count).IsEqualTo(1);
        await Assert.That(manifest.Sequences.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(materializedValues, suppliedValues)).IsTrue();
    }

    [Test]
    public async Task GeneratedDescriptorOverloadsRetainAlternativeAndKeyMetadata()
    {
        var alternative = Activation<IClock, Clock>(_ => new Clock("alternative"), "alternative");
        var alternatives = new[] { alternative };
        var activation = new GeneratedActivationDescriptor(
            "preferred-clock",
            typeof(IClock),
            typeof(Clock),
            Array.Empty<GeneratedParameter>(),
            _ => new Clock("preferred"),
            alternatives,
            isPreferred: true);
        alternatives[0] = Activation<IClock, Clock>(_ => new Clock("replacement"), "replacement");

        var parameter = new GeneratedParameter(
            typeof(IClock),
            serviceKey: "blue",
            lookupMode: ServiceKeyLookupMode.InheritKey);
        var sequence = new GeneratedSequenceDescriptor(
            typeof(IEnumerable<IClock>),
            values => values);
        var manifestActivations = new[] { activation };
        var manifest = new GeneratedActivationManifest(manifestActivations);
        manifestActivations[0] = alternative;

        await Assert.That(activation.Alternatives.Count).IsEqualTo(1);
        await Assert.That(activation.Alternatives[0].Identity).IsEqualTo("alternative");
        await Assert.That(activation.IsPreferred).IsTrue();
        await Assert.That(parameter.ServiceKey).IsEqualTo("blue");
        await Assert.That(parameter.LookupMode).IsEqualTo(ServiceKeyLookupMode.InheritKey);
        await Assert.That(sequence.ElementType is null).IsTrue();
        await Assert.That(manifest.Activations[0]).IsEqualTo(activation);
        await Assert.That(manifest.Sequences.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GeneratedRegistrationRejectsAnActivationKeyMismatch()
    {
        var activation = new GeneratedActivationDescriptor(
            "blue-clock",
            typeof(IClock),
            typeof(Clock),
            Array.Empty<GeneratedParameter>(),
            _ => new Clock("blue"),
            Array.Empty<GeneratedActivationDescriptor>(),
            isPreferred: false,
            assignableTypes: Array.Empty<Type>(),
            serviceKey: "blue");
        var services = new ServiceCollection();

        var failure = Capture(() => services.AddGeneratedKeyedSingleton("red", activation));

        await Assert.That(failure is ArgumentException).IsTrue();
        await Assert.That(failure?.Message).IsEqualTo("The generated activation key does not match the registration key. (Parameter 'serviceKey')");
        await Assert.That(services.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GeneratedServiceDescriptorsExposeDeterministicDiagnostics()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(Activation<IClock, Clock>(_ => new Clock("UTC"), "clock"));
        services.AddGeneratedKeyedSingleton("blue", Activation<IClock, Clock>(_ => new Clock("blue"), "blue"));

        var unkeyedText = services[0].ToString();
        var keyedText = services[1].ToString();

        await Assert.That(unkeyedText.Contains("ServiceType: clock", StringComparison.Ordinal)).IsTrue();
        await Assert.That(unkeyedText.Contains("Lifetime: Singleton", StringComparison.Ordinal)).IsTrue();
        await Assert.That(keyedText.Contains("ServiceKey: blue", StringComparison.Ordinal)).IsTrue();
        await Assert.That(keyedText.Contains("ImplementationType: blue", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task GeneratedLifetimesCacheAtTheirDeclaredBoundaries()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(Activation<ISingletonValue, SingletonValue>(_ => new SingletonValue(), "singleton"));
        services.AddGeneratedScoped(Activation<IScopedValue, ScopedValue>(_ => new ScopedValue(), "scoped"));
        services.AddGeneratedTransient(Activation<ITransientValue, TransientValue>(_ => new TransientValue(), "transient"));

        using var provider = services.BuildServiceProvider();
        var singleton = provider.GetRequiredService<ISingletonValue>();
        await Assert.That(ReferenceEquals(singleton, provider.GetRequiredService<ISingletonValue>())).IsTrue();

        using var firstScope = provider.CreateScope();
        var scoped = firstScope.ServiceProvider.GetRequiredService<IScopedValue>();
        await Assert.That(ReferenceEquals(scoped, firstScope.ServiceProvider.GetRequiredService<IScopedValue>())).IsTrue();
        await Assert.That(ReferenceEquals(scoped, provider.CreateScope().ServiceProvider.GetRequiredService<IScopedValue>())).IsFalse();

        var transient = firstScope.ServiceProvider.GetRequiredService<ITransientValue>();
        await Assert.That(ReferenceEquals(transient, firstScope.ServiceProvider.GetRequiredService<ITransientValue>())).IsFalse();
    }

    [Test]
    public async Task DescriptorCompositionUsesLastRegistrationForScalarResolution()
    {
        var services = new ServiceCollection();
        services.AddGeneratedTransient(Activation<IChoice, Choice>(_ => new Choice("first"), "first"));
        services.AddGeneratedTransient(Activation<IChoice, Choice>(_ => new Choice("last"), "last"));

        using var provider = services.BuildServiceProvider();
        var choice = provider.GetRequiredService<IChoice>();

        await Assert.That(choice.Name).IsEqualTo("last");
        await Assert.That(services.Count).IsEqualTo(2);
        await Assert.That(services[0].Lifetime).IsEqualTo(ServiceLifetime.Transient);
        await Assert.That(services[1].Lifetime).IsEqualTo(ServiceLifetime.Transient);
    }

    [Test]
    public async Task FactoryInstanceAndKeyedRegistrationsPreservePublicBehavior()
    {
        var instance = new Label("instance");
        var services = new ServiceCollection();
        services.AddSingleton<IValue>(instance);
        services.AddScoped<IClock>(_ => new Clock("factory"));
        services.AddKeyedSingleton<IValue>("blue", new Label("blue"));
        services.AddKeyedTransient<IClock>("factory", (_, key) => new Clock((string)key!));

        using var provider = services.BuildServiceProvider();
        await Assert.That(ReferenceEquals(instance, provider.GetRequiredService<IValue>())).IsTrue();
        await Assert.That(provider.GetRequiredService<IClock>().Name).IsEqualTo("factory");
        await Assert.That(provider.GetRequiredKeyedService<IValue>("blue").Name).IsEqualTo("blue");
        await Assert.That(provider.GetRequiredKeyedService<IClock>("factory").Name).IsEqualTo("factory");
    }

    [Test]
    public async Task KeyedGeneratedRegistrationsUseExactAndAnyKeyFallback()
    {
        var services = new ServiceCollection();
        services.AddGeneratedKeyedSingleton("blue", Activation<IClock, Clock>(_ => new Clock("blue"), "blue"));
        services.AddGeneratedKeyedSingleton(KeyedService.AnyKey, Activation<IClock, Clock>(_ => new Clock("any"), "any"));
        services.AddGeneratedSingleton(Activation<IClock, Clock>(_ => new Clock("unkeyed"), "unkeyed"));

        using var provider = services.BuildServiceProvider();
        var exact = provider.GetRequiredKeyedService<IClock>("blue");
        var fallback = provider.GetRequiredKeyedService<IClock>("red");

        await Assert.That(exact.Name).IsEqualTo("blue");
        await Assert.That(fallback.Name).IsEqualTo("any");
        await Assert.That(provider.GetRequiredService<IClock>().Name).IsEqualTo("unkeyed");
        await Assert.That(ReferenceEquals(exact, provider.GetRequiredKeyedService<IClock>("blue"))).IsTrue();
        await Assert.That(provider.IsKeyedService(typeof(IClock), "blue")).IsTrue();
        await Assert.That(provider.IsKeyedService(typeof(IClock), "red")).IsTrue();
        await Assert.That(provider.IsKeyedService(typeof(IClock), "missing")).IsTrue();
    }

    [Test]
    public async Task KeyedProviderContractsExposeLookupAndAnyKeyBoundaries()
    {
        var services = new ServiceCollection();
        services.AddGeneratedKeyedSingleton("blue", Activation<IClock, Clock>(_ => new Clock("blue"), "blue"));

        using var provider = services.BuildServiceProvider();
        var keyedProvider = provider.GetRequiredService<IKeyedServiceProvider>();
        var keyedAvailability = provider.GetRequiredService<IServiceProviderIsKeyedService>();

        await Assert.That(ReferenceEquals(keyedProvider, provider)).IsTrue();
        await Assert.That(ReferenceEquals(keyedAvailability, provider)).IsTrue();
        await Assert.That(keyedProvider.GetKeyedService(typeof(IClock), "blue") is Clock).IsTrue();
        await Assert.That(keyedProvider.GetKeyedService(typeof(IClock), "missing") is null).IsTrue();
        await Assert.That(keyedAvailability.IsKeyedService(typeof(IClock), "blue")).IsTrue();
        await Assert.That(keyedAvailability.IsKeyedService(typeof(IClock), "missing")).IsFalse();

        var missingFailure = Capture(() => keyedProvider.GetRequiredKeyedService(typeof(IClock), "missing"));
        var anyKeyFailure = Capture(() => keyedProvider.GetKeyedService(typeof(IClock), KeyedService.AnyKey));

        await Assert.That(missingFailure is InvalidOperationException).IsTrue();
        await Assert.That(anyKeyFailure is InvalidOperationException).IsTrue();
    }

    [Test]
    public async Task GeneratedKeyedScopedAndTransientRegistrationsRespectLifetimeBoundaries()
    {
        var services = new ServiceCollection();
        services.AddGeneratedKeyedScoped("scoped", Activation<IScopedValue, ScopedValue>(_ => new ScopedValue(), "scoped"));
        services.AddGeneratedKeyedTransient("transient", Activation<ITransientValue, TransientValue>(_ => new TransientValue(), "transient"));

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstScoped = firstScope.ServiceProvider.GetRequiredKeyedService<IScopedValue>("scoped");
        var firstScopedAgain = firstScope.ServiceProvider.GetRequiredKeyedService<IScopedValue>("scoped");
        var secondScoped = secondScope.ServiceProvider.GetRequiredKeyedService<IScopedValue>("scoped");
        var firstTransient = firstScope.ServiceProvider.GetRequiredKeyedService<ITransientValue>("transient");
        var secondTransient = firstScope.ServiceProvider.GetRequiredKeyedService<ITransientValue>("transient");

        await Assert.That(ReferenceEquals(firstScoped, firstScopedAgain)).IsTrue();
        await Assert.That(ReferenceEquals(firstScoped, secondScoped)).IsFalse();
        await Assert.That(ReferenceEquals(firstTransient, secondTransient)).IsFalse();
    }

    [Test]
    public async Task ProviderIntrinsicsExposeServiceProviderAndScopeFactory()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var scopedProvider = provider.GetRequiredService<IServiceProvider>();
        await Assert.That(scopedProvider is IServiceProvider).IsTrue();
        await Assert.That(scopedProvider.GetService(typeof(IClock)) is null).IsTrue();
        await Assert.That(ReferenceEquals(provider, provider.GetRequiredService<IServiceScopeFactory>())).IsTrue();
        await Assert.That(provider.GetService<IClock>() is null).IsTrue();
        await Assert.That(provider.IsService(typeof(IServiceProviderIsService))).IsTrue();
        await Assert.That(provider.IsService(typeof(IServiceScopeFactory))).IsTrue();
    }

    [Test]
    public async Task ValidationRejectsRootScopedDependencyInSingleton()
    {
        var services = new ServiceCollection();
        services.AddGeneratedScoped(Activation<IScopedValue, ScopedValue>(_ => new ScopedValue(), "scoped"));
        services.AddGeneratedSingleton(Activation<ISingletonConsumer, SingletonConsumer>(
            values => new SingletonConsumer((IScopedValue)values[0]!),
            "consumer",
            new GeneratedParameter(typeof(IScopedValue))));

        var failure = Capture(() => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        }));

        await Assert.That(failure is AggregateException).IsTrue();
    }

    [Test]
    public async Task OpenGenericTemplateDoesNotFailBuildValidation()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IOpenGenericValue<>), typeof(OpenGenericValue<>));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
        });
        var value = provider.GetRequiredService<IOpenGenericValue<string>>();

        await Assert.That(value.Name).IsEqualTo("value");
    }

    [Test]
    public async Task ProviderDisposalReleasesCapturedServicesInReverseOrder()
    {
        var disposed = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(Activation<IDisposableValue, DisposableValue>(
            _ => new DisposableValue("first", disposed), "first"));
        services.AddGeneratedSingleton(Activation<IAsyncDisposableValue, AsyncDisposableValue>(
            _ => new AsyncDisposableValue("second", disposed), "second"));

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IDisposableValue>();
        _ = provider.GetRequiredService<IAsyncDisposableValue>();
        provider.Dispose();

        await Assert.That(disposed.Count).IsEqualTo(2);
        await Assert.That(disposed[0]).IsEqualTo("second-sync");
        await Assert.That(disposed[1]).IsEqualTo("first-sync");
    }

    [Test]
    public async Task ProviderAsyncDisposalUsesAsyncOwnedServiceDisposal()
    {
        var disposed = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(Activation<IAsyncDisposableOnly, AsyncDisposableOnly>(
            _ => new AsyncDisposableOnly(disposed), "async-only"));

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IAsyncDisposableOnly>();
        await provider.DisposeAsync();

        await Assert.That(disposed.Count).IsEqualTo(1);
        await Assert.That(disposed[0]).IsEqualTo("async");
    }

    private static GeneratedActivationDescriptor Activation<TService, TImplementation>(
        GeneratedObjectFactory activate,
        string identity,
        params GeneratedParameter[] parameters) =>
        new(identity, typeof(TService), typeof(TImplementation), parameters, activate);

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private interface IClock
    {
        string Name { get; }
    }

    private sealed class Clock(string name) : IClock
    {
        public string Name { get; } = name;
    }

    private interface IClockConsumer
    {
        IClock Clock { get; }

        string Zone { get; }
    }

    private sealed class ClockConsumer(IClock clock, string zone) : IClockConsumer
    {
        public IClock Clock { get; } = clock;

        public string Zone { get; } = zone;
    }

    private interface ISingletonValue;

    private sealed class SingletonValue : ISingletonValue;

    private interface IScopedValue;

    private sealed class ScopedValue : IScopedValue;

    private interface ITransientValue;

    private sealed class TransientValue : ITransientValue;

    private interface IChoice
    {
        string Name { get; }
    }

    private sealed class Choice(string name) : IChoice
    {
        public string Name { get; } = name;
    }

    private interface IValue
    {
        string Name { get; }
    }

    private sealed class Label(string name) : IValue
    {
        public string Name { get; } = name;
    }

    private interface ISingletonConsumer;

    private sealed class SingletonConsumer(IScopedValue value) : ISingletonConsumer
    {
        public IScopedValue Value { get; } = value;
    }

    private interface IDisposableValue;

    private sealed class DisposableValue(string name, List<string> disposed) : IDisposableValue, IDisposable
    {
        public void Dispose() => disposed.Add(name + "-sync");
    }

    private interface IAsyncDisposableValue;

    private sealed class AsyncDisposableValue(string name, List<string> disposed) : IAsyncDisposableValue, IAsyncDisposable, IDisposable
    {
        public void Dispose() => disposed.Add(name + "-sync");

        public ValueTask DisposeAsync()
        {
            disposed.Add(name + "-async");
            return ValueTask.CompletedTask;
        }
    }

    private interface IAsyncDisposableOnly;

    private sealed class AsyncDisposableOnly(List<string> disposed) : IAsyncDisposableOnly, IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            disposed.Add("async");
            return ValueTask.CompletedTask;
        }
    }

    public interface IOpenGenericValue<T>
    {
        string Name { get; }
    }

    public sealed class OpenGenericValue<T> : IOpenGenericValue<T>
    {
        public string Name => "value";
    }
}
