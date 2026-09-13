extern alias netwasmTarget;

using GeneratedActivationDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDescriptor;
using GeneratedActivationDefinition = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDefinition;
using GeneratedActivationLookup = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookup;
using GeneratedActivationManifest = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationManifest;
using GeneratedActivationRegistry = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistry;
using GeneratedOpenGenericRegistrationDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedOpenGenericRegistrationDescriptor;
using GeneratedFactoryRegistration = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryRegistration;
using GeneratedParameter = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedParameter;
using GeneratedSequenceDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedSequenceDescriptor;
using GeneratedSequenceDefinition = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedSequenceDefinition;
using GeneratedSequenceKey = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedSequenceKey;
using GeneratedServiceDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedServiceDescriptor;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using NetWasm.Microsoft.Extensions.DependencyInjection.Generator;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationExtensions;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Tests;

public sealed class GeneratedActivationActorTests
{
    [Fact]
    public void GeneratedCatalogActivatesOrdinaryServiceDescriptorsAndActivatorUtilities()
    {
        var activation = new GeneratedActivationDescriptor(
            "runtime-catalog-service",
            typeof(IRuntimeCatalogService),
            typeof(RuntimeCatalogService),
            Array.Empty<GeneratedParameter>(),
            static _ => new RuntimeCatalogService(),
            Array.Empty<GeneratedActivationDescriptor>(),
            isPreferred: true,
            new[] { typeof(IRuntimeCatalogService), typeof(RuntimeCatalogService) });
        GeneratedActivationRegistry.Register(new GeneratedActivationManifest(new[] { activation }));
        var services = new ServiceCollection();
        services.AddSingleton<IRuntimeCatalogService, RuntimeCatalogService>();

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IRuntimeCatalogService>();
        var factory = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory<RuntimeCatalogService>(
            Array.Empty<Type>());
        var direct = factory(provider, Array.Empty<object>());

        Assert.IsType<RuntimeCatalogService>(resolved);
        Assert.IsType<RuntimeCatalogService>(direct);
        Assert.Same(resolved, provider.GetRequiredService<IRuntimeCatalogService>());
    }

