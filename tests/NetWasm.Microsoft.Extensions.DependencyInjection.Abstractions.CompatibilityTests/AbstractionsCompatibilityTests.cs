using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions.CompatibilityTests;

public sealed class AbstractionsCompatibilityTests
{
    [Test]
    public async Task ServiceCollectionRetainsListOrderingAndReadOnlyBoundary()
    {
        var first = ServiceDescriptor.Singleton<IContract, FirstContract>();
        var second = ServiceDescriptor.Transient<IContract, SecondContract>();
        var third = ServiceDescriptor.Scoped<IContract, ThirdContract>();
        var services = new ServiceCollection();
        ((ICollection<ServiceDescriptor>)services).Add(first);

        services.Insert(0, second);
        services[1] = third;

        await Assert.That(services.Count).IsEqualTo(2);
        await Assert.That(ReferenceEquals(services[0], second)).IsTrue();
        await Assert.That(ReferenceEquals(services[1], third)).IsTrue();
        await Assert.That(services.Remove(second)).IsTrue();
        await Assert.That(services.Count).IsEqualTo(1);

        services.MakeReadOnly();
        await Assert.That(services.IsReadOnly).IsTrue();

        var failure = Capture(() => ((ICollection<ServiceDescriptor>)services).Add(first));
        await Assert.That(failure is InvalidOperationException).IsTrue();
        await Assert.That(failure?.Message).IsEqualTo("The service collection cannot be modified because it is read-only.");
    }

