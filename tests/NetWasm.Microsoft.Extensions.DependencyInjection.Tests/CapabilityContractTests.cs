extern alias netwasmTarget;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using NetWasm.Microsoft.Extensions.DependencyInjection.Generator;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationExtensions;
using static netwasmTarget::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions;

using GeneratedActivationDefinition = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDefinition;
using GeneratedActivationDescriptor = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationDescriptor;
using GeneratedActivationLookup = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookup;
using GeneratedActivationLookupBuilder = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationLookupBuilder;
using GeneratedActivationInitializer = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationInitializer;
using GeneratedActivationRegistrationReader = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationRegistrationReader;
using GeneratedActivationAssignabilityReader = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedActivationAssignabilityReader;
using GeneratedFactoryCandidate = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryCandidate;
using GeneratedFactoryRegistration = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryRegistration;
using GeneratedFactorySelection = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactorySelection;
using GeneratedFactoryShapeSelector = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryShapeSelector;
using GeneratedFactoryCreator = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedFactoryCreator;
using IGeneratedActivator = netwasmTarget::Microsoft.Extensions.DependencyInjection.IGeneratedActivator;
using IGeneratedActivationInitializer = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.IGeneratedActivationInitializer;
using IGeneratedActivationLookupBuilder = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.IGeneratedActivationLookupBuilder;
using IGeneratedActivationRegistrationReader = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.IGeneratedActivationRegistrationReader;
using IGeneratedActivationAssignabilityReader = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.IGeneratedActivationAssignabilityReader;
using IGeneratedFactoryShapeSelector = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.IGeneratedFactoryShapeSelector;
using IGeneratedFactoryCreator = netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.IGeneratedFactoryCreator;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Tests;

public sealed class CapabilityContractTests
{
#pragma warning disable CA1859 // Contract tests deliberately dispatch through each capability interface.
    [Fact]
    public void GeneratorCapabilitiesUseNarrowContractsAndSubstitutes()
    {
        const string source = "namespace Fixture; public sealed class Product { public Product(string value = \"default\") { Value = value; } public string Value { get; } }";
        var compilation = CreateCompilation(source);
        var product = compilation.GetTypeByMetadataName("Fixture.Product")!;
        IRegistrationSyntaxReader syntaxReader = new RegistrationSyntaxAdapter();
        var facts = syntaxReader.Read(new RegistrationSyntaxInput(product, product, "direct", "Transient"));

        IConstructorSelector constructorSelector = new ConstructorSelector();
        var selection = constructorSelector.Select(facts);
        Assert.Single(selection.Ordered);
        Assert.False(selection.IsPreferred);

        IOptionalDefaultExpressionFormatter formatter = new OptionalDefaultExpressionFormatter();
        Assert.Equal("\"default\"", formatter.Format(product.InstanceConstructors[0].Parameters[0].Type, "default"));

        var substituteFormatter = new RecordingFormatter();
        IActivationModelBuilder modelBuilder = new ActivationModelBuilder(
            new SubstituteConstructorSelector(selection),
            substituteFormatter);
        var model = modelBuilder.Build(facts);
        Assert.Equal(1, substituteFormatter.Calls);

        var expectedCallSite = new GeneratedCallSiteBuilder().Build(model);
        var emittedCallSite = new GeneratedCallSiteModel(
            model,
            new[] { new GeneratedCallSiteParameter("global::System.String", true, "\"from-call-site\"") });
        var recordingCallSiteBuilder = new RecordingCallSiteBuilder(emittedCallSite);
        IActivationSourceEmitter sourceEmitter = new ActivationSourceEmitter(recordingCallSiteBuilder);
        var sequenceEmitter = new RecordingSequenceSourceEmitter();
        var recordingLocator = new RecordingDescriptorLocator(model);
        IActivationDescriptorLocator locator = recordingLocator;
        IActivationManifestEmitter manifestEmitter = new ActivationManifestEmitter(locator, sequenceEmitter);
        var emittedSource = sourceEmitter.Emit(model);
        Assert.Contains("new global::Fixture.Product(", emittedSource, StringComparison.Ordinal);
        Assert.Contains("defaultValue: \"from-call-site\"", emittedSource, StringComparison.Ordinal);
        Assert.Equal(1, recordingCallSiteBuilder.Calls);
        Assert.Contains("public static GeneratedActivationManifest Manifest", manifestEmitter.Emit(new[] { model }), StringComparison.Ordinal);
        var sequenceManifest = manifestEmitter.Emit(
            new[] { model },
            new[] { new GeneratedSequenceModel("global::System.Collections.Generic.IEnumerable<global::Fixture.Product>", "global::Fixture.Product") });
        Assert.Contains("generated-sequence", sequenceManifest, StringComparison.Ordinal);
        Assert.Equal(1, sequenceEmitter.Calls);
        Assert.Equal(2, recordingLocator.Calls);

        IGeneratedCallSiteBuilder callSiteBuilder = new GeneratedCallSiteBuilder();
        var callSite = callSiteBuilder.Build(model);
        Assert.Equal("global::System.String", callSite.Parameters[0].TypeDisplay);

        Assert.Same(model, locator.Locate(new[] { model }, model.Facts.Identity));
        Assert.Null(locator.Locate(new[] { model }, "missing"));
    }

