extern alias netwasmTarget;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using NetWasm.Microsoft.Extensions.DependencyInjection.Generator;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationExtensions;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions;
using GeneratedActivationDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDescriptor;
using GeneratedActivationManifest = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationManifest;
using GeneratedParameter = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedParameter;
using ServiceKeyLookupMode = Microsoft.Extensions.DependencyInjection.ServiceKeyLookupMode;
using IClock = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.IClock;
using Clock = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.Clock;
using IEmpty = netwasmTarget::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures.IEmpty;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Tests;

public sealed class DI01DTests
{
    private static readonly string[] ExpectedUnkeyedSequence = ["first", "second"];
    private static readonly string[] ExpectedKeyedSequence = ["blue-one", "any-one"];
    private static readonly string[] ExpectedDisposalOrder = ["sync", "async"];
    private static readonly int[] ExpectedValueSequence = [1, 2];

    [Fact]
    public void KeyedGeneratedServicesUseExactThenAnyKeyAndScopesForwardKeyedAccess()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddGeneratedSingleton(Descriptor("unkeyed", "unkeyed"));
        services.AddGeneratedKeyedSingleton("blue", Descriptor("exact", "blue"));
        services.AddGeneratedKeyedSingleton(KeyedService.AnyKey, Descriptor("any", "any"));

        using var provider = services.BuildServiceProvider();
        var exact = provider.GetRequiredKeyedService<IClock>("blue");
        var fallback = provider.GetRequiredKeyedService<IClock>("red");
        Assert.Equal("blue", exact.Name);
        Assert.Equal("any", fallback.Name);
        Assert.Equal("unkeyed", provider.GetRequiredKeyedService<IClock>(null).Name);
        Assert.Same(exact, provider.GetRequiredKeyedService<IClock>("blue"));
        Assert.True(((IServiceProviderIsKeyedService)provider).IsKeyedService(typeof(IClock), "blue"));
        Assert.True(provider.GetRequiredService<IServiceProviderIsKeyedService>().IsKeyedService(typeof(IClock), "red"));
        Assert.False(((IServiceProviderIsKeyedService)provider).IsKeyedService(typeof(IEmpty), "red"));
        Assert.Throws<InvalidOperationException>(() => provider.GetKeyedService(typeof(IClock), KeyedService.AnyKey));
        Assert.Equal(["blue", "any"], provider.GetKeyedServices<IClock>(KeyedService.AnyKey).Select(clock => clock.Name));

        var keyedMetadata = new GeneratedActivationDescriptor(
            "keyed-metadata", typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock("metadata"),
            Array.Empty<GeneratedActivationDescriptor>(), false, Array.Empty<Type>(), serviceKey: "blue");
        Assert.Throws<ArgumentException>(() => services.AddGenerated("red", keyedMetadata, ServiceLifetime.Transient));
        services.AddGenerated("blue", keyedMetadata, ServiceLifetime.Transient);
        Assert.Contains("ImplementationType", services[^1].ToString(), StringComparison.Ordinal);

