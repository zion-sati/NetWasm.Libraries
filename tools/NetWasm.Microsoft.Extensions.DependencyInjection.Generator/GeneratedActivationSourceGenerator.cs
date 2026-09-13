using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

/// <summary>One closed registration requested from the development-host generator.</summary>
public sealed class GeneratedActivationRegistration
{
    public GeneratedActivationRegistration(
        string serviceTypeMetadataName,
        string implementationTypeMetadataName,
        string? identity,
        string lifetime,
        string? serviceKeyExpression = null)
    {
        Guard.NotNullOrEmpty(serviceTypeMetadataName, nameof(serviceTypeMetadataName));
        Guard.NotNullOrEmpty(implementationTypeMetadataName, nameof(implementationTypeMetadataName));
        Guard.NotNullOrEmpty(lifetime, nameof(lifetime));
        ServiceTypeMetadataName = serviceTypeMetadataName;
        ImplementationTypeMetadataName = implementationTypeMetadataName;
        Identity = identity;
        Lifetime = lifetime;
        ServiceKeyExpression = serviceKeyExpression;
    }

    public string ServiceTypeMetadataName { get; }

    public string ImplementationTypeMetadataName { get; }

    public string? Identity { get; }

    public string Lifetime { get; }

    public string? ServiceKeyExpression { get; }
}

/// <summary>One generated sequence requested from the development-host generator.</summary>
public sealed class GeneratedActivationSequence
{
    public GeneratedActivationSequence(
        string sequenceTypeDisplay,
        string elementTypeDisplay,
        string? serviceKeyExpression = null)
    {
        Guard.NotNullOrEmpty(sequenceTypeDisplay, nameof(sequenceTypeDisplay));
        Guard.NotNullOrEmpty(elementTypeDisplay, nameof(elementTypeDisplay));
        SequenceTypeDisplay = sequenceTypeDisplay;
        ElementTypeDisplay = elementTypeDisplay;
        ServiceKeyExpression = serviceKeyExpression;
    }

    public string SequenceTypeDisplay { get; }

    public string ElementTypeDisplay { get; }

    public string? ServiceKeyExpression { get; }
}

/// <summary>Source and immutable manifest text emitted by the development-host generator.</summary>
public sealed class GeneratedActivationSourceResult
{
    internal GeneratedActivationSourceResult(
        IReadOnlyList<string> activationSources,
        string manifestSource)
    {
        Guard.NotNull(activationSources, nameof(activationSources));
        Guard.NotNull(manifestSource, nameof(manifestSource));
        ActivationSources = Array.AsReadOnly(activationSources.ToArray());
        ManifestSource = manifestSource;
    }

    public IReadOnlyList<string> ActivationSources { get; }

    public string ManifestSource { get; }
}

/// <summary>
/// Development-host entry point for deterministic DI activation source generation.
/// The runtime package consumes only the emitted source and manifest.
/// </summary>
public sealed class GeneratedActivationSourceGenerator
{
    private readonly IGeneratedActivationSourcePipeline _pipeline;
    private readonly IOpenGenericActivationClosurePlanner _closurePlanner;
    private readonly IActivationClosureValidator _closureValidator;

    public GeneratedActivationSourceGenerator()
        : this(
            GeneratorComposition.CreateActivationSourcePipeline(),
            GeneratorComposition.CreateOpenGenericActivationClosurePlanner(),
            GeneratorComposition.CreateActivationClosureValidator())
    {
    }

    internal GeneratedActivationSourceGenerator(IGeneratedActivationSourcePipeline pipeline)
        : this(
            pipeline,
            GeneratorComposition.CreateOpenGenericActivationClosurePlanner(),
            GeneratorComposition.CreateActivationClosureValidator())
    {
    }

