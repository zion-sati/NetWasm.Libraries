namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

/// <summary>Runs the source-generation stages for one closed registration set.</summary>
internal interface IGeneratedActivationSourcePipeline
{
    GeneratedActivationSourceOutput Generate(
        IReadOnlyList<RegistrationSyntaxInput> registrations,
        IReadOnlyList<GeneratedSequenceModel>? sequences = null,
        IReadOnlyList<OpenGenericRegistrationInput>? openGenericRegistrations = null);
}

internal sealed class GeneratedActivationSourcePipeline : IGeneratedActivationSourcePipeline
{
    private readonly IRegistrationSyntaxReader _syntaxReader;
    private readonly IActivationModelBuilder _modelBuilder;
    private readonly IActivationSourceEmitter _sourceEmitter;
    private readonly IActivationManifestEmitter _manifestEmitter;

    internal GeneratedActivationSourcePipeline(
        IRegistrationSyntaxReader syntaxReader,
        IActivationModelBuilder modelBuilder,
        IActivationSourceEmitter sourceEmitter,
        IActivationManifestEmitter manifestEmitter)
    {
        _syntaxReader = syntaxReader ?? throw new ArgumentNullException(nameof(syntaxReader));
        _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
        _sourceEmitter = sourceEmitter ?? throw new ArgumentNullException(nameof(sourceEmitter));
        _manifestEmitter = manifestEmitter ?? throw new ArgumentNullException(nameof(manifestEmitter));
    }

    internal GeneratedActivationSourceOutput Generate(
        IReadOnlyList<RegistrationSyntaxInput> registrations,
        IReadOnlyList<GeneratedSequenceModel>? sequences = null,
        IReadOnlyList<OpenGenericRegistrationInput>? openGenericRegistrations = null)
    {
        Guard.NotNull(registrations, nameof(registrations));
        sequences ??= Array.Empty<GeneratedSequenceModel>();
        openGenericRegistrations ??= Array.Empty<OpenGenericRegistrationInput>();

        var models = new ActivationModel[registrations.Count];
        var activationSources = new string[registrations.Count];
        for (var index = 0; index < registrations.Count; index++)
        {
            var input = registrations[index] ?? throw new ArgumentException("Generated registration inputs cannot contain null entries.", nameof(registrations));
            var facts = _syntaxReader.Read(input);
            models[index] = _modelBuilder.Build(facts);
            activationSources[index] = _sourceEmitter.Emit(models[index]);
        }

        return new GeneratedActivationSourceOutput(
            Array.AsReadOnly(models),
            Array.AsReadOnly(activationSources),
            _manifestEmitter.Emit(models, sequences, openGenericRegistrations));
    }

    GeneratedActivationSourceOutput IGeneratedActivationSourcePipeline.Generate(
        IReadOnlyList<RegistrationSyntaxInput> registrations,
        IReadOnlyList<GeneratedSequenceModel>? sequences,
        IReadOnlyList<OpenGenericRegistrationInput>? openGenericRegistrations) =>
        Generate(registrations, sequences, openGenericRegistrations);
}

/// <summary>Immutable source output produced by the generation pipeline.</summary>
internal sealed class GeneratedActivationSourceOutput
{
    internal GeneratedActivationSourceOutput(
        IReadOnlyList<ActivationModel> activations,
        IReadOnlyList<string> activationSources,
        string manifestSource)
    {
        Guard.NotNull(activations, nameof(activations));
        Guard.NotNull(activationSources, nameof(activationSources));
        Guard.NotNull(manifestSource, nameof(manifestSource));
        if (activations.Count != activationSources.Count)
        {
            throw new ArgumentException("Generated activation models and source outputs must have matching counts.", nameof(activationSources));
        }

        ActivationSources = activationSources;
        ManifestSource = manifestSource;
    }

    internal IReadOnlyList<string> ActivationSources { get; }

    internal string ManifestSource { get; }
}
