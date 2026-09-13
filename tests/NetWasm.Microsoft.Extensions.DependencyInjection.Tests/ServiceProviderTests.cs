extern alias netwasmTarget;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationExtensions;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions;
using GeneratedActivationDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDescriptor;
using GeneratedActivationManifest = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationManifest;
using GeneratedParameter = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedParameter;
using ServiceProviderOptions = netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceProviderOptions;
using IGeneratedActivator = netwasmTarget::Microsoft.Extensions.DependencyInjection.IGeneratedActivator;
using GeneratedActivationDefinition = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDefinition;
using GeneratedActivationLookup = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookup;
using GeneratedActivationLookupBuilder = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookupBuilder;
using GeneratedActivationInitializer = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationInitializer;
using GeneratedActivationRegistrationReader = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationReader;
using GeneratedActivationAssignabilityReader = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationAssignabilityReader;
using GeneratedFactoryShapeSelector = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryShapeSelector;
using GeneratedFactoryCandidate = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryCandidate;
using IClock = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.IClock;
using Clock = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.Clock;
using Consumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.Consumer;
using ConsumerWithZone = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.ConsumerWithZone;
using MultiArgumentConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.MultiArgumentConsumer;
using AmbiguousArguments = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.AmbiguousArguments;
using AmbiguousConstructors = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.AmbiguousConstructors;
using AvailabilityConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.AvailabilityConsumer;
using BaseDependency = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.BaseDependency;
using DerivedDependency = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.DerivedDependency;
using InterfaceConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.InterfaceConsumer;
using BaseConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.BaseConsumer;
using PreferredCatalogConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.PreferredCatalogConsumer;
using LongestCatalogConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.LongestCatalogConsumer;
using DefaultConsumer = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.DefaultConsumer;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Tests;

public sealed class ServiceProviderTests
{
    private static readonly string[] ExpectedDisposalOrder = ["second", "first"];