    internal GeneratedActivationSourceGenerator(
        IGeneratedActivationSourcePipeline pipeline,
        IOpenGenericActivationClosurePlanner closurePlanner,
        IActivationClosureValidator closureValidator)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _closurePlanner = closurePlanner ?? throw new ArgumentNullException(nameof(closurePlanner));
        _closureValidator = closureValidator ?? throw new ArgumentNullException(nameof(closureValidator));
    }

    public GeneratedActivationSourceResult Generate(
        Compilation compilation,
        IReadOnlyList<GeneratedActivationRegistration> registrations,
        IReadOnlyList<GeneratedActivationSequence>? sequences = null,
        IReadOnlyList<GeneratedOpenGenericRegistration>? openGenericRegistrations = null,
        IReadOnlyList<GeneratedClosedGenericRequest>? closedGenericRequests = null)
    {
        Guard.NotNull(compilation, nameof(compilation));
        Guard.NotNull(registrations, nameof(registrations));
        sequences ??= Array.Empty<GeneratedActivationSequence>();
        openGenericRegistrations ??= Array.Empty<GeneratedOpenGenericRegistration>();
        closedGenericRequests ??= Array.Empty<GeneratedClosedGenericRequest>();

        var syntaxInputs = new List<RegistrationSyntaxInput>(registrations.Count + closedGenericRequests.Count);
        for (var index = 0; index < registrations.Count; index++)
        {
            var registration = registrations[index]
                ?? throw new ArgumentException("Generated registrations cannot contain null entries.", nameof(registrations));
            syntaxInputs.Add(Resolve(compilation, registration));
        }

        var plannedSequences = new List<GeneratedSequenceModel>();
        var templateInputs = Array.Empty<OpenGenericRegistrationInput>();
        if (openGenericRegistrations.Count != 0 || closedGenericRequests.Count != 0)
        {
            templateInputs = Resolve(compilation, openGenericRegistrations);
            var requestInputs = Resolve(compilation, closedGenericRequests);
            var closure = _closurePlanner.Plan(templateInputs, requestInputs);
            _closureValidator.Validate(closure.Activations);
            syntaxInputs.AddRange(closure.Activations.Select(activation => activation.ToRegistrationSyntaxInput()));
            plannedSequences.AddRange(closure.Sequences);
        }

        var sequenceModels = new List<GeneratedSequenceModel>(sequences.Count + plannedSequences.Count);
        for (var index = 0; index < sequences.Count; index++)
        {
            var sequence = sequences[index]
                ?? throw new ArgumentException("Generated sequences cannot contain null entries.", nameof(sequences));
            sequenceModels.Add(new GeneratedSequenceModel(
                sequence.SequenceTypeDisplay,
                sequence.ElementTypeDisplay,
                sequence.ServiceKeyExpression));
        }

        sequenceModels.AddRange(plannedSequences);

        var output = _pipeline.Generate(syntaxInputs, sequenceModels, templateInputs);
        return new GeneratedActivationSourceResult(output.ActivationSources, output.ManifestSource);
    }

    private static RegistrationSyntaxInput Resolve(
        Compilation compilation,
        GeneratedActivationRegistration registration)
    {
        var serviceType = compilation.GetTypeByMetadataName(registration.ServiceTypeMetadataName)
            ?? throw new GeneratorDiagnosticException("NWDI008", $"Service type '{registration.ServiceTypeMetadataName}' was not found.");
        var implementationType = compilation.GetTypeByMetadataName(registration.ImplementationTypeMetadataName)
            ?? throw new GeneratorDiagnosticException("NWDI008", $"Implementation type '{registration.ImplementationTypeMetadataName}' was not found.");
        return new RegistrationSyntaxInput(
            serviceType,
            implementationType,
            registration.Identity,
            registration.Lifetime,
            registration.ServiceKeyExpression);
    }

    private static OpenGenericRegistrationInput[] Resolve(
        Compilation compilation,
        IReadOnlyList<GeneratedOpenGenericRegistration> registrations)
    {
        var inputs = new OpenGenericRegistrationInput[registrations.Count];
        for (var index = 0; index < registrations.Count; index++)
        {
            var registration = registrations[index]
                ?? throw new ArgumentException("Open-generic registrations cannot contain null entries.", nameof(registrations));
            var serviceType = compilation.GetTypeByMetadataName(registration.ServiceTypeMetadataName)
                ?? throw new GeneratorDiagnosticException("NWDI008", $"Open-generic service type '{registration.ServiceTypeMetadataName}' was not found.");
            var implementationType = compilation.GetTypeByMetadataName(registration.ImplementationTypeMetadataName)
                ?? throw new GeneratorDiagnosticException("NWDI008", $"Open-generic implementation type '{registration.ImplementationTypeMetadataName}' was not found.");
            inputs[index] = new OpenGenericRegistrationInput(
                serviceType,
                implementationType,
                registration.Lifetime,
                registration.Identity,
                registration.ServiceKeyExpression);
        }

        return inputs;
    }

    private static ClosedGenericRequestInput[] Resolve(
        Compilation compilation,
        IReadOnlyList<GeneratedClosedGenericRequest> requests)
    {
        var inputs = new ClosedGenericRequestInput[requests.Count];
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index]
                ?? throw new ArgumentException("Closed-generic requests cannot contain null entries.", nameof(requests));
            var serviceDefinition = compilation.GetTypeByMetadataName(request.ServiceTypeMetadataName)
                ?? throw new GeneratorDiagnosticException("NWDI008", $"Closed-generic service type '{request.ServiceTypeMetadataName}' was not found.");
            var typeArguments = new ITypeSymbol[request.TypeArgumentMetadataNames.Count];
            for (var argumentIndex = 0; argumentIndex < typeArguments.Length; argumentIndex++)
            {
                var metadataName = request.TypeArgumentMetadataNames[argumentIndex];
                typeArguments[argumentIndex] = compilation.GetTypeByMetadataName(metadataName)
                    ?? throw new GeneratorDiagnosticException("NWDI008", $"Closed-generic type argument '{metadataName}' was not found.");
            }

            INamedTypeSymbol serviceType;
            try
            {
                serviceType = serviceDefinition.Construct(typeArguments);
            }
            catch (ArgumentException exception)
            {
                throw new GeneratorDiagnosticException("NWDI016", "The service request does not satisfy its generic constraints.", exception);
            }

            inputs[index] = new ClosedGenericRequestInput(serviceType, request.ServiceKeyExpression);
        }

        return inputs;
    }
}
