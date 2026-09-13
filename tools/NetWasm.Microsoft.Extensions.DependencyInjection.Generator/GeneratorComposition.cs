namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

/// <summary>Composes the generator feature's policy collaborators at one boundary.</summary>
internal static class GeneratorComposition
{
    internal static IActivationModelBuilder CreateActivationModelBuilder() =>
        new ActivationModelBuilder(new ConstructorSelector(), new OptionalDefaultExpressionFormatter());

    internal static IActivationManifestEmitter CreateActivationManifestEmitter() =>
        new ActivationManifestEmitter(new ActivationDescriptorLocator(), new GeneratedSequenceSourceEmitter());

    internal static IActivationSourceEmitter CreateActivationSourceEmitter() =>
        new ActivationSourceEmitter(new GeneratedCallSiteBuilder());

    internal static GeneratedActivationSourcePipeline CreateActivationSourcePipeline() =>
        new(
            new RegistrationSyntaxAdapter(),
            CreateActivationModelBuilder(),
            CreateActivationSourceEmitter(),
            CreateActivationManifestEmitter());

    internal static IOpenGenericActivationClosurePlanner CreateOpenGenericActivationClosurePlanner() =>
        new OpenGenericActivationClosurePlanner();

    internal static IActivationClosureValidator CreateActivationClosureValidator() =>
        new ActivationClosureValidator();

    internal static IGeneratedApplicationMetadataReader CreateApplicationMetadataReader() =>
        new GeneratedApplicationMetadataReader();

    internal static IGeneratedApplicationMetadataEmitter CreateApplicationMetadataEmitter() =>
        new GeneratedApplicationMetadataEmitter();
}