    [Fact]
    public void RuntimeCapabilitiesUseNarrowContractsAndSubstitutes()
    {
        var definition = new GeneratedActivationDefinition(
            "capability",
            typeof(RuntimeConsumer),
            typeof(RuntimeConsumer),
            Array.Empty<Type>(),
            Array.Empty<GeneratedActivationDefinition>(),
            isPreferred: false,
            Array.Empty<Type>());
        IGeneratedActivationLookupBuilder lookupBuilder = new GeneratedActivationLookupBuilder();
        var expectedLookup = lookupBuilder.Build(new[] { definition });

        var recordingBuilder = new RecordingLookupBuilder(expectedLookup);
        IGeneratedActivationInitializer initializer = new GeneratedActivationInitializer(recordingBuilder);
        var initializedLookup = initializer.Initialize(new[] { definition });
        _ = initializer.Initialize(Array.Empty<GeneratedActivationDefinition>());
        Assert.Equal(2, recordingBuilder.Calls);
        Assert.Same(expectedLookup, initializedLookup);

        IGeneratedActivationRegistrationReader registrationReader = new GeneratedActivationRegistrationReader(expectedLookup);
        Assert.True(registrationReader.TryGetRegistration(typeof(RuntimeConsumer), out var registration));
        Assert.Equal("capability", registration.Candidates[0].Identity);
        Assert.False(registrationReader.TryGetRegistration(typeof(Unregistered), out _));

        IGeneratedActivationAssignabilityReader assignabilityReader = new GeneratedActivationAssignabilityReader(expectedLookup);
        Assert.True(assignabilityReader.IsAssignable(typeof(RuntimeConsumer), typeof(RuntimeConsumer)));
        Assert.False(assignabilityReader.IsAssignable(typeof(string), typeof(int)));

        var candidate = new GeneratedFactoryCandidate("capability", Array.Empty<Type>(), isPreferred: false);
        var candidateRegistration = new GeneratedFactoryRegistration(new[] { candidate });
        IGeneratedFactoryShapeSelector shapeSelector = new GeneratedFactoryShapeSelector(
            new StubRegistrationReader(candidateRegistration),
            new StubAssignabilityReader());
        var selected = shapeSelector.Select(typeof(RuntimeConsumer), Array.Empty<Type>());
        Assert.Equal("capability", selected.Identity);

        IGeneratedFactoryCreator factoryCreator = new GeneratedFactoryCreator(new StubShapeSelector(selected));
        Assert.Equal("capability", factoryCreator.Create(typeof(RuntimeConsumer), Array.Empty<Type>()).Identity);
        Assert.Throws<ArgumentNullException>(() => new GeneratedFactoryCreator(null!));

        var recordingActivator = new RecordingActivator();
        IGeneratedActivator activator = recordingActivator;
        var activated = activator.Create(
            typeof(RuntimeConsumer),
            Array.Empty<object?>(),
            Array.Empty<Type?>(),
            "capability");
        Assert.Same(recordingActivator.Result, activated);
        Assert.Equal("capability", recordingActivator.SelectedIdentity);

        var services = new ServiceCollection();
        services.AddGeneratedTransient(new GeneratedActivationDescriptor(
            "capability",
            typeof(RuntimeConsumer),
            typeof(RuntimeConsumer),
            Array.Empty<netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedParameter>(),
            _ => new RuntimeConsumer()));
        using var provider = services.BuildServiceProvider();
        IGeneratedActivator providerActivator = provider;
        Assert.IsType<RuntimeConsumer>(providerActivator.Create(
            typeof(RuntimeConsumer),
            Array.Empty<object?>(),
            Array.Empty<Type?>(),
            "capability"));
    }
#pragma warning restore CA1859

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        return CSharpCompilation.Create(
            "CapabilityFixture",
            sources.Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private sealed class RuntimeConsumer { }

    private sealed class Unregistered { }

    private sealed class SubstituteConstructorSelector(ConstructorSelection selection) : IConstructorSelector
    {
        public ConstructorSelection Select(RegistrationSyntaxFacts facts) => selection;
    }

    private sealed class RecordingFormatter : IOptionalDefaultExpressionFormatter
    {
        public int Calls { get; private set; }

        public string Format(ITypeSymbol type, object? value)
        {
            Calls++;
            return "\"default\"";
        }
    }

    private sealed class RecordingSequenceSourceEmitter : IGeneratedSequenceSourceEmitter
    {
        public int Calls { get; private set; }

        public string Emit(GeneratedSequenceModel sequence)
        {
            Calls++;
            return "generated-sequence";
        }
    }

    private sealed class RecordingCallSiteBuilder(GeneratedCallSiteModel result) : IGeneratedCallSiteBuilder
    {
        public int Calls { get; private set; }

        public GeneratedCallSiteModel Build(ActivationModel activation)
        {
            Calls++;
            return result;
        }
    }

    private sealed class RecordingDescriptorLocator(ActivationModel result) : IActivationDescriptorLocator
    {
        public int Calls { get; private set; }

        public ActivationModel? Locate(IReadOnlyList<ActivationModel> activations, string identity)
        {
            Calls++;
            return identity == result.Facts.Identity ? result : null;
        }
    }

    private sealed class RecordingLookupBuilder(GeneratedActivationLookup lookup) : IGeneratedActivationLookupBuilder
    {
        public int Calls { get; private set; }

        public GeneratedActivationLookup Build(
            IReadOnlyList<GeneratedActivationDefinition> activations,
            IReadOnlyList<netwasmTarget::Microsoft.Extensions.DependencyInjection.Generated.GeneratedSequenceDefinition>? sequences = null)
        {
            Calls++;
            return lookup;
        }
    }

    private sealed class StubRegistrationReader(GeneratedFactoryRegistration registration) : IGeneratedActivationRegistrationReader
    {
        public bool TryGetRegistration(Type implementationType, out GeneratedFactoryRegistration value)
        {
            if (implementationType == typeof(RuntimeConsumer))
            {
                value = registration;
                return true;
            }

            value = null!;
            return false;
        }
    }

    private sealed class StubAssignabilityReader : IGeneratedActivationAssignabilityReader
    {
        public bool IsAssignable(Type suppliedType, Type expectedType) => suppliedType == expectedType;
    }

    private sealed class StubShapeSelector(GeneratedFactorySelection selection) : IGeneratedFactoryShapeSelector
    {
        public GeneratedFactorySelection Select(Type implementationType, IReadOnlyList<Type> suppliedTypes) => selection;
    }

    private sealed class RecordingActivator : IGeneratedActivator
    {
        public object Result { get; } = new object();

        public string? SelectedIdentity { get; private set; }

        public object? Create(Type instanceType, object?[] parameters, Type?[] parameterTypes, string? selectedIdentity)
        {
            SelectedIdentity = selectedIdentity;
            return Result;
        }
    }
}