    [Fact]
    public void GeneratedActivationResolvesDependenciesAndHonoursLifetimes()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "consumer", typeof(Consumer), typeof(Consumer), new[] { new GeneratedParameter(typeof(IClock)) },
            values => new Consumer((IClock)values[0]!)));

        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var firstValue = first.ServiceProvider.GetRequiredService<Consumer>();
        var firstAgain = first.ServiceProvider.GetRequiredService<Consumer>();
        var secondValue = second.ServiceProvider.GetRequiredService<Consumer>();
        var activatedInScope = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Consumer>(first.ServiceProvider);

        Assert.Same(firstValue, firstAgain);
        Assert.NotSame(firstValue, secondValue);
        Assert.Same(provider.GetRequiredService<IClock>(), firstValue.Clock);
        Assert.Same(provider.GetRequiredService<IClock>(), activatedInScope.Clock);
        Assert.True(provider.IsService(typeof(IServiceProvider)));
        Assert.True(provider.GetRequiredService<IServiceProviderIsService>().IsService(typeof(IClock)));
        Assert.False(provider.IsService(typeof(string)));
        Assert.NotNull(provider.GetRequiredService<IServiceProvider>());
        Assert.Same(first.ServiceProvider, first.ServiceProvider.GetRequiredService<IServiceProvider>());
    }

    [Fact]
    public void TransientActivationCreatesOneInstancePerResolution()
    {
        var services = new ServiceCollection();
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "transient-clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));

        using var provider = services.BuildServiceProvider();
        Assert.NotSame(provider.GetRequiredService<IClock>(), provider.GetRequiredService<IClock>());
    }

    [Fact]
    public void GeneratedActivatorReceivesSuppliedArgumentsAndRejectsUngeneratedTypes()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "consumer", typeof(Consumer), typeof(Consumer), new[] { new GeneratedParameter(typeof(IClock)) },
            values => new Consumer((IClock)values[0]!)));

        using var provider = services.BuildServiceProvider();
        var consumer = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Consumer>(provider);

        Assert.NotNull(consumer.Clock);
        Assert.Throws<InvalidOperationException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Unregistered>(provider));
        Assert.Throws<ArgumentException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Consumer>(provider, "unexpected"));
    }

    [Fact]
    public void ActivatorUtilitiesMatchesSuppliedShapesAndConstructorAlternatives()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "consumer-with-zone", typeof(ConsumerWithZone), typeof(ConsumerWithZone),
            new[] { new GeneratedParameter(typeof(IClock)), new GeneratedParameter(typeof(string), hasDefaultValue: true, defaultValue: "UTC") },
            values => new ConsumerWithZone((IClock)values[0]!, (string)values[1]!),
            new[]
            {
                new GeneratedActivationDescriptor(
                    "consumer-with-zone-fallback", typeof(ConsumerWithZone), typeof(ConsumerWithZone),
                    new[] { new GeneratedParameter(typeof(IClock)) },
                    values => new ConsumerWithZone((IClock)values[0]!, "UTC")),
            }));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "multi-argument", typeof(MultiArgumentConsumer), typeof(MultiArgumentConsumer),
            new[]
            {
                new GeneratedParameter(typeof(IClock)),
                new GeneratedParameter(typeof(string)),
                new GeneratedParameter(typeof(int)),
            },
            values => new MultiArgumentConsumer((IClock)values[0]!, (string)values[1]!, (int)values[2]!)));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "ambiguous-arguments", typeof(AmbiguousArguments), typeof(AmbiguousArguments),
            new[] { new GeneratedParameter(typeof(string)), new GeneratedParameter(typeof(string)) },
            values => new AmbiguousArguments((string)values[0]!, (string)values[1]!)));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "ambiguous-constructors", typeof(AmbiguousConstructors), typeof(AmbiguousConstructors),
            new[] { new GeneratedParameter(typeof(IClock)) },
            values => new AmbiguousConstructors((IClock)values[0]!),
            new[]
            {
                new GeneratedActivationDescriptor(
                    "ambiguous-constructors-alt", typeof(AmbiguousConstructors), typeof(AmbiguousConstructors),
                    new[] { new GeneratedParameter(typeof(IClock)) },
                    values => new AmbiguousConstructors((IClock)values[0]!)),
            }));

        using var provider = services.BuildServiceProvider();
        Assert.Equal("UTC", netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<ConsumerWithZone>(provider).Zone);
        Assert.Equal("local", netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<ConsumerWithZone>(provider, "local").Zone);

        var factory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<ConsumerWithZone>(new[] { typeof(string) });
        Assert.Equal("factory", ((ConsumerWithZone)factory(provider, new object[] { "factory" })).Zone);
        var genericFactory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<ConsumerWithZone>(new[] { typeof(string) });
        Assert.Equal("generic", ((ConsumerWithZone)genericFactory(provider, new object[] { "generic" })).Zone);
        Assert.Equal(
            "UTC",
            netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.GetServiceOrCreateInstance<ConsumerWithZone>(provider).Zone);

        var multiFactory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<MultiArgumentConsumer>(
            new[] { typeof(int), typeof(string), typeof(IClock) });
        var multi = (MultiArgumentConsumer)multiFactory(
            provider,
            new object[] { 7, "reordered", provider.GetRequiredService<IClock>() });
        Assert.Equal(7, multi.Count);
        Assert.Equal("reordered", multi.Zone);

        Assert.Throws<ArgumentException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<ConsumerWithZone>(provider, 42));
        Assert.Throws<ArgumentException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<AmbiguousArguments>(provider, "one"));
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<AmbiguousConstructors>());
    }

    [Fact]
    public void ConstructorSelectionUsesAvailabilityAndHonoursPreferredMetadata()
    {
        var fallbackServices = new ServiceCollection();
        fallbackServices.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "availability", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer),
            new[] { new GeneratedParameter(typeof(IClock)) },
            values => new AvailabilityConsumer((IClock)values[0]!),
            new[]
            {
                new GeneratedActivationDescriptor(
                    "availability-fallback", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer),
                    Array.Empty<GeneratedParameter>(), _ => new AvailabilityConsumer()),
            }));
        using var fallbackProvider = fallbackServices.BuildServiceProvider();
        Assert.False(fallbackProvider.GetRequiredService<AvailabilityConsumer>().UsedClock);

        var preferredServices = new ServiceCollection();
        preferredServices.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "preferred", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer),
            new[] { new GeneratedParameter(typeof(IClock)) },
            values => new AvailabilityConsumer((IClock)values[0]!),
            new[]
            {
                new GeneratedActivationDescriptor(
                    "preferred-fallback", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer),
                    Array.Empty<GeneratedParameter>(), _ => new AvailabilityConsumer()),
            },
            isPreferred: true));
        using var preferredProvider = preferredServices.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => preferredProvider.GetRequiredService<AvailabilityConsumer>());
    }

    [Fact]
    public void FactorySelectionIsImmediateAndUsesGeneratedAssignabilityMaps()
    {
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "derived", typeof(BaseDependency), typeof(DerivedDependency), Array.Empty<GeneratedParameter>(), _ => new DerivedDependency()));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "interface-consumer", typeof(InterfaceConsumer), typeof(InterfaceConsumer),
            new[] { new GeneratedParameter(typeof(IClock)) }, values => new InterfaceConsumer((IClock)values[0]!)));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "base-consumer", typeof(BaseConsumer), typeof(BaseConsumer),
            new[] { new GeneratedParameter(typeof(BaseDependency)) }, values => new BaseConsumer((BaseDependency)values[0]!)));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "availability-factory", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer),
            new[] { new GeneratedParameter(typeof(IClock)) }, values => new AvailabilityConsumer((IClock)values[0]!),
            new[]
            {
                new GeneratedActivationDescriptor("availability-empty", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer), Array.Empty<GeneratedParameter>(), _ => new AvailabilityConsumer()),
            }));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "ambiguous-factory", typeof(AmbiguousConstructors), typeof(AmbiguousConstructors),
            new[] { new GeneratedParameter(typeof(IClock)) }, values => new AmbiguousConstructors((IClock)values[0]!),
            new[]
            {
                new GeneratedActivationDescriptor("ambiguous-factory-alt", typeof(AmbiguousConstructors), typeof(AmbiguousConstructors),
                    new[] { new GeneratedParameter(typeof(IClock)) }, values => new AmbiguousConstructors((IClock)values[0]!)),
            }));

        Assert.Throws<ArgumentException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<BaseConsumer>(
            new[] { typeof(string) }));
        var baseFactory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<BaseConsumer>(
            new[] { typeof(DerivedDependency) });
        var interfaceFactory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<InterfaceConsumer>(
            new[] { typeof(Clock) });
        var availabilityFactory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<AvailabilityConsumer>(Array.Empty<Type>());
        Assert.Throws<InvalidOperationException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<AmbiguousConstructors>(
            new[] { typeof(IClock) }));

        using var provider = services.BuildServiceProvider();
        var directBase = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<BaseConsumer>(provider, new DerivedDependency());
        Assert.IsType<DerivedDependency>(directBase.Dependency);
        var directInterface = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<InterfaceConsumer>(provider, new Clock());
        Assert.IsType<Clock>(directInterface.Clock);
        var derived = (BaseConsumer)baseFactory(provider, new object[] { new DerivedDependency() });
        Assert.IsType<DerivedDependency>(derived.Dependency);
        var interfaceValue = (InterfaceConsumer)interfaceFactory(provider, new object[] { new Clock() });
        Assert.IsType<Clock>(interfaceValue.Clock);
        var unavailableServices = new ServiceCollection();
        unavailableServices.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "availability-factory-missing", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer),
            new[] { new GeneratedParameter(typeof(IClock)) }, values => new AvailabilityConsumer((IClock)values[0]!),
            new[]
            {
                new GeneratedActivationDescriptor("availability-empty-missing", typeof(AvailabilityConsumer), typeof(AvailabilityConsumer), Array.Empty<GeneratedParameter>(), _ => new AvailabilityConsumer()),
            }));
        using var unavailableProvider = unavailableServices.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => availabilityFactory(unavailableProvider, Array.Empty<object>()));
    }

    [Fact]
    public void NullSuppliedValueMapsToTheOnlyGeneratedParameter()
    {
        var services = new ServiceCollection();
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "default-consumer", typeof(DefaultConsumer), typeof(DefaultConsumer),
            new[] { new GeneratedParameter(typeof(string), hasDefaultValue: true, defaultValue: "fallback") },
            values => new DefaultConsumer((string)values[0]!)));

        using var provider = services.BuildServiceProvider();
        var value = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<DefaultConsumer>(provider, (object?)null);
        Assert.Null(value.Value);
    }

    [Fact]
    public void GeneratedFactoryLookupValidatesClosedMetadata()
    {
        Assert.Throws<ArgumentNullException>(() => new GeneratedFactoryCandidate(null!, Array.Empty<Type>(), false));
        Assert.Throws<ArgumentNullException>(() => new GeneratedFactoryCandidate("null-types", null!, false));
        Assert.Throws<ArgumentException>(() => new GeneratedFactoryCandidate("null-item", (IReadOnlyList<Type>)(object)new Type?[] { null }, false));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDefinition(
            "null-parameter-type", typeof(string), typeof(string),
            (IReadOnlyList<Type>)(object)new Type?[] { null }, Array.Empty<GeneratedActivationDefinition>(), false, Array.Empty<Type>()));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDefinition(
            "null-alternative", typeof(string), typeof(string), Array.Empty<Type>(),
            (IReadOnlyList<GeneratedActivationDefinition>)(object)new GeneratedActivationDefinition?[] { null }, false, Array.Empty<Type>()));
        var builder = new GeneratedActivationLookupBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.Build(null!));
        Assert.Throws<ArgumentException>(() => builder.Build(
            (IReadOnlyList<GeneratedActivationDefinition>)(object)new GeneratedActivationDefinition?[] { null }));
        var ambiguous = Definition(
            "ambiguous-shape", typeof(AmbiguousArguments), typeof(AmbiguousArguments),
            new[] { typeof(string), typeof(string) });
        var lookup = builder.Build(new[] { ambiguous });
        var selector = CreateSelector(lookup);
        var registrationReader = new GeneratedActivationRegistrationReader(lookup);
        var assignabilityReader = new GeneratedActivationAssignabilityReader(lookup);
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationRegistrationReader(null!));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationAssignabilityReader(null!));
        Assert.Throws<ArgumentNullException>(() => new GeneratedFactoryShapeSelector(null!, assignabilityReader));
        Assert.Throws<ArgumentNullException>(() => new GeneratedFactoryShapeSelector(registrationReader, null!));
        Assert.Throws<ArgumentNullException>(() => selector.Select(null!, Array.Empty<Type>()));
        Assert.Throws<ArgumentNullException>(() => selector.Select(typeof(AmbiguousArguments), null!));
        Assert.Throws<ArgumentException>(() => selector.Select(typeof(Unregistered), Array.Empty<Type>()));
        Assert.Throws<ArgumentException>(() => selector.Select(typeof(AmbiguousArguments), new[] { typeof(string) }));
        Assert.Throws<ArgumentException>(() => selector.Select(
            typeof(AmbiguousArguments),
            new[] { typeof(string), typeof(string), typeof(string) }));
        Assert.False(assignabilityReader.IsAssignable(typeof(string), typeof(int)));
        var assignable = Definition(
            "assignable", typeof(LongestCatalogConsumer), typeof(LongestCatalogConsumer), Array.Empty<Type>(),
            assignableTypes: new[] { typeof(IDisposable) });
        var assignableLookup = builder.Build(new[] { assignable });
        Assert.True(new GeneratedActivationAssignabilityReader(assignableLookup)
            .IsAssignable(typeof(LongestCatalogConsumer), typeof(IDisposable)));
        var ordered = Definition(
            "ordered", typeof(PreferredCatalogConsumer), typeof(PreferredCatalogConsumer), new[] { typeof(string), typeof(int) });
        var orderedLookup = builder.Build(new[] { ordered });
        Assert.Throws<ArgumentException>(() => CreateSelector(orderedLookup).Select(typeof(PreferredCatalogConsumer), new[] { typeof(string), typeof(string) }));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationInitializer(null!));
        var initializer = new GeneratedActivationInitializer(builder);
        Assert.Throws<ArgumentNullException>(() => initializer.Initialize(null!));
        _ = initializer.Initialize(Array.Empty<GeneratedActivationDefinition>());
        _ = initializer.Initialize(Array.Empty<GeneratedActivationDefinition>());

        var preferred = Definition(
            "preferred", typeof(PreferredCatalogConsumer), typeof(PreferredCatalogConsumer), new[] { typeof(int) }, isPreferred: true,
            alternatives: new[] { Definition("preferred-fallback", typeof(PreferredCatalogConsumer), typeof(PreferredCatalogConsumer), Array.Empty<Type>()) });
        var preferredLookup = builder.Build(new[] { preferred });
        var preferredSelector = CreateSelector(preferredLookup);
        Assert.Throws<ArgumentException>(() => preferredSelector.Select(typeof(PreferredCatalogConsumer), new[] { typeof(string) }));
        var selectedPreferred = preferredSelector.Select(typeof(PreferredCatalogConsumer), new[] { typeof(int) });
        Assert.Equal("preferred", selectedPreferred.Identity);

        var longest = Definition(
            "longest", typeof(LongestCatalogConsumer), typeof(LongestCatalogConsumer), new[] { typeof(int) },
            alternatives: new[] { Definition("shorter", typeof(LongestCatalogConsumer), typeof(LongestCatalogConsumer), Array.Empty<Type>()) });
        var longestLookup = builder.Build(new[] { longest });
        Assert.Equal("longest", CreateSelector(longestLookup).Select(typeof(LongestCatalogConsumer), Array.Empty<Type>()).Identity);
    }

    [Fact]
    public void ActivatorUtilitiesValidatesPublicFactoryBoundaries()
    {
        Assert.Throws<ArgumentNullException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Consumer>(null!));
        Assert.Throws<ArgumentNullException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(
            new EmptyProvider(), null!, Array.Empty<object?>()));
        Assert.Throws<ArgumentNullException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(
            new EmptyProvider(), typeof(Consumer), null!));
        Assert.Throws<NotSupportedException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Consumer>(new EmptyProvider()));
        Assert.Throws<ArgumentNullException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(null!, Array.Empty<Type>()));
        var consumerType = typeof(Consumer);
        Assert.Throws<ArgumentNullException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(consumerType, null!));
        var invalidArgumentTypes = (Type[])(object)new Type?[] { null };
        Assert.Throws<ArgumentNullException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(consumerType, invalidArgumentTypes));

        var services = new ServiceCollection();
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "consumer-with-zone", typeof(ConsumerWithZone), typeof(ConsumerWithZone),
            new[] { new GeneratedParameter(typeof(IClock)), new GeneratedParameter(typeof(string), hasDefaultValue: true, defaultValue: "UTC") },
            values => new ConsumerWithZone((IClock)values[0]!, (string)values[1]!)));
        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService(typeof(string)));
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService(typeof(string)));
        var factory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<ConsumerWithZone>(new[] { typeof(string) });
        Assert.Throws<ArgumentException>(() => factory(provider, Array.Empty<object>()));
        Assert.Throws<ArgumentNullException>(() => factory(null!, new object[] { "x" }));
        Assert.Throws<NotSupportedException>(() => factory(new EmptyProvider(), new object[] { "x" }));
        Assert.Throws<InvalidOperationException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Unregistered>(provider, (object?)null));
        Assert.Throws<InvalidOperationException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.GetServiceOrCreateInstance<Unregistered>(provider));

        var nullFactory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<Consumer>(Array.Empty<Type>());
        Assert.Throws<InvalidOperationException>(() => nullFactory(new NullActivatorProvider(), Array.Empty<object>()));
        Assert.Throws<InvalidOperationException>(() => netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<Consumer>(new NullActivatorProvider()));

        var bridge = (IGeneratedActivator)provider;
        Assert.Throws<ArgumentNullException>(() => bridge.Create(null!, Array.Empty<object?>(), Array.Empty<Type?>(), null));
        Assert.Throws<ArgumentNullException>(() => bridge.Create(typeof(Consumer), null!, Array.Empty<Type?>(), null));
        Assert.Throws<ArgumentNullException>(() => bridge.Create(typeof(Consumer), Array.Empty<object?>(), null!, null));
        Assert.Throws<ArgumentException>(() => bridge.Create(typeof(Consumer), Array.Empty<object?>(), new[] { typeof(string) }, null));
        provider.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bridge.Create(typeof(Consumer), Array.Empty<object?>(), Array.Empty<Type?>(), null));
    }

    [Fact]
    public void FactoryAndInstanceDescriptorsUseUpstreamCallSites()
    {
        var clock = new Clock();
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(clock);
        services.AddTransient<Consumer>(provider => new Consumer(provider.GetRequiredService<IClock>()));

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<Consumer>();
        var second = provider.GetRequiredService<Consumer>();

        Assert.NotSame(first, second);
        Assert.Same(clock, first.Clock);
        Assert.Same(clock, provider.GetRequiredService<IClock>());
    }

    [Fact]
    public void GeneratedDefaultParametersResolveWithoutAServiceRegistration()
    {
        var services = new ServiceCollection();
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "default-consumer", typeof(DefaultConsumer), typeof(DefaultConsumer),
            new[] { new GeneratedParameter(typeof(string), hasDefaultValue: true, defaultValue: "default") },
            values => new DefaultConsumer((string)values[0]!)));

        using var provider = services.BuildServiceProvider();
        Assert.Equal("default", provider.GetRequiredService<DefaultConsumer>().Value);
    }

    [Fact]
    public void SynchronousDisposalCapturesScopedAndRootInstances()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "root-disposable", typeof(RootDisposable), typeof(RootDisposable), Array.Empty<GeneratedParameter>(), _ => new RootDisposable(events)));
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "scope-disposable", typeof(ScopeDisposable), typeof(ScopeDisposable), Array.Empty<GeneratedParameter>(), _ => new ScopeDisposable(events)));

        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<ScopeDisposable>();
            _ = scope.ServiceProvider.GetRequiredService<RootDisposable>();
        }

        Assert.Equal(["scope"], events);
        provider.Dispose();
        Assert.Equal(["scope", "root"], events);
    }

    [Fact]
    public void ValidationRejectsMissingDependencyAndSingletonScopedEdge()
    {
        var missing = new ServiceCollection();
        missing.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "missing", typeof(Consumer), typeof(Consumer), new[] { new GeneratedParameter(typeof(IClock)) },
            values => new Consumer((IClock)values[0]!)));
        Assert.Throws<AggregateException>(() => missing.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

        var scopes = new ServiceCollection();
        scopes.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "scoped-clock", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock()));
        scopes.AddGeneratedSingleton(new GeneratedActivationDescriptor(
            "singleton-consumer", typeof(Consumer), typeof(Consumer), new[] { new GeneratedParameter(typeof(IClock)) },
            values => new Consumer((IClock)values[0]!)));
        Assert.Throws<AggregateException>(() => scopes.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        }));

        using var runtimeValidated = scopes.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Assert.Throws<InvalidOperationException>(() => runtimeValidated.GetRequiredService<IClock>());

        using var buildValidated = scopes.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        Assert.NotNull(buildValidated);
    }

    [Fact]
    public async Task AsyncScopeDisposesInReverseCreationOrder()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "first", typeof(FirstDisposable), typeof(FirstDisposable), Array.Empty<GeneratedParameter>(),
            _ => new FirstDisposable(events)));
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "second", typeof(SecondDisposable), typeof(SecondDisposable), Array.Empty<GeneratedParameter>(),
            _ => new SecondDisposable(events)));

        await using var provider = services.BuildServiceProvider();
        await using (var scope = new AsyncServiceScope(((IServiceScopeFactory)provider).CreateScope()))
        {
            _ = scope.ServiceProvider.GetRequiredService<FirstDisposable>();
            _ = scope.ServiceProvider.GetRequiredService<SecondDisposable>();
        }

        Assert.Equal(ExpectedDisposalOrder, events);
        await provider.DisposeAsync();
        await provider.DisposeAsync();
    }

    private static GeneratedFactoryShapeSelector CreateSelector(GeneratedActivationLookup lookup)
    {
        return new GeneratedFactoryShapeSelector(
            new GeneratedActivationRegistrationReader(lookup),
            new GeneratedActivationAssignabilityReader(lookup));
    }

    private static GeneratedActivationDefinition Definition(
        string identity,
        Type serviceType,
        Type implementationType,
        IReadOnlyList<Type> parameterTypes,
        bool isPreferred = false,
        IReadOnlyList<GeneratedActivationDefinition>? alternatives = null,
        IReadOnlyList<Type>? assignableTypes = null) =>
        new(
            identity,
            serviceType,
            implementationType,
            parameterTypes,
            alternatives ?? Array.Empty<GeneratedActivationDefinition>(),
            isPreferred,
            assignableTypes ?? Array.Empty<Type>());

    private sealed class EmptyProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class NullActivatorProvider : IServiceProvider, IGeneratedActivator
    {
        public object? GetService(Type serviceType) => null;

        object? IGeneratedActivator.Create(Type instanceType, object?[] parameters, Type?[] parameterTypes, string? selectedIdentity) => null;
    }

    private sealed class Unregistered { }

    private sealed class FirstDisposable(List<string> events) : IDisposable
    {
        public void Dispose() => events.Add("first");
    }

    private sealed class SecondDisposable(List<string> events) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            events.Add("second");
            return default;
        }
    }

    private sealed class RootDisposable(List<string> events) : IDisposable
    {
        public void Dispose() => events.Add("root");
    }

    private sealed class ScopeDisposable(List<string> events) : IDisposable
    {
        public void Dispose() => events.Add("scope");
    }
}