    [Test]
    public async Task ServiceDescriptorsRetainTypeFactoryInstanceAndLifetimeData()
    {
        var instance = new FirstContract();
        Func<IServiceProvider, IContract> factory = _ => new SecondContract();
        var typeDescriptor = ServiceDescriptor.Transient<IContract, ThirdContract>();
        var factoryDescriptor = ServiceDescriptor.Scoped(factory);
        var instanceDescriptor = ServiceDescriptor.Singleton<IContract>(instance);
        var transientTypedFactory = ServiceDescriptor.Transient<IContract, FirstContract>(_ => new FirstContract());
        var transientServiceFactory = ServiceDescriptor.Transient<IContract>(_ => new FirstContract());
        var keyedTransientTypedFactory = ServiceDescriptor.KeyedTransient<IContract, FirstContract>("transient", (_, _) => new FirstContract());
        var keyedTransientServiceFactory = ServiceDescriptor.KeyedTransient<IContract>("transient-service", (_, _) => new FirstContract());
        var scopedTypedFactory = ServiceDescriptor.Scoped<IContract, SecondContract>(_ => new SecondContract());
        var scopedServiceFactory = ServiceDescriptor.Scoped<IContract>(_ => new SecondContract());
        var keyedScopedTypedFactory = ServiceDescriptor.KeyedScoped<IContract, SecondContract>("scoped", (_, _) => new SecondContract());
        var keyedScopedServiceFactory = ServiceDescriptor.KeyedScoped<IContract>("scoped-service", (_, _) => new SecondContract());
        var singletonTypedFactory = ServiceDescriptor.Singleton<IContract, ThirdContract>(_ => new ThirdContract());
        var singletonServiceFactory = ServiceDescriptor.Singleton<IContract>(_ => new ThirdContract());
        var keyedSingletonTypedFactory = ServiceDescriptor.KeyedSingleton<IContract, ThirdContract>("singleton", (_, _) => new ThirdContract());
        var keyedSingletonServiceFactory = ServiceDescriptor.KeyedSingleton<IContract>("singleton-service", (_, _) => new ThirdContract());
        var nullKeyFactory = ServiceDescriptor.KeyedTransient<IContract>(null, (_, _) => new FirstContract());

        await Assert.That(typeDescriptor.ServiceType).IsEqualTo(typeof(IContract));
        await Assert.That(typeDescriptor.ImplementationType).IsEqualTo(typeof(ThirdContract));
        await Assert.That(typeDescriptor.Lifetime).IsEqualTo(ServiceLifetime.Transient);
        await Assert.That(factoryDescriptor.ImplementationFactory is not null).IsTrue();
        await Assert.That(factoryDescriptor.ImplementationFactory!(new DelegateProvider(_ => null)) is SecondContract).IsTrue();
        await Assert.That(factoryDescriptor.ImplementationType is null).IsTrue();
        await Assert.That(factoryDescriptor.Lifetime).IsEqualTo(ServiceLifetime.Scoped);
        await Assert.That(ReferenceEquals(instanceDescriptor.ImplementationInstance, instance)).IsTrue();
        await Assert.That(instanceDescriptor.Lifetime).IsEqualTo(ServiceLifetime.Singleton);
        await Assert.That(transientTypedFactory.ImplementationFactory is not null).IsTrue();
        await Assert.That(transientServiceFactory.ImplementationFactory is not null).IsTrue();
        await Assert.That(keyedTransientTypedFactory.KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(keyedTransientServiceFactory.KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(scopedTypedFactory.ImplementationFactory is not null).IsTrue();
        await Assert.That(scopedServiceFactory.ImplementationFactory is not null).IsTrue();
        await Assert.That(keyedScopedTypedFactory.KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(keyedScopedServiceFactory.KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(singletonTypedFactory.ImplementationFactory is not null).IsTrue();
        await Assert.That(singletonServiceFactory.ImplementationFactory is not null).IsTrue();
        await Assert.That(keyedSingletonTypedFactory.KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(keyedSingletonServiceFactory.KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(nullKeyFactory.ImplementationFactory is not null).IsTrue();
    }

    [Test]
    public async Task KeyedDescriptorPropertiesAndDiagnosticsRetainTheirContracts()
    {
        var factoryDescriptor = ServiceDescriptor.KeyedScoped<IContract>(
            "factory",
            static (_, key) => key is "factory" ? new FirstContract() : new SecondContract());
        var instance = new SecondContract();
        var instanceDescriptor = ServiceDescriptor.KeyedSingleton<IContract>("instance", instance);
        var typeDescriptor = ServiceDescriptor.KeyedTransient<IContract, ThirdContract>("type");

        await Assert.That(factoryDescriptor.IsKeyedService).IsTrue();
        await Assert.That(factoryDescriptor.ServiceKey).IsEqualTo("factory");
        await Assert.That(factoryDescriptor.ImplementationType is null).IsTrue();
        await Assert.That(factoryDescriptor.ImplementationInstance is null).IsTrue();
        await Assert.That(factoryDescriptor.KeyedImplementationType is null).IsTrue();
        await Assert.That(factoryDescriptor.KeyedImplementationFactory!(new DelegateProvider(_ => null), "factory") is FirstContract).IsTrue();
        await Assert.That(ReferenceEquals(instanceDescriptor.KeyedImplementationInstance, instance)).IsTrue();
        await Assert.That(instanceDescriptor.KeyedImplementationFactory is null).IsTrue();
        await Assert.That(typeDescriptor.KeyedImplementationType).IsEqualTo(typeof(ThirdContract));

        var factoryText = factoryDescriptor.ToString();
        var instanceText = instanceDescriptor.ToString();
        await Assert.That(factoryText.Contains("ServiceKey: factory", StringComparison.Ordinal)).IsTrue();
        await Assert.That(factoryText.Contains("KeyedImplementationFactory", StringComparison.Ordinal)).IsTrue();
        await Assert.That(instanceText.Contains("KeyedImplementationInstance", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RegistrationExtensionsRetainTryAddEnumerableReplaceAndRemoveRules()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IContract, FirstContract>();
        services.AddTransient<IContract, SecondContract>();
        services.TryAddSingleton<IContract, ThirdContract>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IContract, ThirdContract>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IContract, ThirdContract>());

        await Assert.That(services.Count).IsEqualTo(3);
        await Assert.That(services[0].ImplementationType).IsEqualTo(typeof(FirstContract));
        await Assert.That(services[1].ImplementationType).IsEqualTo(typeof(SecondContract));
        await Assert.That(services[2].ImplementationType).IsEqualTo(typeof(ThirdContract));

        services.Replace(ServiceDescriptor.Singleton<IContract, FirstContract>());
        await Assert.That(services.Count).IsEqualTo(3);
        await Assert.That(services[0].ImplementationType).IsEqualTo(typeof(SecondContract));
        await Assert.That(services[2].ImplementationType).IsEqualTo(typeof(FirstContract));

        services.AddKeyedSingleton<IContract, ThirdContract>("kept");
        services.RemoveAll<IContract>();
        await Assert.That(services.Count).IsEqualTo(1);
        await Assert.That(services[0].ServiceKey).IsEqualTo("kept");
        services.RemoveAllKeyed<IContract>("kept");
        await Assert.That(services.Count).IsEqualTo(0);
    }

    [Test]
    public async Task KeyedDescriptorsAndAttributesRetainIdentity()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IContract, FirstContract>("transient");
        services.AddKeyedScoped<IContract, SecondContract>("scoped");
        services.AddKeyedSingleton<IContract, ThirdContract>("singleton");
        services.AddKeyedTransient(typeof(IContract), "factory", (_, _) => new FirstContract());
        services.AddKeyedSingleton<IContract>("instance", new SecondContract());

        await Assert.That(services.Count).IsEqualTo(5);
        await Assert.That(services[0].ServiceKey).IsEqualTo("transient");
        await Assert.That(services[0].Lifetime).IsEqualTo(ServiceLifetime.Transient);
        await Assert.That(services[0].ImplementationType is null).IsTrue();
        await Assert.That(services[0].KeyedImplementationType).IsEqualTo(typeof(FirstContract));
        await Assert.That(services[3].KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(services[4].KeyedImplementationInstance is SecondContract).IsTrue();
        await Assert.That(ReferenceEquals(KeyedService.AnyKey, KeyedService.AnyKey)).IsTrue();

        var explicitKey = new FromKeyedServicesAttribute("key");
        var inheritedKey = new FromKeyedServicesAttribute();
        await Assert.That(explicitKey.Key).IsEqualTo("key");
        await Assert.That(explicitKey.LookupMode).IsEqualTo(ServiceKeyLookupMode.ExplicitKey);
        await Assert.That(inheritedKey.Key is null).IsTrue();
        await Assert.That(inheritedKey.LookupMode).IsEqualTo(ServiceKeyLookupMode.InheritKey);
        await Assert.That(new ActivatorUtilitiesConstructorAttribute() is Attribute).IsTrue();
    }

    [Test]
    public async Task RegistrationOverloadsRetainExplicitFactoryAndKeyIdentity()
    {
        var factoryServices = new ServiceCollection();
        factoryServices.AddTransient<IContract>(_ => new FirstContract());
        factoryServices.AddTransient<IContract, FirstContract>(_ => new FirstContract());
        factoryServices.AddScoped<IContract>(_ => new SecondContract());
        factoryServices.AddScoped<IContract, SecondContract>(_ => new SecondContract());
        factoryServices.AddSingleton<IContract>(_ => new ThirdContract());
        factoryServices.AddSingleton<IContract, ThirdContract>(_ => new ThirdContract());

        await Assert.That(factoryServices.Count).IsEqualTo(6);
        await Assert.That(factoryServices[0].ImplementationType is null).IsTrue();
        await Assert.That(factoryServices[1].ImplementationType is null).IsTrue();
        await Assert.That(factoryServices[2].ImplementationType is null).IsTrue();
        await Assert.That(factoryServices[3].ImplementationType is null).IsTrue();
        await Assert.That(factoryServices[4].ImplementationType is null).IsTrue();
        await Assert.That(factoryServices[5].ImplementationType is null).IsTrue();
        await Assert.That(factoryServices[0].Lifetime).IsEqualTo(ServiceLifetime.Transient);
        await Assert.That(factoryServices[2].Lifetime).IsEqualTo(ServiceLifetime.Scoped);
        await Assert.That(factoryServices[4].Lifetime).IsEqualTo(ServiceLifetime.Singleton);

        var keyedServices = new ServiceCollection();
        keyedServices.AddKeyedTransient(typeof(IContract), "t-type", typeof(FirstContract));
        keyedServices.AddKeyedTransient(typeof(IContract), "t-self");
        keyedServices.AddKeyedTransient(typeof(IContract), "t-factory", (_, _) => new FirstContract());
        keyedServices.AddKeyedTransient<IContract, FirstContract>("t-generic-type");
        keyedServices.AddKeyedTransient<IContract>("t-generic-self");
        keyedServices.AddKeyedTransient<IContract>("t-generic-factory", (_, _) => new FirstContract());
        keyedServices.AddKeyedTransient<IContract, FirstContract>("t-generic-typed-factory", (_, _) => new FirstContract());

        keyedServices.AddKeyedScoped(typeof(IContract), "c-type", typeof(SecondContract));
        keyedServices.AddKeyedScoped(typeof(IContract), "c-self");
        keyedServices.AddKeyedScoped(typeof(IContract), "c-factory", (_, _) => new SecondContract());
        keyedServices.AddKeyedScoped<IContract, SecondContract>("c-generic-type");
        keyedServices.AddKeyedScoped<IContract>("c-generic-self");
        keyedServices.AddKeyedScoped<IContract>("c-generic-factory", (_, _) => new SecondContract());
        keyedServices.AddKeyedScoped<IContract, SecondContract>("c-generic-typed-factory", (_, _) => new SecondContract());

        keyedServices.AddKeyedSingleton(typeof(IContract), "s-type", typeof(ThirdContract));
        keyedServices.AddKeyedSingleton(serviceType: typeof(IContract), serviceKey: "s-self");
        keyedServices.AddKeyedSingleton(typeof(IContract), "s-factory", (_, _) => new ThirdContract());
        keyedServices.AddKeyedSingleton(typeof(IContract), "s-instance", new ThirdContract());
        keyedServices.AddKeyedSingleton<IContract, ThirdContract>("s-generic-type");
        keyedServices.AddKeyedSingleton<IContract>("s-generic-self");
        keyedServices.AddKeyedSingleton<IContract>("s-generic-factory", (_, _) => new ThirdContract());
        keyedServices.AddKeyedSingleton<IContract, ThirdContract>("s-generic-typed-factory", (_, _) => new ThirdContract());
        keyedServices.AddKeyedSingleton<IContract>("s-generic-instance", new ThirdContract());

        var keys = new object?[]
        {
            "t-type", "t-self", "t-factory", "t-generic-type", "t-generic-self", "t-generic-factory", "t-generic-typed-factory",
            "c-type", "c-self", "c-factory", "c-generic-type", "c-generic-self", "c-generic-factory", "c-generic-typed-factory",
            "s-type", "s-self", "s-factory", "s-instance", "s-generic-type", "s-generic-self", "s-generic-factory", "s-generic-typed-factory", "s-generic-instance",
        };

        await Assert.That(keyedServices.Count).IsEqualTo(keys.Length);
        for (var i = 0; i < keys.Length; i++)
        {
            await Assert.That(keyedServices[i].ServiceKey).IsEqualTo(keys[i]);
            await Assert.That(keyedServices[i].IsKeyedService).IsTrue();
        }
    }

    [Test]
    public async Task KeyedTryAddOverloadsPreserveServiceAndKeyIdentity()
    {
        var services = new ServiceCollection();
        services.TryAddKeyedTransient(typeof(IContract), "t-self");
        services.TryAddKeyedTransient(typeof(IContract), "t-type", typeof(FirstContract));
        services.TryAddKeyedTransient(typeof(IContract), "t-factory", (_, _) => new FirstContract());
        services.TryAddKeyedTransient<IContract>("t-generic-self");
        services.TryAddKeyedTransient<IContract, FirstContract>("t-generic-type");
        services.TryAddKeyedTransient<IContract>("t-generic-factory", (_, _) => new FirstContract());

        services.TryAddKeyedScoped(typeof(IContract), "c-self");
        services.TryAddKeyedScoped(typeof(IContract), "c-type", typeof(SecondContract));
        services.TryAddKeyedScoped(typeof(IContract), "c-factory", (_, _) => new SecondContract());
        services.TryAddKeyedScoped<IContract>("c-generic-self");
        services.TryAddKeyedScoped<IContract, SecondContract>("c-generic-type");
        services.TryAddKeyedScoped<IContract>("c-generic-factory", (_, _) => new SecondContract());

        services.TryAddKeyedSingleton(service: typeof(IContract), serviceKey: "s-self");
        services.TryAddKeyedSingleton(typeof(IContract), "s-type", typeof(ThirdContract));
        services.TryAddKeyedSingleton(typeof(IContract), "s-factory", (_, _) => new ThirdContract());
        services.TryAddKeyedSingleton<IContract>("s-generic-self");
        services.TryAddKeyedSingleton<IContract, ThirdContract>("s-generic-type");
        services.TryAddKeyedSingleton<IContract>("s-instance", new ThirdContract());
        services.TryAddKeyedSingleton<IContract>("s-generic-factory", (_, _) => new ThirdContract());

        await Assert.That(services.Count).IsEqualTo(19);
        await Assert.That(services[0].ServiceKey).IsEqualTo("t-self");
        await Assert.That(services[5].ServiceKey).IsEqualTo("t-generic-factory");
        await Assert.That(services[0].KeyedImplementationType).IsEqualTo(typeof(IContract));
        await Assert.That(services[1].KeyedImplementationType).IsEqualTo(typeof(FirstContract));
        await Assert.That(services[2].KeyedImplementationFactory is not null).IsTrue();
        await Assert.That(services[6].Lifetime).IsEqualTo(ServiceLifetime.Scoped);
        await Assert.That(services[12].Lifetime).IsEqualTo(ServiceLifetime.Singleton);
        await Assert.That(services[17].KeyedImplementationInstance is ThirdContract).IsTrue();
        await Assert.That(services[18].KeyedImplementationFactory is not null).IsTrue();

        var unkeyed = new ServiceCollection();
        unkeyed.TryAddTransient<IContract>();
        await Assert.That(unkeyed[0].ImplementationType).IsEqualTo(typeof(IContract));
        await Assert.That(unkeyed[0].Lifetime).IsEqualTo(ServiceLifetime.Transient);
        unkeyed.TryAddScoped<IContract>();
        unkeyed.TryAddSingleton<IContract>();
        unkeyed.TryAddSingleton<IContract>(new FirstContract());
        await Assert.That(unkeyed.Count).IsEqualTo(1);

        var instanceOnly = new ServiceCollection();
        var singleton = new FirstContract();
        instanceOnly.TryAddSingleton<IContract>(singleton);
        await Assert.That(ReferenceEquals(instanceOnly[0].ImplementationInstance, singleton)).IsTrue();
    }

    [Test]
    public async Task GenericFactoryIdentityPreservesEnumerableDeduplicationAndDiagnostics()
    {
        var services = new ServiceCollection();
        services.AddTransient<IContract, FirstContract>(_ => new FirstContract());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IContract, FirstContract>(_ => new FirstContract()));

        await Assert.That(services.Count).IsEqualTo(1);

        var keyedServices = new ServiceCollection();
        keyedServices.AddKeyedTransient<IContract, FirstContract>("key", (_, _) => new FirstContract());
        keyedServices.TryAddEnumerable(ServiceDescriptor.KeyedTransient<IContract, FirstContract>("key", (_, _) => new FirstContract()));
        await Assert.That(keyedServices.Count).IsEqualTo(1);

        var failure = Capture(() => services.TryAddEnumerable(
            ServiceDescriptor.Transient<IContract>(_ => new FirstContract())));
        await Assert.That(failure is ArgumentException).IsTrue();
        await Assert.That(failure?.Message).IsEqualTo(
            $"Implementation type cannot be '{typeof(IContract)}' because it is indistinguishable from other services registered for '{typeof(IContract)}'. (Parameter 'descriptor')");
    }

    [Test]
    public async Task ProviderExtensionsRetainRequiredAndDynamicEnumerationContracts()
    {
        var instance = new FirstContract();
        IServiceProvider provider = new DelegateProvider(serviceType =>
            serviceType == typeof(IContract) ? instance : null);

        await Assert.That(ReferenceEquals(provider.GetService<IContract>(), instance)).IsTrue();
        await Assert.That(ReferenceEquals(provider.GetRequiredService<IContract>(), instance)).IsTrue();
        await Assert.That(provider.GetService<SecondContract>() is null).IsTrue();

        var requiredFailure = Capture(() => provider.GetRequiredService<SecondContract>());
        await Assert.That(requiredFailure is InvalidOperationException).IsTrue();
        await Assert.That(requiredFailure?.Message).IsEqualTo($"No service for type '{typeof(SecondContract)}' has been registered.");

        var dynamicFailure = Capture(() => provider.GetServices(typeof(IContract)));
#if NETWASM
        await Assert.That(dynamicFailure is NotSupportedException).IsTrue();
        await Assert.That(dynamicFailure?.Message).IsEqualTo("Dynamic service enumeration by Type requires generated NetWasm activation metadata.");
#else
        await Assert.That(dynamicFailure is InvalidOperationException).IsTrue();
#endif

        var keyedFailure = Capture(() => provider.GetKeyedServices(typeof(IContract), "key"));
#if NETWASM
        await Assert.That(keyedFailure is NotSupportedException).IsTrue();
        await Assert.That(keyedFailure?.Message).IsEqualTo(
            "Dynamic keyed service enumeration by Type requires generated NetWasm activation metadata.");
#else
        await Assert.That(keyedFailure is InvalidOperationException).IsTrue();
#endif
    }

    [Test]
    public async Task KeyedProviderExtensionsForwardGenericLookupContract()
    {
        var instance = new FirstContract();
        IServiceProvider provider = new KeyedDelegateProvider(instance);

        await Assert.That(ReferenceEquals(provider.GetKeyedService<IContract>("key"), instance)).IsTrue();
        await Assert.That(ReferenceEquals(provider.GetRequiredKeyedService<IContract>("key"), instance)).IsTrue();
    }

    [Test]
    public async Task KeyedProviderExtensionsForwardNonGenericLookupContract()
    {
        var instance = new FirstContract();
        IServiceProvider provider = new KeyedDelegateProvider(instance);

        await Assert.That(ReferenceEquals(provider.GetKeyedService(typeof(IContract), "key"), instance)).IsTrue();
        await Assert.That(ReferenceEquals(provider.GetRequiredKeyedService(typeof(IContract), "key"), instance)).IsTrue();
        var sequence = provider.GetRequiredKeyedService(typeof(IEnumerable<IContract>), "key");
        await Assert.That(sequence is ContractSequence).IsTrue();
    }

    [Test]
    public async Task KeyedProviderExtensionsForwardGenericSequenceContract()
    {
        var instance = new FirstContract();
        IServiceProvider provider = new KeyedDelegateProvider(instance);

        var keyedServices = provider.GetKeyedServices<IContract>("key");
        await Assert.That(keyedServices is ContractSequence).IsTrue();
        using var enumerator = keyedServices.GetEnumerator();
        await Assert.That(enumerator.MoveNext()).IsTrue();
        await Assert.That(ReferenceEquals(enumerator.Current, instance)).IsTrue();
    }

    [Test]
    public async Task KeyedProviderExtensionsRejectUnsupportedProviders()
    {
        var unsupported = new DelegateProvider(_ => null);
        var failure = Capture(() => unsupported.GetKeyedService(typeof(IContract), "key"));
        await Assert.That(failure is InvalidOperationException).IsTrue();

        var requiredFailure = Capture(() => unsupported.GetRequiredKeyedService(typeof(IContract), "key"));
        await Assert.That(requiredFailure is InvalidOperationException).IsTrue();

        var genericFailure = Capture(() => unsupported.GetKeyedService<IContract>("key"));
        await Assert.That(genericFailure is InvalidOperationException).IsTrue();

        var genericRequiredFailure = Capture(() => unsupported.GetRequiredKeyedService<IContract>("key"));
        await Assert.That(genericRequiredFailure is InvalidOperationException).IsTrue();
#if NETWASM
        const string unsupportedMessage = "This service provider does not support keyed services.";
#else
        const string unsupportedMessage = "This service provider doesn't support keyed services.";
#endif
        await Assert.That(failure?.Message).IsEqualTo(unsupportedMessage);
        await Assert.That(requiredFailure?.Message).IsEqualTo(unsupportedMessage);
        await Assert.That(genericFailure?.Message).IsEqualTo(unsupportedMessage);
        await Assert.That(genericRequiredFailure?.Message).IsEqualTo(unsupportedMessage);
    }

    [Test]
    public async Task AsyncServiceScopeForwardsProviderAndDisposalContracts()
    {
        var synchronousScope = new TrackingScope(new DelegateProvider(_ => null));
        var asyncScope = new AsyncServiceScope(synchronousScope);
        await Assert.That(ReferenceEquals(asyncScope.ServiceProvider, synchronousScope.ServiceProvider)).IsTrue();
        asyncScope.Dispose();
        await Assert.That(synchronousScope.WasDisposed).IsTrue();

        var asynchronousScope = new AsyncTrackingScope(new DelegateProvider(_ => null));
        await new AsyncServiceScope(asynchronousScope).DisposeAsync();
        await Assert.That(asynchronousScope.WasAsynchronouslyDisposed).IsTrue();
        await Assert.That(asynchronousScope.WasDisposed).IsFalse();

        var createdScope = new TrackingScope(new DelegateProvider(_ => null));
        IServiceProvider provider = new DelegateProvider(serviceType =>
            serviceType == typeof(IServiceScopeFactory) ? new ScopeFactory(createdScope) : null);
        await provider.CreateAsyncScope().DisposeAsync();
        await Assert.That(createdScope.WasDisposed).IsTrue();
    }

    [Test]
    public async Task ActivatorUtilitiesRetainsPlatformActivationBoundary()
    {
        IServiceProvider provider = new DelegateProvider(_ => null);
        var createFailure = Capture(() => ActivatorUtilities.CreateInstance<IContract>(provider));
        var getOrCreateFailure = Capture(() => ActivatorUtilities.GetServiceOrCreateInstance<IContract>(provider));

#if NETWASM
        await Assert.That(createFailure is NotSupportedException).IsTrue();
        await Assert.That(getOrCreateFailure is NotSupportedException).IsTrue();
        await Assert.That(createFailure?.Message).IsEqualTo("ActivatorUtilities requires a generated NetWasm service provider.");
#else
        await Assert.That(createFailure is InvalidOperationException).IsTrue();
        await Assert.That(getOrCreateFailure is InvalidOperationException).IsTrue();
#endif
    }

    [Test]
    public async Task ServiceProviderFactoryPreservesBuilderAndProviderContracts()
    {
        var services = new ServiceCollection();
        var factory = new IdentityServiceProviderFactory();
        var builder = factory.CreateBuilder(services);
        var provider = factory.CreateServiceProvider(builder);

        await Assert.That(ReferenceEquals(builder, services)).IsTrue();
        await Assert.That(provider.GetService<IContract>() is null).IsTrue();
        await Assert.That(factory.CreateServiceProvider(builder) is IServiceProvider).IsTrue();
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

    private interface IContract;

    private sealed class FirstContract : IContract;

    private sealed class SecondContract : IContract;

    private sealed class ThirdContract : IContract;

    private class DelegateProvider(Func<Type, object?> resolve) : IServiceProvider
    {
        public object? GetService(Type serviceType) => resolve(serviceType);
    }

    private sealed class KeyedDelegateProvider(object instance) : DelegateProvider(_ => null), IKeyedServiceProvider
    {
        public object? GetKeyedService(Type serviceType, object? serviceKey) =>
            Equals(serviceKey, "key") switch
            {
                true when serviceType == typeof(IContract) => instance,
                true when serviceType == typeof(IEnumerable<IContract>) => new ContractSequence((IContract)instance),
                _ => null,
            };

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
            GetKeyedService(serviceType, serviceKey) ?? throw new InvalidOperationException();
    }

    private sealed class ContractSequence(IContract instance) : IEnumerable<IContract>
    {
        public IEnumerator<IContract> GetEnumerator() => new SingleContractEnumerator(instance);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SingleContractEnumerator(IContract instance) : IEnumerator<IContract>
    {
        private int _position = -1;

        public IContract Current => _position == 0 ? instance : throw new InvalidOperationException();

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_position < 0)
            {
                _position = 0;
                return true;
            }

            _position = 1;
            return false;
        }

        public void Reset() => _position = -1;

        public void Dispose()
        {
        }
    }

    private class TrackingScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public bool WasDisposed { get; private set; }

        public IServiceProvider ServiceProvider => serviceProvider;

        public void Dispose()
        {
            WasDisposed = true;
        }
    }

    private sealed class AsyncTrackingScope(IServiceProvider serviceProvider) : TrackingScope(serviceProvider), IAsyncDisposable
    {
        public bool WasAsynchronouslyDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            WasAsynchronouslyDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ScopeFactory(IServiceScope scope) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => scope;
    }

    private sealed class IdentityServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services) => services;

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) => new DelegateProvider(_ => null);
    }
}