        using var scope = provider.CreateScope();
        Assert.Same(exact, scope.ServiceProvider.GetRequiredKeyedService<IClock>("blue"));
        services.AddGeneratedKeyedScoped("scoped", Descriptor("scoped", "scoped"));
        services.AddGeneratedKeyedTransient("transient", Descriptor("transient", "transient"));
    }

    [Fact]
    public void KeyedGeneratedParametersUseExplicitAndInheritedKeys()
    {
        var services = new ServiceCollection();
        var keyedClock = Descriptor("blue-clock", "blue");
        var explicitConsumer = new GeneratedActivationDescriptor(
            "explicit-consumer",
            typeof(ExplicitConsumer),
            typeof(ExplicitConsumer),
            new[] { new GeneratedParameter(typeof(IClock), "blue", lookupMode: ServiceKeyLookupMode.ExplicitKey) },
            values => new ExplicitConsumer((IClock)values[0]!));
        var inheritedConsumer = new GeneratedActivationDescriptor(
            "inherited-consumer",
            typeof(InheritedConsumer),
            typeof(InheritedConsumer),
            new[] { new GeneratedParameter(typeof(IClock), serviceKey: null, lookupMode: ServiceKeyLookupMode.InheritKey) },
            values => new InheritedConsumer((IClock)values[0]!),
            Array.Empty<GeneratedActivationDescriptor>(),
            isPreferred: false,
            assignableTypes: Array.Empty<Type>(),
            serviceKey: "blue");

        services.AddGeneratedKeyedSingleton("blue", keyedClock);
        services.AddGeneratedTransient(explicitConsumer);
        services.AddGeneratedKeyedTransient("blue", inheritedConsumer);
        using var provider = services.BuildServiceProvider();

        Assert.Equal("blue", provider.GetRequiredService<ExplicitConsumer>().Clock.Name);
        Assert.Equal("blue", provider.GetRequiredKeyedService<InheritedConsumer>("blue").Clock.Name);
    }

    [Fact]
    public void KeyedInstanceAndFactoryDescriptorsPreserveKeyAndLifetime()
    {
        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(typeof(IClock), "instance", new Clock("instance")));
        services.Add(new ServiceDescriptor(
            typeof(IClock), "factory", (provider, key) => new Clock("factory:" + key), ServiceLifetime.Transient));
        using var provider = services.BuildServiceProvider();

        Assert.Equal("instance", provider.GetRequiredKeyedService<IClock>("instance").Name);
        var first = provider.GetRequiredKeyedService<IClock>("factory");
        var second = provider.GetRequiredKeyedService<IClock>("factory");
        Assert.Equal("factory:factory", first.Name);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void GeneratedSequencesPreserveRegistrationOrderAndTypedMaterialization()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddGeneratedTransient(Descriptor("first", "first"));
        services.AddGeneratedTransient(Descriptor("second", "second"));
        services.AddGeneratedKeyedTransient("blue", Descriptor("blue-one", "blue-one"));
        services.AddGeneratedKeyedTransient(KeyedService.AnyKey, Descriptor("any-one", "any-one"));
        services.Add(new ServiceDescriptor(typeof(int), 1));
        services.Add(new ServiceDescriptor(typeof(int), 2));
        using var provider = services.BuildServiceProvider();

        var unkeyed = provider.GetRequiredService<IEnumerable<IClock>>().ToArray();
        var keyed = provider.GetRequiredKeyedService<IEnumerable<IClock>>("blue").ToArray();
        var values = provider.GetRequiredService<IEnumerable<int>>().ToArray();
        Assert.Equal(ExpectedUnkeyedSequence, unkeyed.Select(clock => clock.Name));
        Assert.Equal(ExpectedKeyedSequence, keyed.Select(clock => clock.Name));
        Assert.Equal(ExpectedValueSequence, values);
    }

    [Fact]
    public void GeneratedSequenceMetadataResolvesAnEmptyTypedSequence()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var empty = provider.GetRequiredService<IEnumerable<IEmpty>>();
        Assert.Empty(empty);
        Assert.IsType<IEmpty[]>(empty);
    }

    [Fact]
    public async Task AsyncScopeDisposesAsyncAndSyncDisposablesInReverseOrder()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "async-disposable", typeof(AsyncDisposableService), typeof(AsyncDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new AsyncDisposableService(events)));
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "sync-disposable", typeof(SyncDisposableService), typeof(SyncDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new SyncDisposableService(events)));
        await using var provider = services.BuildServiceProvider();
        await using (var scope = ((IServiceProvider)provider).CreateAsyncScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<AsyncDisposableService>();
            _ = scope.ServiceProvider.GetRequiredService<SyncDisposableService>();
        }

        Assert.Equal(ExpectedDisposalOrder, events);
    }

    [Fact]
    public async Task AsyncScopeUsesTheAsyncPathForDualDisposableOwnership()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "dual-disposable", typeof(DualDisposableService), typeof(DualDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new DualDisposableService(events)));
        using var provider = services.BuildServiceProvider();
        var scope = ((IServiceProvider)provider).CreateAsyncScope();
        _ = scope.ServiceProvider.GetRequiredService<DualDisposableService>();
        await scope.DisposeAsync();

        Assert.Equal(["dual-async"], events);
    }

    [Fact]
    public async Task AsyncScopeAwaitsSuspendedDisposalBeforeReturning()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "suspended-disposable", typeof(SuspendedDisposableService), typeof(SuspendedDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new SuspendedDisposableService(events)));
        using var provider = services.BuildServiceProvider();
        var scope = ((IServiceProvider)provider).CreateAsyncScope();
        _ = scope.ServiceProvider.GetRequiredService<SuspendedDisposableService>();
        await scope.DisposeAsync();

        Assert.Equal(["suspended"], events);
    }

    [Fact]
    public async Task AsyncScopeStopsAtTheFirstFaultInReverseOwnershipOrder()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "before-fault", typeof(AsyncDisposableService), typeof(AsyncDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new AsyncDisposableService(events)));
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "fault", typeof(FaultingAsyncDisposableService), typeof(FaultingAsyncDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new FaultingAsyncDisposableService(events)));
        using var provider = services.BuildServiceProvider();
        var scope = ((IServiceProvider)provider).CreateAsyncScope();
        _ = scope.ServiceProvider.GetRequiredService<AsyncDisposableService>();
        _ = scope.ServiceProvider.GetRequiredService<FaultingAsyncDisposableService>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await scope.DisposeAsync());
        Assert.Equal("fault", exception.Message);
        Assert.Equal(["fault"], events);
    }

    [Fact]
    public void SyncScopeRejectsAsyncOnlyDisposal()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddGeneratedScoped(new GeneratedActivationDescriptor(
            "async-only", typeof(AsyncDisposableService), typeof(AsyncDisposableService), Array.Empty<GeneratedParameter>(),
            _ => new AsyncDisposableService(new List<string>())));
        using var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<AsyncDisposableService>();
        Assert.Throws<InvalidOperationException>(() => scope.Dispose());
    }

    [Fact]
    public void GeneratedKeyedParameterSourceCompilesAndExecutes()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            namespace Fixture;
            public interface IClock { string Name { get; } }
            public sealed class Clock : IClock { public string Name => "blue"; }
            public sealed class Consumer
            {
                public Consumer([FromKeyedServices("blue")] IClock clock) { Clock = clock; }
                public IClock Clock { get; }
            }
            """;
        var input = CreateCompilation(source);
        var service = input.GetTypeByMetadataName("Fixture.Consumer")!;
        var clockService = input.GetTypeByMetadataName("Fixture.IClock")!;
        var clock = input.GetTypeByMetadataName("Fixture.Clock")!;
        var adapter = new RegistrationSyntaxAdapter();
        var clockModel = GeneratorComposition.CreateActivationModelBuilder().Build(
            adapter.Read(new RegistrationSyntaxInput(clockService, clock, null, "Singleton")));
        var consumerModel = GeneratorComposition.CreateActivationModelBuilder().Build(
            adapter.Read(new RegistrationSyntaxInput(service, service, null, "Transient")));
        var clockSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(clockModel);
        var consumerSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(consumerModel);
        Assert.Contains("ServiceKeyLookupMode.ExplicitKey", consumerSource, StringComparison.Ordinal);
        Assert.Contains("serviceKey: \"blue\"", consumerSource, StringComparison.Ordinal);

        var generated = CreateCompilation(source, clockSource, consumerSource);
        using var assemblyStream = new MemoryStream();
        var emit = generated.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var clockDescriptor = CreateDescriptor(assembly, clockModel.Facts);
        var consumerDescriptor = CreateDescriptor(assembly, consumerModel.Facts);
        var services = new ServiceCollection();
        services.AddGeneratedKeyedSingleton("blue", clockDescriptor);
        services.AddGeneratedTransient(consumerDescriptor);
        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService(consumerDescriptor.ServiceType);
        Assert.Equal("blue", result.GetType().GetProperty("Clock")!.GetValue(result)!.GetType().GetProperty("Name")!.GetValue(
            result.GetType().GetProperty("Clock")!.GetValue(result)));
    }

    [Fact]
    public void GeneratedSequenceSourceCompilesAndExecutes()
    {
        const string source = """
            namespace Fixture;
            public interface IClock { }
            public readonly record struct ValueElement(int Value);
            public sealed class GenericElement<T>
            {
                public GenericElement(T value) { Value = value; }
                public T Value { get; }
            }
            public static class Values
            {
                public static ValueElement One => new(1);
                public static ValueElement Two => new(2);
                public static GenericElement<int> Three => new(3);
            }
            """;
        var input = CreateCompilationNamed("DI01DSequenceInput", source);
        var valueSequence = new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.ValueElement>",
            "global::Fixture.ValueElement");
        var genericSequence = new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.GenericElement<int>>",
            "global::Fixture.GenericElement<int>");
        var sequence = new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.IClock>",
            "global::Fixture.IClock");
        var manifestSource = GeneratorComposition.CreateActivationManifestEmitter().Emit(
            Array.Empty<ActivationModel>(), new[] { valueSequence, genericSequence, sequence });
        var generated = CreateCompilationNamed("DI01DSequenceGenerated", source, manifestSource);
        using var assemblyStream = new MemoryStream();
        var emit = generated.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        Assert.Equal(3, manifest.Sequences.Count);
        Assert.Equal("global::Fixture.ValueElement", valueSequence.ElementTypeDisplay);

        var valuesType = assembly.GetType("Fixture.Values")!;
        var valueOne = valuesType.GetProperty("One")!.GetValue(null)!;
        var valueTwo = valuesType.GetProperty("Two")!.GetValue(null)!;
        var genericThree = valuesType.GetProperty("Three")!.GetValue(null)!;
        var valueResult = (Array)manifest.Sequences[0].Materialize(new[] { valueOne, valueTwo })!;
        var genericResult = (Array)manifest.Sequences[1].Materialize(new[] { genericThree })!;
        Assert.Equal(2, valueResult.Length);
        Assert.Equal(1, valueResult.GetValue(0)!.GetType().GetProperty("Value")!.GetValue(valueResult.GetValue(0)));
        Assert.Single(genericResult.Cast<object>());
        Assert.Equal(3, genericResult.GetValue(0)!.GetType().GetProperty("Value")!.GetValue(genericResult.GetValue(0)));
    }

    private static GeneratedActivationDescriptor Descriptor(string identity, string name, object? serviceKey = null) =>
        new(identity, typeof(IClock), typeof(Clock), Array.Empty<GeneratedParameter>(), _ => new Clock(name),
            Array.Empty<GeneratedActivationDescriptor>(), false, Array.Empty<Type>(), serviceKey: serviceKey);

    private static GeneratedActivationDescriptor CreateDescriptor(Assembly assembly, RegistrationFacts facts)
    {
        var type = assembly.GetType(facts.NamespaceName + "." + facts.TypeName)!;
        return (GeneratedActivationDescriptor)type.GetMethod("Create")!.Invoke(null, null)!;
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
        => CreateCompilationNamed("DI01DGeneratedFixture", sources);

    private static CSharpCompilation CreateCompilationNamed(string assemblyName, params string[] sources)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        AddReference(references, typeof(GeneratedActivationDescriptor).Assembly.Location);
        AddReference(references, typeof(IServiceCollection).Assembly.Location);
        return CSharpCompilation.Create(
            assemblyName,
            sources.Select(text => CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview))),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static void AddReference(List<MetadataReference> references, string path)
    {
        if (references.All(reference => !string.Equals(reference.Display, path, StringComparison.OrdinalIgnoreCase)))
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }
    }

    private sealed class ExplicitConsumer(IClock clock)
    {
        public IClock Clock { get; } = clock;
    }

    private sealed class InheritedConsumer(IClock clock)
    {
        public IClock Clock { get; } = clock;
    }

    private sealed class AsyncDisposableService(List<string> events) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            events.Add("async");
            return default;
        }
    }

    private sealed class SyncDisposableService(List<string> events) : IDisposable
    {
        public void Dispose() => events.Add("sync");
    }

    private sealed class DualDisposableService(List<string> events) : IDisposable, IAsyncDisposable
    {
        public void Dispose() => events.Add("dual-sync");

        public ValueTask DisposeAsync()
        {
            events.Add("dual-async");
            return default;
        }
    }

    private sealed class SuspendedDisposableService(List<string> events) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            events.Add("suspended");
        }
    }

    private sealed class FaultingAsyncDisposableService(List<string> events) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            events.Add("fault");
            return ValueTask.FromException(new InvalidOperationException("fault"));
        }
    }
}