    [Fact]
    public void PackageGeneratorReadsOrdinaryRegistrationsAndEmitsAnImmutableCatalog()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using Microsoft.Extensions.DependencyInjection;
            namespace Fixture;
            public interface IClock { }
            public sealed class Clock : IClock { }
            public sealed class Consumer
            {
                public Consumer(IClock clock, IEnumerable<IClock> clocks) { }
            }
            public static class Composition
            {
                public static void Configure(IServiceCollection services, IServiceProvider provider)
                {
                    services.AddSingleton<IClock, Clock>();
                    services.AddScoped<Consumer>();
                    _ = provider.GetServices<IClock>();
                }
            }
            """;
        var compilation = CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var result = driver.GetRunResult();
        var generatedText = string.Join(
            "\n",
            result.Results.Single().GeneratedSources.Select(sourceResult => sourceResult.SourceText.ToString()));

        Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(
            generatedCompilation.GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(4, result.Results.Single().GeneratedSources.Length);
        Assert.Contains("new global::Fixture.Clock(", generatedText, StringComparison.Ordinal);
        Assert.Contains("new global::Fixture.Consumer(", generatedText, StringComparison.Ordinal);
        Assert.Contains("GeneratedSequenceDescriptor", generatedText, StringComparison.Ordinal);
        Assert.Contains("ModuleInitializer", generatedText, StringComparison.Ordinal);
        Assert.Contains("GeneratedActivationRegistry.Register", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public void PackageGeneratorDistinguishesSameServiceAndImplementationByKey()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            namespace Fixture;
            public interface IClock { }
            public sealed class Clock : IClock { }
            public static class Composition
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddKeyedSingleton<IClock, Clock>("blue");
                    services.AddKeyedSingleton<IClock, Clock>("red");
                }
            }
            """;
        var compilation = CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var result = driver.GetRunResult();
        var generatedText = string.Join(
            "\n",
            result.Results.Single().GeneratedSources.Select(sourceResult => sourceResult.SourceText.ToString()));

        Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(
            generatedCompilation.GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(4, result.Results.Single().GeneratedSources.Length);
        Assert.Contains("serviceKey: \"blue\"", generatedText, StringComparison.Ordinal);
        Assert.Contains("serviceKey: \"red\"", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public void PackageGeneratorClosesAnOrdinaryOpenGenericRequestWithoutRuntimeConstruction()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            namespace Fixture;
            public sealed class Order { }
            public interface IRepository<T> { string Name { get; } }
            public sealed class Repository<T> : IRepository<T> where T : class, new()
            {
                public string Name => "closed";
            }
            public static class Composition
            {
                public static object Configure(IServiceCollection services, IServiceProvider provider)
                {
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                    return provider.GetRequiredService(typeof(IRepository<Order>));
                }
            }
            """;
        var compilation = CreateCompilationNamed("OrdinaryOpenGenericFixture", source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var result = driver.GetRunResult();
        var generatedText = string.Join(
            "\n",
            result.Results.Single().GeneratedSources.Select(sourceResult => sourceResult.SourceText.ToString()));

        Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(
            generatedCompilation.GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("typeof(global::Fixture.IRepository<>)", generatedText, StringComparison.Ordinal);
        Assert.Contains("new global::Fixture.Repository<T>", generatedText, StringComparison.Ordinal);
        Assert.Contains("<global::Fixture.Order>.Create()", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", generatedText, StringComparison.Ordinal);

        using var assemblyStream = new MemoryStream();
        var emit = generatedCompilation.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        var activation = Assert.Single(manifest.Activations);
        Assert.Equal(assembly.GetType("Fixture.IRepository`1"), activation.TemplateServiceType);
        Assert.Equal(assembly.GetType("Fixture.Repository`1"), activation.TemplateImplementationType);
        Assert.Single(manifest.OpenGenericRegistrations);

        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(
            activation.TemplateServiceType!,
            activation.TemplateImplementationType!,
            ServiceLifetime.Transient));
        using var provider = services.BuildServiceProvider(
            new netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceProviderOptions
            {
                ValidateOnBuild = true,
            });
        var repository = provider.GetRequiredService(activation.ServiceType);
        Assert.Equal("closed", repository.GetType().GetProperty("Name")!.GetValue(repository));
    }

    [Fact]
    public void PackageGeneratorPreservesUnrequestedOpenGenericForBuildValidation()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            namespace UnrequestedOpenGenericFixture;
            public interface IRepository<T> { }
            public sealed class Repository<T> : IRepository<T> where T : class { }
            public static class Composition
            {
                public static void Configure(IServiceCollection services) =>
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            }
            """;
        var compilation = CreateCompilationNamed(
            "UnrequestedOpenGenericFixture",
            source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var run = driver.GetRunResult();
        var generatedText = string.Join(
            "\n",
            run.Results.Single().GeneratedSources.Select(
                sourceResult => sourceResult.SourceText.ToString()));

        Assert.Empty(generatorDiagnostics.Where(
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(
            generatedCompilation.GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            "GeneratedOpenGenericRegistrationDescriptor",
            generatedText,
            StringComparison.Ordinal);

        using var assemblyStream = new MemoryStream();
        var emit = generatedCompilation.Emit(assemblyStream);
        Assert.True(
            emit.Success,
            string.Join(
                Environment.NewLine,
                emit.Diagnostics.Where(
                    diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType(
            "Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType
            .GetProperty("Manifest")!
            .GetValue(null)!;
        Assert.Empty(manifest.Activations);
        var registration = Assert.Single(manifest.OpenGenericRegistrations);

        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(
            registration.ServiceType,
            registration.ImplementationType,
            ServiceLifetime.Transient));
        using var provider = services.BuildServiceProvider(
            new netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceProviderOptions
            {
                ValidateOnBuild = true,
            });

        Assert.NotNull(provider);
    }

    [Fact]
    public void PackageGeneratorSharesAnOpenGenericActivationAcrossRegistrationLifetimes()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            namespace SharedOpenGenericFixture;
            public sealed class Order { }
            public interface IRepository<T> { }
            internal sealed class Repository<T> : IRepository<T> where T : class, new() { }
            public static class Composition
            {
                public static object Configure(IServiceCollection services, IServiceProvider provider)
                {
                    services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                    return provider.GetRequiredService<IRepository<Order>>();
                }
            }
            """;
        var compilation = CreateCompilationNamed("SharedOpenGenericFixture", source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var run = driver.GetRunResult();

        Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(run.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using var assemblyStream = new MemoryStream();
        var emit = generatedCompilation.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        Assert.Single(manifest.Activations);
    }

    [Fact]
    public void PackageGeneratorClosesAReferencedInternalOpenGenericImplementation()
    {
        const string librarySource = """
            using Microsoft.Extensions.DependencyInjection;
            namespace LibraryFixture;
            public sealed class Order { }
            public interface IRepository<T> { string Name { get; } }
            internal sealed class Repository<T> : IRepository<T> where T : class, new()
            {
                public string Name => "cross-package";
            }
            public static class Registration
            {
                public static void Add(IServiceCollection services) =>
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            }
            """;
        var libraryCompilation = CreateCompilationNamed("OpenGenericLibraryFixture", librarySource);
        GeneratorDriver libraryDriver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        libraryDriver = libraryDriver.RunGeneratorsAndUpdateCompilation(
            libraryCompilation,
            out var generatedLibrary,
            out var libraryGeneratorDiagnostics);
        Assert.Empty(libraryGeneratorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedLibrary.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using var libraryStream = new MemoryStream();
        var libraryEmit = generatedLibrary.Emit(libraryStream);
        Assert.True(libraryEmit.Success, string.Join(Environment.NewLine, libraryEmit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        var libraryImage = libraryStream.ToArray();

        const string applicationSource = """
            using System;
            using LibraryFixture;
            using Microsoft.Extensions.DependencyInjection;
            namespace ApplicationFixture;
            public static class Request
            {
                public static object Resolve(IServiceProvider provider) =>
                    provider.GetRequiredService<IRepository<Order>>();
            }
            """;
        var applicationCompilation = CreateCompilationNamed("OpenGenericApplicationFixture", applicationSource)
            .AddReferences(MetadataReference.CreateFromImage(libraryImage));
        GeneratorDriver applicationDriver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        applicationDriver = applicationDriver.RunGeneratorsAndUpdateCompilation(
            applicationCompilation,
            out var generatedApplication,
            out var applicationGeneratorDiagnostics);
        var applicationRun = applicationDriver.GetRunResult();
        var generatedText = string.Join(
            "\n",
            applicationRun.Results.Single().GeneratedSources.Select(sourceResult => sourceResult.SourceText.ToString()));

        Assert.Empty(applicationGeneratorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(applicationRun.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedApplication.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("LibraryFixture.RepositoryGeneratedActivation_", generatedText, StringComparison.Ordinal);
        Assert.Contains("<global::LibraryFixture.Order>.Create()", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("new global::LibraryFixture.Repository", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", generatedText, StringComparison.Ordinal);

        libraryStream.SetLength(0);
        libraryStream.Write(libraryImage, 0, libraryImage.Length);
        libraryStream.Position = 0;
        var libraryAssembly = AssemblyLoadContext.Default.LoadFromStream(libraryStream);
        using var applicationStream = new MemoryStream();
        var applicationEmit = generatedApplication.Emit(applicationStream);
        Assert.True(applicationEmit.Success, string.Join(Environment.NewLine, applicationEmit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        applicationStream.Position = 0;
        var applicationAssembly = AssemblyLoadContext.Default.LoadFromStream(applicationStream);
        var manifestType = applicationAssembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        var activation = Assert.Single(manifest.Activations);
        Assert.False(activation.ImplementationType.IsPublic);

        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(
            libraryAssembly.GetType("LibraryFixture.IRepository`1")!,
            libraryAssembly.GetType("LibraryFixture.Repository`1")!,
            ServiceLifetime.Transient));
        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService(activation.ServiceType);
        Assert.Equal("cross-package", repository.GetType().GetProperty("Name")!.GetValue(repository));
    }

    [Fact]
    public void PackageGeneratorCarriesALibraryRequestToAnApplicationTemplate()
    {
        const string librarySource = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            namespace RequestLibraryFixture;
            public sealed class Order { }
            public interface IRepository<T> { string Name { get; } }
            public static class Request
            {
                public static object Resolve(IServiceProvider provider) =>
                    provider.GetRequiredService<IRepository<Order>>();
            }
            """;
        var libraryCompilation = CreateCompilationNamed("OpenGenericRequestLibraryFixture", librarySource);
        GeneratorDriver libraryDriver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        libraryDriver = libraryDriver.RunGeneratorsAndUpdateCompilation(
            libraryCompilation,
            out var generatedLibrary,
            out var libraryGeneratorDiagnostics);
        var libraryRun = libraryDriver.GetRunResult();
        Assert.Empty(libraryGeneratorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedLibrary.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            "ClosedGenericRequestAttribute",
            libraryRun.Results.Single().GeneratedSources.Single().SourceText.ToString(),
            StringComparison.Ordinal);

        using var libraryStream = new MemoryStream();
        var libraryEmit = generatedLibrary.Emit(libraryStream);
        Assert.True(libraryEmit.Success, string.Join(Environment.NewLine, libraryEmit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        var libraryImage = libraryStream.ToArray();

        const string applicationSource = """
            using Microsoft.Extensions.DependencyInjection;
            using RequestLibraryFixture;
            namespace RequestApplicationFixture;
            internal sealed class Repository<T> : IRepository<T> where T : class, new()
            {
                public string Name => "propagated-request";
            }
            public static class Registration
            {
                public static void Add(IServiceCollection services) =>
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            }
            """;
        var applicationCompilation = CreateCompilationNamed("OpenGenericRequestApplicationFixture", applicationSource)
            .AddReferences(MetadataReference.CreateFromImage(libraryImage));
        GeneratorDriver applicationDriver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        applicationDriver = applicationDriver.RunGeneratorsAndUpdateCompilation(
            applicationCompilation,
            out var generatedApplication,
            out var applicationGeneratorDiagnostics);
        var applicationRun = applicationDriver.GetRunResult();
        var generatedText = string.Join(
            "\n",
            applicationRun.Results.Single().GeneratedSources.Select(sourceResult => sourceResult.SourceText.ToString()));

        Assert.Empty(applicationGeneratorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(applicationRun.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedApplication.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("<global::RequestLibraryFixture.Order>.Create()", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", generatedText, StringComparison.Ordinal);

        libraryStream.SetLength(0);
        libraryStream.Write(libraryImage, 0, libraryImage.Length);
        libraryStream.Position = 0;
        _ = AssemblyLoadContext.Default.LoadFromStream(libraryStream);
        using var applicationStream = new MemoryStream();
        var applicationEmit = generatedApplication.Emit(applicationStream);
        Assert.True(applicationEmit.Success, string.Join(Environment.NewLine, applicationEmit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        applicationStream.Position = 0;
        var applicationAssembly = AssemblyLoadContext.Default.LoadFromStream(applicationStream);
        var manifestType = applicationAssembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        var activation = Assert.Single(manifest.Activations);

        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(
            activation.TemplateServiceType!,
            activation.TemplateImplementationType!,
            ServiceLifetime.Transient));
        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService(activation.ServiceType);
        Assert.Equal("propagated-request", repository.GetType().GetProperty("Name")!.GetValue(repository));
    }

    [Fact]
    public void PackageGeneratorRecursivelyClosesGenericDependenciesAndSequences()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using Microsoft.Extensions.DependencyInjection;
            namespace RecursiveGenericFixture;
            public sealed class Order { }
            public interface IAudit<T> { }
            internal sealed class Audit<T> : IAudit<T> where T : class, new() { }
            public interface IRepository<T> { int AuditCount { get; } }
            internal sealed class Repository<T> : IRepository<T> where T : class, new()
            {
                public Repository(IAudit<T> audit, IEnumerable<IAudit<T>> audits)
                {
                    AuditCount = audit is null ? -1 : new List<IAudit<T>>(audits).Count;
                }
                public int AuditCount { get; }
            }
            public static class Composition
            {
                public static object Configure(IServiceCollection services, IServiceProvider provider)
                {
                    services.AddTransient(typeof(IAudit<>), typeof(Audit<>));
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                    return provider.GetRequiredService<IRepository<Order>>();
                }
            }
            """;
        var compilation = CreateCompilationNamed("RecursiveOpenGenericFixture", source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var run = driver.GetRunResult();

        Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(run.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using var assemblyStream = new MemoryStream();
        var emit = generatedCompilation.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        Assert.Equal(2, manifest.Activations.Count);
        Assert.Single(manifest.Sequences);

        IServiceCollection services = new ServiceCollection();
        foreach (var activation in manifest.Activations)
        {
            services.Add(new ServiceDescriptor(
                activation.TemplateServiceType!,
                activation.TemplateImplementationType!,
                ServiceLifetime.Transient));
        }

        using var provider = services.BuildServiceProvider();
        var repositoryActivation = manifest.Activations.Single(candidate =>
            candidate.ServiceType.ToString()!.Contains("IRepository", StringComparison.Ordinal));
        var repository = provider.GetRequiredService(repositoryActivation.ServiceType);
        Assert.Equal(1, repository.GetType().GetProperty("AuditCount")!.GetValue(repository));
    }

    [Fact]
    public void PackageGeneratorClosesExactAndAnyKeyOpenGenericRegistrations()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            namespace KeyedOpenGenericFixture;
            public sealed class Order { }
            public interface IRepository<T> { string Name { get; } }
            internal sealed class BlueRepository<T> : IRepository<T> where T : class, new()
            {
                public string Name => "blue";
            }
            internal sealed class AnyRepository<T> : IRepository<T> where T : class, new()
            {
                public string Name => "any";
            }
            public static class Composition
            {
                public static void Configure(IServiceCollection services, IServiceProvider provider)
                {
                    services.AddKeyedTransient(typeof(IRepository<>), "blue", typeof(BlueRepository<>));
                    services.AddKeyedTransient(typeof(IRepository<>), KeyedService.AnyKey, typeof(AnyRepository<>));
                    _ = provider.GetRequiredKeyedService<IRepository<Order>>("blue");
                    _ = provider.GetRequiredKeyedService<IRepository<Order>>("red");
                }
            }
            """;
        var compilation = CreateCompilationNamed("KeyedOpenGenericFixture", source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var generatedCompilation,
            out var generatorDiagnostics);
        var run = driver.GetRunResult();

        Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(run.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.DoesNotContain(generatedCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using var assemblyStream = new MemoryStream();
        var emit = generatedCompilation.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        Assert.Equal(2, manifest.Activations.Count);

        IServiceCollection services = new ServiceCollection();
        services.Add(new ServiceDescriptor(
            assembly.GetType("KeyedOpenGenericFixture.IRepository`1")!,
            "blue",
            assembly.GetType("KeyedOpenGenericFixture.BlueRepository`1")!,
            ServiceLifetime.Transient));
        services.Add(new ServiceDescriptor(
            assembly.GetType("KeyedOpenGenericFixture.IRepository`1")!,
            KeyedService.AnyKey,
            assembly.GetType("KeyedOpenGenericFixture.AnyRepository`1")!,
            ServiceLifetime.Transient));
        using var provider = services.BuildServiceProvider();
        var serviceType = manifest.Activations[0].ServiceType;
        var blue = provider.GetRequiredKeyedService(serviceType, "blue");
        var red = provider.GetRequiredKeyedService(serviceType, "red");
        Assert.Equal("blue", blue.GetType().GetProperty("Name")!.GetValue(blue));
        Assert.Equal("any", red.GetType().GetProperty("Name")!.GetValue(red));
    }

    [Fact]
    public void PackageGeneratorRejectsLateBoundServiceTypesAndKeys()
    {
        const string lateTypeSource = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            namespace LateTypeFixture;
            public static class Request
            {
                public static object Resolve(IServiceProvider provider, Type serviceType) =>
                    provider.GetRequiredService(serviceType);
            }
            """;
        var lateTypeCompilation = CreateCompilationNamed("LateTypeFixture", lateTypeSource);
        GeneratorDriver lateTypeDriver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        lateTypeDriver = lateTypeDriver.RunGenerators(lateTypeCompilation);
        Assert.Contains(lateTypeDriver.GetRunResult().Diagnostics, diagnostic => diagnostic.Id == "NWDI010");

        const string lateKeySource = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            namespace LateKeyFixture;
            public sealed class Order { }
            public interface IRepository<T> { }
            public static class Request
            {
                public static object Resolve(IServiceProvider provider, object serviceKey) =>
                    provider.GetRequiredKeyedService<IRepository<Order>>(serviceKey);
            }
            """;
        var lateKeyCompilation = CreateCompilationNamed("LateKeyFixture", lateKeySource);
        GeneratorDriver lateKeyDriver = CSharpGeneratorDriver.Create(
            generators: [new NetWasmDependencyInjectionGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
        lateKeyDriver = lateKeyDriver.RunGenerators(lateKeyCompilation);
        Assert.Contains(lateKeyDriver.GetRunResult().Diagnostics, diagnostic => diagnostic.Id == "NWDI007");
    }

    [Fact]
    public void ActivationIdentityAndTypeNameDistinguishEveryServiceKey()
    {
        const string source = """
            namespace Fixture;
            public interface IClock { }
            public sealed class Clock : IClock { }
            """;
        var compilation = CreateCompilation(source);
        var serviceType = compilation.GetTypeByMetadataName("Fixture.IClock")!;
        var implementationType = compilation.GetTypeByMetadataName("Fixture.Clock")!;
        var adapter = new RegistrationSyntaxAdapter();
        var builder = GeneratorComposition.CreateActivationModelBuilder();
        var models = new[]
        {
            builder.Build(adapter.Read(new RegistrationSyntaxInput(
                serviceType,
                implementationType,
                null,
                "Singleton"))),
            builder.Build(adapter.Read(new RegistrationSyntaxInput(
                serviceType,
                implementationType,
                null,
                "Singleton",
                "\"blue\""))),
            builder.Build(adapter.Read(new RegistrationSyntaxInput(
                serviceType,
                implementationType,
                null,
                "Singleton",
                "\"a-b\""))),
            builder.Build(adapter.Read(new RegistrationSyntaxInput(
                serviceType,
                implementationType,
                null,
                "Singleton",
                "\"a_b\""))),
        };

        Assert.Equal(models.Length, models.Select(model => model.Facts.Identity).Distinct().Count());
        Assert.Equal(models.Length, models.Select(model => model.Facts.TypeName).Distinct().Count());
        Assert.Contains("[unkeyed]", models[0].Facts.Identity, StringComparison.Ordinal);
        Assert.Contains("[key:\"blue\"]", models[1].Facts.Identity, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionGeneratorRootsSyntaxModelCallSiteSourceAndManifestStages()
    {
        const string source = "namespace Fixture; public sealed class Product { public Product(string value = \"default\") { Value = value; } public string Value { get; } }";
        var compilation = CreateCompilation(source);
        var output = new GeneratedActivationSourceGenerator().Generate(
            compilation,
            new[] { new GeneratedActivationRegistration("Fixture.Product", "Fixture.Product", "product", "Transient") },
            new[] { new GeneratedActivationSequence("global::System.Collections.Generic.IEnumerable<global::Fixture.Product>", "global::Fixture.Product") });

        Assert.Single(output.ActivationSources);
        Assert.Contains("new global::Fixture.Product(", output.ActivationSources[0], StringComparison.Ordinal);
        Assert.Contains("GeneratedActivationManifest", output.ManifestSource, StringComparison.Ordinal);
        Assert.Contains("GeneratedSequenceDescriptor", output.ManifestSource, StringComparison.Ordinal);

        var generated = CreateCompilationNamed("ProductionPipelineFixture", source, output.ActivationSources[0], output.ManifestSource);
        using var assemblyStream = new MemoryStream();
        var emitResult = generated.Emit(assemblyStream);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        Assert.Single(manifest.Activations);
        Assert.Single(manifest.Sequences);
    }

    [Fact]
    public void ProductionGenerationPipelineRejectsNullCollaboratorsAndInputs()
    {
        var syntaxReader = new RegistrationSyntaxAdapter();
        var modelBuilder = GeneratorComposition.CreateActivationModelBuilder();
        var sourceEmitter = GeneratorComposition.CreateActivationSourceEmitter();
        var manifestEmitter = GeneratorComposition.CreateActivationManifestEmitter();
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourcePipeline(null!, modelBuilder, sourceEmitter, manifestEmitter));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourcePipeline(syntaxReader, null!, sourceEmitter, manifestEmitter));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourcePipeline(syntaxReader, modelBuilder, null!, manifestEmitter));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourcePipeline(syntaxReader, modelBuilder, sourceEmitter, null!));

        var pipeline = GeneratorComposition.CreateActivationSourcePipeline();
        var emptyOutput = pipeline.Generate(Array.Empty<RegistrationSyntaxInput>());
        Assert.Throws<ArgumentNullException>(() => pipeline.Generate(null!));
        Assert.Throws<ArgumentException>(() => pipeline.Generate(new RegistrationSyntaxInput[] { null! }));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationSourceOutput(
            Array.Empty<ActivationModel>(), new List<string> { "orphan" }, "manifest"));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourceOutput(null!, Array.Empty<string>(), "manifest"));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourceOutput(Array.Empty<ActivationModel>(), null!, "manifest"));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourceOutput(Array.Empty<ActivationModel>(), Array.Empty<string>(), null!));
    }

    [Fact]
    public void ProductionGeneratorUsesInjectedPipelineAndValidatesHostInputs()
    {
        const string source = "namespace Fixture; public sealed class Product { public Product() { } }";
        var compilation = CreateCompilation(source);
        var pipeline = new RecordingSourcePipeline(new GeneratedActivationSourceOutput(
            Array.Empty<ActivationModel>(),
            Array.Empty<string>(),
            "manifest"));
        var generator = new GeneratedActivationSourceGenerator(pipeline);

        IGeneratedActivationSourcePipeline pipelineContract = pipeline;
        var directOutput = pipelineContract.Generate(Array.Empty<RegistrationSyntaxInput>(), Array.Empty<GeneratedSequenceModel>());
        Assert.Equal("manifest", directOutput.ManifestSource);
        Assert.Equal(1, pipeline.Calls);
        var result = generator.Generate(compilation, Array.Empty<GeneratedActivationRegistration>());
        Assert.Equal("manifest", result.ManifestSource);
        Assert.Equal(2, pipeline.Calls);

        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationSourceGenerator(null!));
        Assert.Throws<ArgumentNullException>(() => generator.Generate(null!, Array.Empty<GeneratedActivationRegistration>()));
        Assert.Throws<ArgumentNullException>(() => generator.Generate(compilation, null!));
        Assert.Throws<ArgumentException>(() => generator.Generate(compilation, new GeneratedActivationRegistration[] { null! }));
        Assert.Throws<ArgumentException>(() => generator.Generate(
            compilation,
            Array.Empty<GeneratedActivationRegistration>(),
            new GeneratedActivationSequence[] { null! }));
        Assert.Throws<GeneratorDiagnosticException>(() => generator.Generate(
            compilation,
            new[] { new GeneratedActivationRegistration("Fixture.Missing", "Fixture.Product", null, "Transient") }));
        Assert.Throws<GeneratorDiagnosticException>(() => generator.Generate(
            compilation,
            new[] { new GeneratedActivationRegistration("Fixture.Product", "Fixture.Missing", null, "Transient") }));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationRegistration(string.Empty, "Fixture.Product", null, "Transient"));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationRegistration("Fixture.Product", string.Empty, null, "Transient"));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationRegistration("Fixture.Product", "Fixture.Product", null, string.Empty));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationSequence(string.Empty, "global::Fixture.Product"));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationSequence("global::Fixture.Product", string.Empty));
    }

    [Fact]
    public void RoslynAdapterReadsConstructorSymbolsAndGeneratedSourceExecutes()
    {
        const string userSource = """
            namespace Fixture;
            public interface IClock { }
            public sealed class Clock : IClock { }
            public sealed class Consumer
            {
                public Consumer(IClock clock, string zone = "UTC") { Clock = clock; Zone = zone; }
                public Consumer(IClock clock) { Clock = clock; Zone = "UTC"; }
                public IClock Clock { get; }
                public string Zone { get; }
            }
            public enum Mode { Low, High }
            public sealed class AllLiteralDefaults
            {
                public AllLiteralDefaults(
                    sbyte tiny = -128,
                    byte small = 255,
                    short shortValue = -32768,
                    ushort ushortValue = 65535,
                    int count = 4,
                    uint unsignedCount = 4294967295U,
                    long big = 9223372036854775807L,
                    ulong unsignedBig = 18446744073709551615UL,
                    char letter = 'x',
                    bool enabled = true,
                    float single = 1.5F,
                    double number = 2.5,
                    decimal money = 3.5M,
                    Mode mode = Mode.High,
                    string text = "literal",
                    object? value = null)
                {
                    Tiny = tiny; Small = small; ShortValue = shortValue; UShortValue = ushortValue;
                    Count = count; UnsignedCount = unsignedCount; Big = big; UnsignedBig = unsignedBig;
                    Letter = letter; Enabled = enabled; Single = single; Number = number; Money = money;
                    Mode = mode; Text = text; Value = value;
                }
                public sbyte Tiny { get; }
                public byte Small { get; }
                public short ShortValue { get; }
                public ushort UShortValue { get; }
                public int Count { get; }
                public uint UnsignedCount { get; }
                public long Big { get; }
                public ulong UnsignedBig { get; }
                public char Letter { get; }
                public bool Enabled { get; }
                public float Single { get; }
                public double Number { get; }
                public decimal Money { get; }
                public Mode Mode { get; }
                public string Text { get; }
                public object? Value { get; }
            }
            public class FixtureBase { public FixtureBase() { } }
            public sealed class FixtureDerived : FixtureBase { public FixtureDerived() { } }
            public sealed class SpecialLiteralDefaults
            {
                public SpecialLiteralDefaults(float single, double number) { Single = single; Number = number; }
                public float Single { get; }
                public double Number { get; }
            }
            """;

        var inputCompilation = CreateCompilation(userSource);
        var serviceType = inputCompilation.GetTypeByMetadataName("Fixture.IClock")!;
        var clockType = inputCompilation.GetTypeByMetadataName("Fixture.Clock")!;
        var consumerType = inputCompilation.GetTypeByMetadataName("Fixture.Consumer")!;
        var adapter = new RegistrationSyntaxAdapter();
        var clockModel = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(serviceType, clockType, null, "Singleton")));
        var consumerModel = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(consumerType, consumerType, null, "Scoped")));
        var clockFacts = clockModel.Facts;
        var consumerFacts = consumerModel.Facts;
        var literalType = inputCompilation.GetTypeByMetadataName("Fixture.AllLiteralDefaults")!;
        var literalModel = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(literalType, literalType, null, "Transient")));
        var literalFacts = literalModel.Facts;
        _ = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(
            inputCompilation.GetTypeByMetadataName("Fixture.FixtureBase")!,
            inputCompilation.GetTypeByMetadataName("Fixture.FixtureDerived")!,
            null,
            "Transient")));
        var specialFacts = new RegistrationFacts(
            "special-literals",
            "Fixture",
            "SpecialLiteralDefaultsGeneratedActivation_special_literals",
            "global::Fixture.SpecialLiteralDefaults",
            "global::Fixture.SpecialLiteralDefaults",
            "Transient",
            new[]
            {
                new GeneratedParameterModel("global::System.Single", true, "float.NaN"),
                new GeneratedParameterModel("global::System.Double", true, "double.PositiveInfinity"),
            });
        var specialModel = new ActivationModel(specialFacts);
        var clockSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(clockModel);
        var consumerSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(consumerModel);
        var literalSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(literalModel);
        var specialSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(specialModel);
        var manifestSource = GeneratorComposition.CreateActivationManifestEmitter().Emit(new[] { clockModel, consumerModel, literalModel });

        Assert.Contains("new global::Fixture.Clock(", clockSource, StringComparison.Ordinal);
        Assert.Contains("new global::Fixture.Consumer(", consumerSource, StringComparison.Ordinal);
        Assert.Contains("GeneratedActivationDescriptor[]", consumerSource, StringComparison.Ordinal);
        Assert.Contains("hasDefaultValue: true", consumerSource, StringComparison.Ordinal);
        Assert.Contains("public static GeneratedActivationManifest Manifest", manifestSource, StringComparison.Ordinal);

        var outputCompilation = CreateCompilation(userSource, clockSource, consumerSource, literalSource, specialSource, manifestSource);
        using var assemblyStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(assemblyStream);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var clockDescriptor = CreateDescriptor(assembly, clockFacts);
        var consumerDescriptor = CreateDescriptor(assembly, consumerFacts);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        Assert.Equal(3, manifest.Activations.Count);

        var services = new ServiceCollection();
        services.AddGeneratedSingleton(clockDescriptor);
        services.AddGeneratedScoped(consumerDescriptor);
        var literalDescriptor = CreateDescriptor(assembly, literalFacts);
        var specialDescriptor = CreateDescriptor(assembly, specialFacts);
        services.AddGeneratedTransient(literalDescriptor);
        services.AddGeneratedTransient(specialDescriptor);
        using var provider = services.BuildServiceProvider();
        var consumer = provider.GetRequiredService(consumerDescriptor.ServiceType);
        Assert.Same(provider.GetRequiredService(clockDescriptor.ServiceType), consumer.GetType().GetProperty("Clock")!.GetValue(consumer));
        Assert.Equal("UTC", consumer.GetType().GetProperty("Zone")!.GetValue(consumer));
        var direct = netwasmTarget::Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(
            provider,
            consumerDescriptor.ImplementationType,
            "local");
        Assert.Equal("local", direct.GetType().GetProperty("Zone")!.GetValue(direct));
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstScoped = firstScope.ServiceProvider.GetRequiredService(consumerDescriptor.ServiceType);
        var firstScopedAgain = firstScope.ServiceProvider.GetRequiredService(consumerDescriptor.ServiceType);
        var secondScoped = secondScope.ServiceProvider.GetRequiredService(consumerDescriptor.ServiceType);
        Assert.Same(firstScoped, firstScopedAgain);
        Assert.NotSame(firstScoped, secondScoped);

        var literal = provider.GetRequiredService(literalDescriptor.ServiceType);
        Assert.Equal((sbyte)-128, literal.GetType().GetProperty("Tiny")!.GetValue(literal));
        Assert.Equal((byte)255, literal.GetType().GetProperty("Small")!.GetValue(literal));
        Assert.Equal((short)-32768, literal.GetType().GetProperty("ShortValue")!.GetValue(literal));
        Assert.Equal((ushort)65535, literal.GetType().GetProperty("UShortValue")!.GetValue(literal));
        Assert.Equal(uint.MaxValue, literal.GetType().GetProperty("UnsignedCount")!.GetValue(literal));
        Assert.Equal(long.MaxValue, literal.GetType().GetProperty("Big")!.GetValue(literal));
        Assert.Equal(ulong.MaxValue, literal.GetType().GetProperty("UnsignedBig")!.GetValue(literal));
        Assert.Equal('x', literal.GetType().GetProperty("Letter")!.GetValue(literal));
        Assert.True((bool)literal.GetType().GetProperty("Enabled")!.GetValue(literal)!);
        Assert.Equal(3.5M, literal.GetType().GetProperty("Money")!.GetValue(literal));
        Assert.Equal("High", literal.GetType().GetProperty("Mode")!.GetValue(literal)!.ToString());
        Assert.Equal("literal", literal.GetType().GetProperty("Text")!.GetValue(literal));
        Assert.Null(literal.GetType().GetProperty("Value")!.GetValue(literal));
        var special = provider.GetRequiredService(specialDescriptor.ServiceType);
        Assert.True(float.IsNaN((float)special.GetType().GetProperty("Single")!.GetValue(special)!));
        Assert.True(double.IsPositiveInfinity((double)special.GetType().GetProperty("Number")!.GetValue(special)!));
    }

    [Fact]
    public void RoslynAdapterFormatsSupportedKeyConstantsAndInheritedKeys()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            namespace Fixture;
            public enum Mode { High = 7 }
            public sealed class KeyedConstants
            {
                public KeyedConstants(
                    [FromKeyedServices(null)] object nil,
                    [FromKeyedServices("text")] object text,
                    [FromKeyedServices('x')] object character,
                    [FromKeyedServices(true)] object truth,
                    [FromKeyedServices(false)] object falseTruth,
                    [FromKeyedServices((sbyte)-1)] object signedByte,
                    [FromKeyedServices((byte)255)] object unsignedByte,
                    [FromKeyedServices((short)-2)] object signedShort,
                    [FromKeyedServices((ushort)65535)] object unsignedShort,
                    [FromKeyedServices(-3)] object signedInt,
                    [FromKeyedServices(4294967295U)] object unsignedInt,
                    [FromKeyedServices(-4L)] object signedLong,
                    [FromKeyedServices(18446744073709551615UL)] object unsignedLong,
                    [FromKeyedServices(Mode.High)] object mode,
                    [FromKeyedServices] object inherited,
                    [Other] object notKeyed) { }
            }
            [AttributeUsage(AttributeTargets.Parameter)]
            public sealed class FromKeyedServices : Attribute
            {
                public FromKeyedServices(object key) { }
            }
            [AttributeUsage(AttributeTargets.Parameter)]
            public sealed class OtherAttribute : Attribute { }
            public sealed class AliasKeyed
            {
                public AliasKeyed([FromKeyedServices("alias")] object value) { }
            }
            public sealed class UnsupportedKey
            {
                public UnsupportedKey([Microsoft.Extensions.DependencyInjection.FromKeyedServices(typeof(UnsupportedKey))] object value) { }
            }
            """;

        var compilation = CreateCompilation(source);
        var type = compilation.GetTypeByMetadataName("Fixture.KeyedConstants")!;
        var model = GeneratorComposition.CreateActivationModelBuilder().Build(
            new RegistrationSyntaxAdapter().Read(new RegistrationSyntaxInput(type, type, null, "Transient")));
        var generated = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(model);

        Assert.Contains("serviceKey: null", generated, StringComparison.Ordinal);
        Assert.Contains("serviceKey: \"text\"", generated, StringComparison.Ordinal);
        Assert.Contains("serviceKey: (sbyte)-1", generated, StringComparison.Ordinal);
        Assert.Contains("serviceKey: (byte)255", generated, StringComparison.Ordinal);
        Assert.Contains("serviceKey: 4294967295U", generated, StringComparison.Ordinal);
        Assert.Contains("serviceKey: 18446744073709551615UL", generated, StringComparison.Ordinal);
        Assert.Contains("ServiceKeyLookupMode.InheritKey", generated, StringComparison.Ordinal);

        var aliasType = compilation.GetTypeByMetadataName("Fixture.AliasKeyed")!;
        var aliasModel = GeneratorComposition.CreateActivationModelBuilder().Build(
            new RegistrationSyntaxAdapter().Read(new RegistrationSyntaxInput(aliasType, aliasType, null, "Transient")));
        Assert.Contains("serviceKey: \"alias\"", new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(aliasModel), StringComparison.Ordinal);

        var unsupportedType = compilation.GetTypeByMetadataName("Fixture.UnsupportedKey")!;
        Assert.Throws<GeneratorDiagnosticException>(() => GeneratorComposition.CreateActivationModelBuilder().Build(
            new RegistrationSyntaxAdapter().Read(new RegistrationSyntaxInput(unsupportedType, unsupportedType, null, "Transient"))));

        var keyedRegistrationModel = GeneratorComposition.CreateActivationModelBuilder().Build(
            new RegistrationSyntaxAdapter().Read(new RegistrationSyntaxInput(type, type, null, "Transient", "\"outer\"")));
        Assert.Contains("serviceKey: \"outer\"", new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(keyedRegistrationModel), StringComparison.Ordinal);
    }

    [Fact]
    public void RoslynAdapterSelectsMarkedConstructorAndRejectsUnsupportedShapes()
    {
        const string source = """
            using System;
            namespace Fixture;
            [AttributeUsage(AttributeTargets.Constructor)]
            public sealed class ActivatorUtilitiesConstructorAttribute : Attribute { }
            [AttributeUsage(AttributeTargets.Constructor)]
            public sealed class ActivatorUtilitiesConstructor : Attribute { }
            [AttributeUsage(AttributeTargets.Constructor)]
            public sealed class OtherAttribute : Attribute { }
            public interface IContract { }
            public sealed class Marked
            {
                public Marked() { }
                [ActivatorUtilitiesConstructor]
                public Marked(IContract value) { }
            }
            public sealed class Ambiguous
            {
                public Ambiguous(IContract value) { }
                public Ambiguous(string other) { }
            }
            public sealed class MultipleMarked
            {
                [ActivatorUtilitiesConstructor]
                public MultipleMarked() { }
                [ActivatorUtilitiesConstructor]
                public MultipleMarked(IContract value) { }
            }
            public sealed class AlternativeMarked
            {
                [ActivatorUtilitiesConstructor]
                public AlternativeMarked() { }
            }
            public sealed class WithOtherAttribute
            {
                [Other]
                public WithOtherAttribute() { }
            }
            public sealed class LiteralDefaults
            {
                public LiteralDefaults(string text = "x", char letter = 'x', bool enabled = false, float single = 1.5F, double number = 2.5, decimal money = 3.5M, int count = 4, object value = null) { }
            }
            public sealed class LiteralTrue
            {
                public LiteralTrue(bool enabled = true) { }
            }
            public sealed class Hidden
            {
                private Hidden() { }
            }
            public abstract class Abstract { public Abstract() { } }
            """;
        var compilation = CreateCompilation(source, "public sealed class GlobalType { public GlobalType() { } }");
        var service = compilation.GetTypeByMetadataName("Fixture.IContract")!;
        var adapter = new RegistrationSyntaxAdapter();

        var selectedSyntax = adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.Marked")!, "marked", "Transient"));
        var selectedPolicy = new ConstructorSelector().Select(selectedSyntax);
        Assert.True(selectedPolicy.IsPreferred);
        Assert.Equal(2, selectedPolicy.Ordered.Count);
        var selected = GeneratorComposition.CreateActivationModelBuilder().Build(selectedSyntax).Facts;
        Assert.Single(selected.Parameters);
        Assert.Equal("marked", selected.Identity);

        var alternative = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.AlternativeMarked")!, null, "Transient"))).Facts;
        Assert.Empty(alternative.Parameters);
        _ = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.WithOtherAttribute")!, null, "Transient")));
        var literals = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.LiteralDefaults")!, null, "Transient"))).Facts;
        Assert.Equal(8, literals.Parameters.Count);
        _ = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.LiteralTrue")!, null, "Transient")));

        var globalType = compilation.GetTypeByMetadataName("GlobalType");
        Assert.NotNull(globalType);
        _ = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(service, globalType!, null, "Transient")));

        var formatter = new OptionalDefaultExpressionFormatter();
        var unsupported = Assert.Throws<GeneratorDiagnosticException>(() => formatter.Format(compilation.GetSpecialType(SpecialType.System_DateTime), new object()));
        Assert.Equal("NWDI005", unsupported.Code);
        Assert.Equal("float.NaN", OptionalDefaultExpressionFormatter.FormatSingle(float.NaN));
        Assert.Equal("float.PositiveInfinity", OptionalDefaultExpressionFormatter.FormatSingle(float.PositiveInfinity));
        Assert.Equal("float.NegativeInfinity", OptionalDefaultExpressionFormatter.FormatSingle(float.NegativeInfinity));
        Assert.Equal("double.NaN", OptionalDefaultExpressionFormatter.FormatDouble(double.NaN));
        Assert.Equal("double.PositiveInfinity", OptionalDefaultExpressionFormatter.FormatDouble(double.PositiveInfinity));
        Assert.Equal("double.NegativeInfinity", OptionalDefaultExpressionFormatter.FormatDouble(double.NegativeInfinity));

        var ambiguous = GeneratorComposition.CreateActivationModelBuilder().Build(adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.Ambiguous")!, null, "Transient"))).Facts;
        Assert.Equal(2, ambiguous.Constructors.Count);
        var multipleMarkedSyntax = adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.MultipleMarked")!, null, "Transient"));
        AssertDiagnostic("NWDI003", () => new ConstructorSelector().Select(multipleMarkedSyntax));
        AssertDiagnostic("NWDI002", () => adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.Hidden")!, null, "Transient")));
        AssertDiagnostic("NWDI001", () => adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.Abstract")!, null, "Transient")));
        AssertDiagnostic("NWDI006", () => adapter.Read(new RegistrationSyntaxInput(service, compilation.GetTypeByMetadataName("Fixture.Marked")!, null, "Invalid")));
    }

    [Fact]
    public void PlainModelsEmitDeterministicSourceAndManifest()
    {
        var model = Model("alpha", "Transient", new GeneratedParameterModel("global::System.Int32", false, null));
        var empty = Model("empty", "Singleton");
        var multiFacts = new RegistrationFacts(
            "multi",
            "Fixture",
            "GeneratedProductActivation_multi",
            "global::Fixture.IProduct",
            "global::Fixture.Product",
            "Transient",
            Array.Empty<GeneratedParameterModel>(),
            new[]
            {
                new GeneratedConstructorModel("multi-primary", Array.Empty<GeneratedParameterModel>(), false),
                new GeneratedConstructorModel("multi-second", Array.Empty<GeneratedParameterModel>(), false),
                new GeneratedConstructorModel("multi-third", Array.Empty<GeneratedParameterModel>(), false),
            });
        var multi = new ActivationModel(multiFacts);
        var source = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(model);
        var multiSource = new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(multi);
        var manifest = GeneratorComposition.CreateActivationManifestEmitter().Emit(new[] { model, empty });
        var sequence = new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.Product>",
            "global::Fixture.Product",
            "\"blue\"");
        var sequenceSource = new GeneratedSequenceSourceEmitter().Emit(sequence);
        var unkeyedSequenceSource = new GeneratedSequenceSourceEmitter().Emit(new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.Product>",
            "global::Fixture.Product"));
        var manifestWithSequence = GeneratorComposition.CreateActivationManifestEmitter().Emit(new[] { model }, new[] { sequence });
        var manifestWithMultipleSequences = GeneratorComposition.CreateActivationManifestEmitter().Emit(
            new[] { model },
            new[] { sequence, new GeneratedSequenceModel(
                "global::System.Collections.Generic.IEnumerable<global::Fixture.Product>",
                "global::Fixture.Product") });

        Assert.Contains("new global::Fixture.Product(", source, StringComparison.Ordinal);
        Assert.Contains('\n', source);
        Assert.DoesNotContain('\r', source);
        Assert.DoesNotContain('\r', multiSource);
        Assert.Contains("ServiceLifetime.Transient", source, StringComparison.Ordinal);
        Assert.Contains("GeneratedProductActivation", source, StringComparison.Ordinal);
        Assert.Contains("multi-second", multiSource, StringComparison.Ordinal);
        Assert.Contains("GeneratedActivationManifest", manifest, StringComparison.Ordinal);
        Assert.Contains('\n', manifest);
        Assert.DoesNotContain('\r', manifest);
        Assert.Contains("GeneratedProductActivation", manifest, StringComparison.Ordinal);
        Assert.Contains("GeneratedSequenceDescriptor", sequenceSource, StringComparison.Ordinal);
        Assert.Contains("serviceKey: \"blue\"", sequenceSource, StringComparison.Ordinal);
        Assert.DoesNotContain("serviceKey:", unkeyedSequenceSource, StringComparison.Ordinal);
        Assert.Contains("var result = new global::Fixture.Product[values.Count]", manifestWithSequence, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Linq", manifestWithSequence, StringComparison.Ordinal);
        Assert.Contains("serviceKey: \"blue\"", manifestWithMultipleSequences, StringComparison.Ordinal);
        var callSite = new GeneratedCallSiteBuilder().Build(model);
        Assert.Contains("GeneratedProductActivation", callSite.Activation.Facts.TypeName, StringComparison.Ordinal);
        Assert.Single(callSite.Parameters);
        Assert.Equal("global::System.Int32", callSite.Parameters[0].TypeDisplay);
        Assert.False(callSite.Parameters[0].HasDefaultValue);
        Assert.Null(callSite.Parameters[0].DefaultExpression);
        Assert.Null(callSite.Parameters[0].ServiceKeyExpression);
        Assert.Equal("NullKey", callSite.Parameters[0].LookupMode);
    }

    [Fact]
    public void RealSequenceEmitterSatisfiesItsTypedContract()
    {
        IGeneratedSequenceSourceEmitter emitter = CreateRealSequenceEmitter();
        var unkeyed = emitter.Emit(new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.Product>",
            "global::Fixture.Product"));
        var keyed = emitter.Emit(new GeneratedSequenceModel(
            "global::System.Collections.Generic.IEnumerable<global::Fixture.Product>",
            "global::Fixture.Product",
            "\"blue\""));

        Assert.Contains("new GeneratedSequenceDescriptor(typeof(", unkeyed, StringComparison.Ordinal);
        Assert.Contains("var result = new global::Fixture.Product[values.Count]", unkeyed, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Linq", unkeyed, StringComparison.Ordinal);
        Assert.Contains("serviceKey: \"blue\"", keyed, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => emitter.Emit(null!));
    }

#pragma warning disable CA1859
    private static IGeneratedSequenceSourceEmitter CreateRealSequenceEmitter() => new GeneratedSequenceSourceEmitter();
#pragma warning restore CA1859

    [Fact]
    public void GeneratedManifestRejectsNullSequenceEntries()
    {
        Assert.Throws<ArgumentException>(() => new GeneratedActivationManifest(
            Array.Empty<GeneratedActivationDescriptor>(),
            new GeneratedSequenceDescriptor[] { null! }));

        var sequence = new GeneratedSequenceDefinition(
            typeof(IEnumerable<RuntimeService>),
            typeof(RuntimeService),
            "duplicate",
            values => values.Cast<RuntimeService>().ToArray());
        Assert.Throws<ArgumentException>(() => new netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookupBuilder()
            .Build(Array.Empty<GeneratedActivationDefinition>(), new[] { sequence, sequence }));
        Assert.Throws<ArgumentException>(() => new netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookupBuilder()
            .Build(Array.Empty<GeneratedActivationDefinition>(), new GeneratedSequenceDefinition[] { null! }));
        var lookupBuilder = new netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookupBuilder();
        _ = lookupBuilder.Build(Array.Empty<GeneratedActivationDefinition>(), new[] { sequence });
        _ = new GeneratedActivationLookup(
            new Dictionary<Type, GeneratedFactoryRegistration>(),
            new Dictionary<Type, HashSet<Type>>());
    }

    [Fact]
    public void LocatorAndCallSiteUseSemanticModels()
    {
        var first = Model("first", "Singleton");
        var second = Model("second", "Scoped");
        var locator = new ActivationDescriptorLocator();
        var activations = new[] { first, second };
        Assert.Same(first, locator.Locate(activations, "first"));
        Assert.Null(locator.Locate(activations, "missing"));
        Assert.Throws<ArgumentNullException>(() => locator.Locate(null!, "first"));
        Assert.Throws<ArgumentNullException>(() => locator.Locate(activations, null!));
        Assert.Throws<ArgumentException>(() => locator.Locate(new ActivationModel[] { null! }, "first"));
        Assert.Throws<GeneratorDiagnosticException>(() => locator.Locate(new[] { first, first }, "first"));
        Assert.Same(first, new GeneratedCallSiteBuilder().Build(first).Activation);

    }

    [Fact]
    public void ActorBoundariesRejectNullModels()
    {
        Assert.Throws<ArgumentNullException>(() => GeneratorComposition.CreateActivationModelBuilder().Build(null!));
        Assert.Throws<ArgumentNullException>(() => new ActivationModelBuilder(null!, new OptionalDefaultExpressionFormatter()));
        Assert.Throws<ArgumentNullException>(() => new ActivationModelBuilder(new ConstructorSelector(), null!));
        Assert.Throws<ArgumentNullException>(() => new ActivationSourceEmitter(null!));
        Assert.Throws<ArgumentNullException>(() => new ActivationSourceEmitter(new GeneratedCallSiteBuilder()).Emit(null!));
        Assert.Throws<ArgumentNullException>(() => new ActivationManifestEmitter(null!, new GeneratedSequenceSourceEmitter()));
        Assert.Throws<ArgumentNullException>(() => new ActivationManifestEmitter(new ActivationDescriptorLocator(), null!));
        var manifestEmitter = GeneratorComposition.CreateActivationManifestEmitter();
        Assert.Throws<ArgumentNullException>(() => manifestEmitter.Emit(null!));
        Assert.Throws<ArgumentNullException>(() => manifestEmitter.Emit(Array.Empty<ActivationModel>(), null!));
        Assert.Throws<GeneratorDiagnosticException>(() => new ActivationManifestEmitter(
            new NullDescriptorLocator(), new GeneratedSequenceSourceEmitter()).Emit(new[] { Model("missing", "Transient") }));
        Assert.Throws<ArgumentNullException>(() => new GeneratedSequenceSourceEmitter().Emit(null!));
        Assert.Throws<ArgumentException>(() => new GeneratedSequenceModel(string.Empty, "global::System.Object"));
        Assert.Throws<ArgumentException>(() => new GeneratedSequenceModel("global::System.Object", string.Empty));
        Assert.Throws<ArgumentNullException>(() => new GeneratedCallSiteBuilder().Build(null!));
    }

    [Fact]
    public void RuntimeGeneratedContractsAreImmutableAndValidateIdentities()
    {
        var parameter = new GeneratedParameter(typeof(string), hasDefaultValue: true, defaultValue: "default");
        var descriptor = new GeneratedActivationDescriptor(
            "runtime",
            typeof(RuntimeService),
            typeof(RuntimeService),
            new[] { parameter },
            values => new RuntimeService((string)values[0]!));
        Assert.Equal("runtime", descriptor.Identity);
        Assert.Same(parameter, descriptor.Parameters[0]);
        Assert.True(parameter.HasDefaultValue);
        Assert.Equal("default", parameter.DefaultValue);

        var alternative = new GeneratedActivationDescriptor(
            "runtime-alt",
            typeof(RuntimeService),
            typeof(RuntimeService),
            Array.Empty<GeneratedParameter>(),
            _ => new RuntimeService("alternative"));
        var preferred = new GeneratedActivationDescriptor(
            "runtime-preferred",
            typeof(RuntimeService),
            typeof(RuntimeService),
            Array.Empty<GeneratedParameter>(),
            _ => new RuntimeService("preferred"),
            new[] { alternative },
            isPreferred: true);
        Assert.True(preferred.IsPreferred);
        Assert.Single(preferred.Alternatives);

        var manifest = new GeneratedActivationManifest(new[] { descriptor });
        Assert.Same(descriptor, manifest.Activations[0]);
        var sequence = new GeneratedSequenceDescriptor(
            typeof(IEnumerable<RuntimeService>),
            typeof(RuntimeService),
            values => values.Cast<RuntimeService>().ToArray());
        Assert.Equal(typeof(IEnumerable<RuntimeService>), sequence.SequenceType);
        Assert.Null(sequence.ServiceKey);
        Assert.Equal(typeof(RuntimeService), sequence.ElementType);
        Assert.Null(new GeneratedSequenceDescriptor(
            typeof(IEnumerable<RuntimeService>),
            values => values.Cast<RuntimeService>().ToArray()).ElementType);
        var sequenceKey = new GeneratedSequenceKey(typeof(IEnumerable<RuntimeService>), "key");
        Assert.True(sequenceKey.Equals(sequenceKey));
        Assert.False(sequenceKey.Equals(new object()));
        Assert.True(sequenceKey.Equals((object)sequenceKey));
        Assert.False(sequenceKey.Equals(new GeneratedSequenceKey(typeof(string), "key")));
        var sequenceManifest = new GeneratedActivationManifest(new[] { descriptor }, new[] { sequence });
        Assert.Single(sequenceManifest.Sequences);
        var openGenericRegistration = new GeneratedOpenGenericRegistrationDescriptor(
            typeof(IEnumerable<>),
            typeof(List<>),
            isKeyedService: true,
            serviceKey: "key");
        var openGenericManifest = new GeneratedActivationManifest(
            new[] { descriptor },
            new[] { sequence },
            new[] { openGenericRegistration });
        Assert.Same(
            openGenericRegistration,
            openGenericManifest.OpenGenericRegistrations[0]);
        Assert.True(openGenericRegistration.IsKeyedService);
        Assert.Equal("key", openGenericRegistration.ServiceKey);
        var services = new ServiceCollection();
        services.AddGeneratedSingleton(descriptor);
        services.AddGeneratedScoped(descriptor);
        services.AddGeneratedTransient(descriptor);

        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService),
            (IReadOnlyList<GeneratedParameter>)(object)new GeneratedParameter?[] { null },
            values => new RuntimeService("x")));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationManifest(
            (IReadOnlyList<GeneratedActivationDescriptor>)(object)new GeneratedActivationDescriptor?[] { null }));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationManifest(
            Array.Empty<GeneratedActivationDescriptor>(),
            Array.Empty<GeneratedSequenceDescriptor>(),
            null!));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationManifest(
            Array.Empty<GeneratedActivationDescriptor>(),
            Array.Empty<GeneratedSequenceDescriptor>(),
            (IReadOnlyList<GeneratedOpenGenericRegistrationDescriptor>)(object)
                new GeneratedOpenGenericRegistrationDescriptor?[] { null }));
        Assert.Throws<ArgumentNullException>(() =>
            new GeneratedOpenGenericRegistrationDescriptor(
                null!,
                typeof(List<>),
                isKeyedService: false,
                serviceKey: null));
        Assert.Throws<ArgumentNullException>(() =>
            new GeneratedOpenGenericRegistrationDescriptor(
                typeof(IEnumerable<>),
                null!,
                isKeyedService: false,
                serviceKey: null));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"), null!));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            (IReadOnlyList<GeneratedActivationDescriptor>)(object)new GeneratedActivationDescriptor?[] { null }));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            new[] { new GeneratedActivationDescriptor("other", typeof(string), typeof(string), Array.Empty<GeneratedParameter>(), _ => "x") }));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            new[] { new GeneratedActivationDescriptor("same-service", typeof(RuntimeService), typeof(string), Array.Empty<GeneratedParameter>(), _ => "x") }));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            Array.Empty<GeneratedActivationDescriptor>(), false,
            (IReadOnlyList<Type>)(object)new Type?[] { null }));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            Array.Empty<GeneratedActivationDescriptor>(), false, Array.Empty<Type>(),
            templateServiceType: typeof(IEnumerable<>)));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            Array.Empty<GeneratedActivationDescriptor>(), false, Array.Empty<Type>(),
            templateImplementationType: typeof(List<>)));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x"),
            Array.Empty<GeneratedActivationDescriptor>(), false, Array.Empty<Type>(),
            templateServiceKey: "orphaned-key"));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDefinition(
            "mismatched-keys",
            typeof(RuntimeService),
            typeof(RuntimeService),
            new[] { typeof(string) },
            Array.Empty<GeneratedActivationDefinition>(),
            isPreferred: false,
            Array.Empty<Type>(),
            parameterKeys: Array.Empty<object?>()));
        Assert.Throws<ArgumentException>(() => new GeneratedActivationDefinition(
            "invalid-lookup-mode",
            typeof(RuntimeService),
            typeof(RuntimeService),
            new[] { typeof(string) },
            Array.Empty<GeneratedActivationDefinition>(),
            isPreferred: false,
            Array.Empty<Type>(),
            parameterLookupModes: new[] { (ServiceKeyLookupMode)99 }));
        var unkeyedDescriptor = new GeneratedServiceDescriptor(
            typeof(RuntimeService),
            typeof(RuntimeService),
            ServiceLifetime.Singleton,
            descriptor);
        Assert.False(unkeyedDescriptor.IsKeyedService);
        Assert.Equal(
            "ServiceType: runtime Lifetime: Singleton ImplementationType: runtime",
            unkeyedDescriptor.ToString());
        var keyedDescriptor = new GeneratedServiceDescriptor(
            typeof(RuntimeService),
            "key",
            typeof(RuntimeService),
            ServiceLifetime.Scoped,
            descriptor);
        Assert.Equal(
            "ServiceType: runtime Lifetime: Scoped ServiceKey: key ImplementationType: runtime",
            keyedDescriptor.ToString());
        Assert.Throws<ArgumentException>(() => new GeneratedServiceDescriptor(
            typeof(string), typeof(RuntimeService), ServiceLifetime.Singleton, descriptor));
        Assert.Throws<ArgumentException>(() => new GeneratedServiceDescriptor(
            typeof(RuntimeService), typeof(string), ServiceLifetime.Singleton, descriptor));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationDescriptor(
            null!, typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x")));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationDescriptor(
            "runtime", null!, typeof(RuntimeService), Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x")));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), null!, Array.Empty<GeneratedParameter>(), _ => new RuntimeService("x")));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), null!, _ => new RuntimeService("x")));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationDescriptor(
            "runtime", typeof(RuntimeService), typeof(RuntimeService), Array.Empty<GeneratedParameter>(), null!));
        Assert.Throws<ArgumentNullException>(() => new GeneratedActivationManifest(null!));
        Assert.Throws<ArgumentNullException>(() => new GeneratedParameter(null!));
        Assert.Throws<ArgumentNullException>(() => new GeneratedServiceDescriptor(
            typeof(RuntimeService), typeof(RuntimeService), ServiceLifetime.Singleton, null!));
    }

    private static ActivationModel Model(string identity, string lifetime, params GeneratedParameterModel[] parameters) =>
        new(new RegistrationFacts(identity, "Fixture", "GeneratedProductActivation_" + identity, "global::Fixture.IProduct", "global::Fixture.Product", lifetime, parameters));

    private static void AssertDiagnostic(string code, Action action)
    {
        var exception = Assert.Throws<GeneratorDiagnosticException>(action);
        Assert.Equal(code, exception.Code);
    }

    private static GeneratedActivationDescriptor CreateDescriptor(Assembly assembly, RegistrationFacts facts)
    {
        var type = assembly.GetType(facts.NamespaceName + "." + facts.TypeName)!;
        return (GeneratedActivationDescriptor)type.GetMethod("Create")!.Invoke(null, null)!;
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
        => CreateCompilationNamed("GeneratedFixture", sources);

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
            sources.Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))),
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

    private sealed class RuntimeService(string value)
    {
        public string Value { get; } = value;
    }

    private interface IRuntimeCatalogService
    {
    }

    private sealed class RuntimeCatalogService : IRuntimeCatalogService
    {
    }

    private sealed class RecordingSourcePipeline(GeneratedActivationSourceOutput output) : IGeneratedActivationSourcePipeline
    {
        public int Calls { get; private set; }

        public GeneratedActivationSourceOutput Generate(
            IReadOnlyList<RegistrationSyntaxInput> registrations,
            IReadOnlyList<GeneratedSequenceModel>? sequences = null,
            IReadOnlyList<OpenGenericRegistrationInput>? openGenericRegistrations = null)
        {
            Calls++;
            return output;
        }
    }

    private sealed class NullDescriptorLocator : IActivationDescriptorLocator
    {
        public ActivationModel? Locate(IReadOnlyList<ActivationModel> activations, string identity) => null;
    }

}
