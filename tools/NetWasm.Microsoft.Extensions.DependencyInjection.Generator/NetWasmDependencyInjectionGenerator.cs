using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

/// <summary>Normal package-owned Roslyn entry point for closed-world DI activation generation.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class NetWasmDependencyInjectionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var invocations = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax,
                static (syntaxContext, _) => (InvocationExpressionSyntax)syntaxContext.Node)
            .Collect();
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(invocations),
            static (productionContext, input) => Execute(productionContext, input.Left, input.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        System.Collections.Immutable.ImmutableArray<InvocationExpressionSyntax> invocations)
    {
        try
        {
            var reader = new RegistrationInvocationReader();
            var inputs = reader.Read(compilation, invocations);
            var registrations = new List<RegistrationSyntaxInput>(inputs.Registrations);
            var sequences = new List<GeneratedSequenceModel>(inputs.Sequences);
            var localTemplates = EmitOpenGenericTemplates(context, inputs.OpenGenericRegistrations);
            var referencedMetadata = GeneratorComposition.CreateApplicationMetadataReader().Read(compilation);
            var openGenericRegistrations = MergeTemplates(localTemplates, referencedMetadata.OpenGenericRegistrations);
            var closedGenericRequests = MergeRequests(inputs.ClosedGenericRequests, referencedMetadata.ClosedGenericRequests);
            var matchedRequestIdentities = new HashSet<string>(StringComparer.Ordinal);
            if (openGenericRegistrations.Length != 0)
            {
                var requests = closedGenericRequests
                    .Where(request => openGenericRegistrations.Any(template =>
                        SymbolEqualityComparer.Default.Equals(
                            template.ServiceType.OriginalDefinition,
                            request.ServiceType.OriginalDefinition) &&
                        OpenGenericServiceKeyPolicy.Matches(template.ServiceKeyExpression, request.ServiceKeyExpression)))
                    .ToArray();
                foreach (var request in requests)
                {
                    matchedRequestIdentities.Add(RequestIdentity(request));
                }

                if (requests.Length != 0)
                {
                    var closure = GeneratorComposition.CreateOpenGenericActivationClosurePlanner().Plan(
                        openGenericRegistrations,
                        requests);
                    GeneratorComposition.CreateActivationClosureValidator().Validate(closure.Activations);
                    registrations.AddRange(closure.Activations.Select(activation => activation.ToRegistrationSyntaxInput()));
                    sequences.AddRange(closure.Sequences);
                }
            }

            var unmatchedRequests = closedGenericRequests
                .Where(request => !matchedRequestIdentities.Contains(RequestIdentity(request)))
                .ToArray();
            if (localTemplates.Count != 0 || unmatchedRequests.Length != 0)
            {
                context.AddSource(
                    "NetWasm.DependencyInjection.ApplicationMetadata.g.cs",
                    GeneratorComposition.CreateApplicationMetadataEmitter().Emit(localTemplates, unmatchedRequests));
            }

            if (registrations.Count == 0 &&
                sequences.Count == 0 &&
                localTemplates.Count == 0)
            {
                return;
            }

            var uniqueRegistrations = DedupeRegistrations(registrations);
            var uniqueSequences = DedupeSequences(sequences);
            var output = GeneratorComposition.CreateActivationSourcePipeline().Generate(
                uniqueRegistrations,
                uniqueSequences,
                localTemplates);
            for (var index = 0; index < output.ActivationSources.Count; index++)
            {
                context.AddSource($"NetWasm.DependencyInjection.Activation.{index:D4}.g.cs", output.ActivationSources[index]);
            }

            context.AddSource("NetWasm.DependencyInjection.Manifest.g.cs", output.ManifestSource);
            var registrationEmitter = new GeneratedCatalogRegistrationEmitter();
            context.AddSource("NetWasm.DependencyInjection.ModuleInitializer.g.cs", registrationEmitter.Emit());
        }
        catch (GeneratorDiagnosticException exception)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    exception.Code,
                    "NetWasm dependency injection source generation",
                    "{0}",
                    "NetWasm.DependencyInjection",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                exception.Message));
        }
    }

    private static IReadOnlyList<OpenGenericRegistrationInput> EmitOpenGenericTemplates(
        SourceProductionContext context,
        IReadOnlyList<OpenGenericRegistrationInput> templates)
    {
        if (templates.Count == 0)
        {
            return templates;
        }

        var syntaxReader = new RegistrationSyntaxAdapter();
        var modelBuilder = GeneratorComposition.CreateActivationModelBuilder();
        var sourceEmitter = GeneratorComposition.CreateActivationSourceEmitter();
        var result = new OpenGenericRegistrationInput[templates.Count];
        for (var index = 0; index < templates.Count; index++)
        {
            var template = templates[index];
            var model = modelBuilder.Build(syntaxReader.Read(new RegistrationSyntaxInput(
                template.ServiceType,
                template.ImplementationType,
                template.Identity,
                template.Lifetime,
                template.ServiceKeyExpression,
                template.ServiceType,
                template.ImplementationType,
                template.ServiceKeyExpression,
                IsOpenGenericTemplate: true)));
            context.AddSource(
                $"NetWasm.DependencyInjection.OpenGenericTemplate.{index:D4}.g.cs",
                sourceEmitter.Emit(model));
            var factoryTypeName = "global::" + model.Facts.NamespaceName + "." + model.Facts.TypeName;
            result[index] = template with { FactoryTypeName = factoryTypeName };
        }

        return result;
    }

    private static OpenGenericRegistrationInput[] MergeTemplates(
        IReadOnlyList<OpenGenericRegistrationInput> local,
        IReadOnlyList<OpenGenericRegistrationInput> referenced)
    {
        var merged = new Dictionary<string, OpenGenericRegistrationInput>(StringComparer.Ordinal);
        foreach (var template in referenced.Concat(local))
        {
            merged[TemplateIdentity(template)] = template;
        }

        return merged.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray();
    }

    private static ClosedGenericRequestInput[] MergeRequests(
        IReadOnlyList<ClosedGenericRequestInput> local,
        IReadOnlyList<ClosedGenericRequestInput> referenced)
    {
        var merged = new Dictionary<string, ClosedGenericRequestInput>(StringComparer.Ordinal);
        foreach (var request in referenced.Concat(local))
        {
            merged[RequestIdentity(request)] = request;
        }

        return merged.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray();
    }

    private static string TemplateIdentity(OpenGenericRegistrationInput template) =>
        SymbolIdentity(template.ServiceType) + "|" + SymbolIdentity(template.ImplementationType) + "|" +
        template.ServiceKeyExpression;

    private static string RequestIdentity(ClosedGenericRequestInput request) =>
        SymbolIdentity(request.ServiceType) + "|" + request.ServiceKeyExpression;

    private static string SymbolIdentity(INamedTypeSymbol symbol) =>
        symbol.ContainingAssembly.Identity + "|" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static RegistrationSyntaxInput[] DedupeRegistrations(IReadOnlyList<RegistrationSyntaxInput> registrations)
    {
        var unique = new Dictionary<string, RegistrationSyntaxInput>(StringComparer.Ordinal);
        foreach (var registration in registrations)
        {
            var key = SymbolIdentity(registration.ServiceType) + "|" +
                SymbolIdentity(registration.ImplementationType) + "|" + registration.ServiceKeyExpression;
            unique[key] = registration;
        }

        return unique.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray();
    }

    private static GeneratedSequenceModel[] DedupeSequences(IReadOnlyList<GeneratedSequenceModel> sequences)
    {
        var unique = new Dictionary<string, GeneratedSequenceModel>(StringComparer.Ordinal);
        foreach (var sequence in sequences)
        {
            var key = sequence.SequenceTypeDisplay + "|" + sequence.ServiceKeyExpression;
            unique[key] = sequence;
        }

        return unique.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray();
    }
}
