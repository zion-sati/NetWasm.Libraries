using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.CompatibilityTests;

public sealed class DesktopCompatibilityTests
{
    [Test]
    public async Task StandardRegistrationsPreserveLifetimeBoundaries()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISingletonValue, SingletonValue>();
        services.AddScoped<IScopedValue, ScopedValue>();
        services.AddTransient<ITransientValue, TransientValue>();

        using var provider = services.BuildServiceProvider();
        var singleton = provider.GetRequiredService<ISingletonValue>();
        await Assert.That(ReferenceEquals(singleton, provider.GetRequiredService<ISingletonValue>())).IsTrue();

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var scoped = firstScope.ServiceProvider.GetRequiredService<IScopedValue>();
        await Assert.That(ReferenceEquals(scoped, firstScope.ServiceProvider.GetRequiredService<IScopedValue>())).IsTrue();
        await Assert.That(ReferenceEquals(scoped, secondScope.ServiceProvider.GetRequiredService<IScopedValue>())).IsFalse();

        var transient = firstScope.ServiceProvider.GetRequiredService<ITransientValue>();
        await Assert.That(ReferenceEquals(transient, firstScope.ServiceProvider.GetRequiredService<ITransientValue>())).IsFalse();
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
    public async Task ProviderIntrinsicsExposeServiceProviderAndScopeFactory()
    {
        var services = new ServiceCollection();

        using var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetRequiredService<IServiceProvider>() is IServiceProvider).IsTrue();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        await Assert.That(scope.ServiceProvider is IServiceProvider).IsTrue();
        await Assert.That(provider.GetService<IClock>() is null).IsTrue();
        var availability = provider.GetRequiredService<IServiceProviderIsService>();
        await Assert.That(availability.IsService(typeof(IServiceScopeFactory))).IsTrue();
    }

    [Test]
    public async Task ScopeValidationRejectsRootScopedResolution()
    {
        var services = new ServiceCollection();
        services.AddScoped<IScopedValue, ScopedValue>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var failure = Capture(() => provider.GetRequiredService<IScopedValue>());

        await Assert.That(failure is InvalidOperationException).IsTrue();
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
    public async Task SynchronousDisposalReleasesCapturedServicesInReverseOrder()
    {
        var disposed = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IFirstValue>(_ => new DisposableValue("first", disposed));
        services.AddSingleton<ISecondValue>(_ => new DisposableValue("second", disposed));

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IFirstValue>();
        _ = provider.GetRequiredService<ISecondValue>();
        provider.Dispose();

        await Assert.That(disposed).IsEquivalentTo(new[] { "second", "first" });
    }

    [Test]
    public async Task AsynchronousDisposalUsesOwnedServiceAsyncDisposal()
    {
        var disposed = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IAsyncValue>(_ => new AsyncDisposableValue(disposed));

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IAsyncValue>();
        await provider.DisposeAsync();

        await Assert.That(disposed).IsEquivalentTo(new[] { "async" });
    }

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

    private interface ISingletonValue;

    private sealed class SingletonValue : ISingletonValue;

    private interface IScopedValue;

    private sealed class ScopedValue : IScopedValue;

    private interface ITransientValue;

    private sealed class TransientValue : ITransientValue;

    private interface IClock
    {
        string Name { get; }
    }

    private sealed class Clock(string name) : IClock
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

    private interface IFirstValue;

    private interface ISecondValue;

    private sealed class DisposableValue(string name, List<string> disposed) : IFirstValue, ISecondValue, IDisposable
    {
        public void Dispose() => disposed.Add(name);
    }

    private interface IAsyncValue;

    private sealed class AsyncDisposableValue(List<string> disposed) : IAsyncValue, IAsyncDisposable
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
