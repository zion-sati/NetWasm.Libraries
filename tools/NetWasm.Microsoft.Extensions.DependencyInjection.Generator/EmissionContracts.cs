namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal interface IActivationSourceEmitter
{
    string Emit(ActivationModel model);
}

internal interface IActivationManifestEmitter
{
    string Emit(IReadOnlyList<ActivationModel> activations);

    string Emit(IReadOnlyList<ActivationModel> activations, IReadOnlyList<GeneratedSequenceModel> sequences);

    string Emit(
        IReadOnlyList<ActivationModel> activations,
        IReadOnlyList<GeneratedSequenceModel> sequences,
        IReadOnlyList<OpenGenericRegistrationInput> openGenericRegistrations);
}

internal sealed class GeneratedSequenceModel
{
    internal GeneratedSequenceModel(string sequenceTypeDisplay, string elementTypeDisplay, string? serviceKeyExpression = null)
    {
        Guard.NotNullOrEmpty(sequenceTypeDisplay, nameof(sequenceTypeDisplay));
        Guard.NotNullOrEmpty(elementTypeDisplay, nameof(elementTypeDisplay));
        SequenceTypeDisplay = sequenceTypeDisplay;
        ElementTypeDisplay = elementTypeDisplay;
        ServiceKeyExpression = serviceKeyExpression;
    }

    internal string SequenceTypeDisplay { get; }
    internal string ElementTypeDisplay { get; }
    internal string? ServiceKeyExpression { get; }
}

internal interface IGeneratedSequenceSourceEmitter
{
    string Emit(GeneratedSequenceModel sequence);
}

internal sealed class GeneratedCallSiteModel
{
    internal GeneratedCallSiteModel(ActivationModel activation, IReadOnlyList<GeneratedCallSiteParameter> parameters)
    {
        Activation = activation;
        Parameters = parameters;
    }

    internal ActivationModel Activation { get; }
    internal IReadOnlyList<GeneratedCallSiteParameter> Parameters { get; }
}

internal sealed class GeneratedCallSiteParameter
{
    internal GeneratedCallSiteParameter(
        string typeDisplay,
        bool hasDefaultValue,
        string? defaultExpression,
        string? serviceKeyExpression = null,
        string lookupMode = "NullKey",
        string? constantExpression = null)
    {
        TypeDisplay = typeDisplay;
        HasDefaultValue = hasDefaultValue;
        DefaultExpression = defaultExpression;
        ServiceKeyExpression = serviceKeyExpression;
        LookupMode = lookupMode;
        ConstantExpression = constantExpression;
    }

    internal string TypeDisplay { get; }
    internal bool HasDefaultValue { get; }
    internal string? DefaultExpression { get; }
    internal string? ServiceKeyExpression { get; }
    internal string LookupMode { get; }
    internal string? ConstantExpression { get; }
}

internal interface IGeneratedCallSiteBuilder
{
    GeneratedCallSiteModel Build(ActivationModel activation);
}
