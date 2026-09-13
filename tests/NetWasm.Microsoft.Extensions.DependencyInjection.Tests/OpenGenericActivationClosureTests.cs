extern alias netwasmTarget;

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using NetWasm.Microsoft.Extensions.DependencyInjection.Generator;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationExtensions;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions;
using GeneratedActivationDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDescriptor;
using GeneratedActivationManifest = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationManifest;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Tests;

public sealed class OpenGenericActivationClosureTests
{
    [Fact]
    public void ApplicationGeneratorClosesReachableOpenTemplateAndRunsDesktopOracle()
    {
        var compilation = CreateCompilation(FixtureSource);
        var generator = new GeneratedActivationSourceGenerator();
        var output = generator.Generate(
            compilation,
            Array.Empty<GeneratedActivationRegistration>(),
            openGenericRegistrations:
            [
                new GeneratedOpenGenericRegistration(
                    "Fixture.IRepository`1",
                    "Fixture.Repository`1",
                    "Transient")
            ],
            closedGenericRequests:
            [
                new GeneratedClosedGenericRequest("Fixture.IRepository`1", ["Fixture.Order"])
            ]);

        Assert.Single(output.ActivationSources);
        Assert.Contains("global::Fixture.IRepository<global::Fixture.Order>", output.ActivationSources[0], StringComparison.Ordinal);
        Assert.Contains("new global::Fixture.Repository<global::Fixture.Order>", output.ActivationSources[0], StringComparison.Ordinal);
        Assert.DoesNotContain("MakeGenericType", output.ActivationSources[0], StringComparison.Ordinal);

        var generated = CreateCompilation(FixtureSource, output.ActivationSources[0], output.ManifestSource);
        using var assemblyStream = new MemoryStream();
        var emit = generated.Emit(assemblyStream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        assemblyStream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        var manifestType = assembly.GetType("Generated.GeneratedActivationManifestSource")!;
        var manifest = (GeneratedActivationManifest)manifestType.GetProperty("Manifest")!.GetValue(null)!;
        var descriptor = Assert.Single(manifest.Activations);
        Assert.Contains("Fixture.IRepository", descriptor.Identity, StringComparison.Ordinal);

        var services = new ServiceCollection();
        services.AddGenerated(descriptor, ServiceLifetime.Transient);
        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService(descriptor.ServiceType);
        Assert.Equal("order", repository.GetType().GetProperty("Name")!.GetValue(repository));
    }

    [Fact]
    public void ApplicationGeneratorOutputIsDeterministicForTheSameClosedRequestSet()
    {
        var compilation = CreateCompilation(FixtureSource);
        var templates = new[]
        {
            new GeneratedOpenGenericRegistration("Fixture.IRepository`1", "Fixture.Repository`1", "Transient")
        };
        var requests = new[]
        {
            new GeneratedClosedGenericRequest("Fixture.IRepository`1", ["Fixture.Order"])
        };

        var first = new GeneratedActivationSourceGenerator().Generate(
            compilation,
            Array.Empty<GeneratedActivationRegistration>(),
            openGenericRegistrations: templates,
            closedGenericRequests: requests);
        var second = new GeneratedActivationSourceGenerator().Generate(
            compilation,
            Array.Empty<GeneratedActivationRegistration>(),
            openGenericRegistrations: templates,
            closedGenericRequests: requests);

        Assert.Equal(first.ManifestSource, second.ManifestSource);
        Assert.Equal(first.ActivationSources, second.ActivationSources);
    }

    [Fact]
    public void ClosureRejectsMissingTemplateAndConstraintViolationsBeforeEmission()
    {
        var compilation = CreateCompilation(FixtureSource);
        var generator = new GeneratedActivationSourceGenerator();
        var missing = Assert.Throws<GeneratorDiagnosticException>(() => generator.Generate(
            compilation,
            Array.Empty<GeneratedActivationRegistration>(),
            closedGenericRequests:
            [
                new GeneratedClosedGenericRequest("Fixture.IRepository`1", ["Fixture.Order"])
            ]));
        Assert.StartsWith("NWDI011:", missing.Message, StringComparison.Ordinal);

        var constrainedCompilation = CreateCompilation(ConstrainedFixtureSource);
        var constraint = Assert.Throws<GeneratorDiagnosticException>(() => generator.Generate(
            constrainedCompilation,
            Array.Empty<GeneratedActivationRegistration>(),
            openGenericRegistrations:
            [
                new GeneratedOpenGenericRegistration("Fixture.IReferenceRepository`1", "Fixture.ReferenceRepository`1", "Transient")
            ],
            closedGenericRequests:
            [
                new GeneratedClosedGenericRequest("Fixture.IReferenceRepository`1", ["System.Int32"])
            ]));
        Assert.StartsWith("NWDI016:", constraint.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClosureValidatorRejectsDuplicateClosedIdentity()
    {
        var compilation = CreateCompilation(FixtureSource);
        var generator = new GeneratedActivationSourceGenerator();
        var duplicate = Assert.Throws<GeneratorDiagnosticException>(() => generator.Generate(
            compilation,
            Array.Empty<GeneratedActivationRegistration>(),
            openGenericRegistrations:
            [
                new GeneratedOpenGenericRegistration("Fixture.IRepository`1", "Fixture.Repository`1", "Transient"),
                new GeneratedOpenGenericRegistration("Fixture.IRepository`1", "Fixture.Repository`1", "Transient")
            ],
            closedGenericRequests:
            [
                new GeneratedClosedGenericRequest("Fixture.IRepository`1", ["Fixture.Order"])
            ]));
        Assert.StartsWith("NWDI017:", duplicate.Message, StringComparison.Ordinal);
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        AddReference(references, typeof(GeneratedActivationDescriptor).Assembly.Location);
        AddReference(references, typeof(IServiceCollection).Assembly.Location);
        return CSharpCompilation.Create(
            "DI01EGeneratedFixture",
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

    private const string FixtureSource = """
        namespace Fixture;
        public sealed class Order { }
        public interface IRepository<T> { string Name { get; } }
        public sealed class Repository<T> : IRepository<T> where T : class, new()
        {
            public string Name => typeof(T).Name.ToLowerInvariant();
        }
        """;

    private const string ConstrainedFixtureSource = """
        namespace Fixture;
        public interface IReferenceRepository<T> { }
        public sealed class ReferenceRepository<T> : IReferenceRepository<T> where T : class { }
        """;
}
