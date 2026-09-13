using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

/// <summary>One package-owned open service/implementation template.</summary>
public sealed class GeneratedOpenGenericRegistration
{
    public GeneratedOpenGenericRegistration(
        string serviceTypeMetadataName,
        string implementationTypeMetadataName,
        string lifetime,
        string? identity = null,
        string? serviceKeyExpression = null)
    {
        Guard.NotNullOrEmpty(serviceTypeMetadataName, nameof(serviceTypeMetadataName));
        Guard.NotNullOrEmpty(implementationTypeMetadataName, nameof(implementationTypeMetadataName));
        Guard.NotNullOrEmpty(lifetime, nameof(lifetime));
        ServiceTypeMetadataName = serviceTypeMetadataName;
        ImplementationTypeMetadataName = implementationTypeMetadataName;
        Lifetime = lifetime;
        Identity = identity;
        ServiceKeyExpression = serviceKeyExpression;
    }

    public string ServiceTypeMetadataName { get; }

    public string ImplementationTypeMetadataName { get; }

    public string Lifetime { get; }

    public string? Identity { get; }

    public string? ServiceKeyExpression { get; }
}

/// <summary>One constructed service request reachable from the application build.</summary>
public sealed class GeneratedClosedGenericRequest
{
    public GeneratedClosedGenericRequest(
        string serviceTypeMetadataName,
        IReadOnlyList<string> typeArgumentMetadataNames,
        string? serviceKeyExpression = null)
    {
        Guard.NotNullOrEmpty(serviceTypeMetadataName, nameof(serviceTypeMetadataName));
        Guard.NotNull(typeArgumentMetadataNames, nameof(typeArgumentMetadataNames));
        if (typeArgumentMetadataNames.Count == 0)
        {
            throw new ArgumentException("Closed generic requests require at least one type argument.", nameof(typeArgumentMetadataNames));
        }

        if (typeArgumentMetadataNames.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Closed generic request type arguments cannot be empty.", nameof(typeArgumentMetadataNames));
        }

        ServiceTypeMetadataName = serviceTypeMetadataName;
        TypeArgumentMetadataNames = Array.AsReadOnly(typeArgumentMetadataNames.ToArray());
        ServiceKeyExpression = serviceKeyExpression;
    }

    public string ServiceTypeMetadataName { get; }

    public IReadOnlyList<string> TypeArgumentMetadataNames { get; }

    public string? ServiceKeyExpression { get; }
}

internal sealed record OpenGenericRegistrationInput(
    INamedTypeSymbol ServiceType,
    INamedTypeSymbol ImplementationType,
    string Lifetime,
    string? Identity = null,
    string? ServiceKeyExpression = null,
    string? FactoryTypeName = null);

internal sealed record ClosedGenericRequestInput(
    INamedTypeSymbol ServiceType,
    string? ServiceKeyExpression = null);

internal sealed class GeneratedActivationClosurePlan
{
    internal GeneratedActivationClosurePlan(
        IReadOnlyList<GeneratedClosedGenericActivation> activations,
        IReadOnlyList<GeneratedSequenceModel> sequences)
    {
        Guard.NotNull(activations, nameof(activations));
        Guard.NotNull(sequences, nameof(sequences));
        Activations = activations;
        Sequences = sequences;
    }

    internal IReadOnlyList<GeneratedClosedGenericActivation> Activations { get; }

    internal IReadOnlyList<GeneratedSequenceModel> Sequences { get; }
}

internal sealed class GeneratedClosedGenericActivation
{
    internal GeneratedClosedGenericActivation(
        string identity,
        INamedTypeSymbol serviceType,
        INamedTypeSymbol implementationType,
        string lifetime,
        string? serviceKeyExpression,
        INamedTypeSymbol templateServiceType,
        INamedTypeSymbol templateImplementationType,
        string? templateServiceKeyExpression,
        string? factoryInvocationExpression)
    {
        Identity = identity;
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        ServiceKeyExpression = serviceKeyExpression;
        TemplateServiceType = templateServiceType;
        TemplateImplementationType = templateImplementationType;
        TemplateServiceKeyExpression = templateServiceKeyExpression;
        FactoryInvocationExpression = factoryInvocationExpression;
    }

    internal string Identity { get; }

    internal INamedTypeSymbol ServiceType { get; }

    internal INamedTypeSymbol ImplementationType { get; }

    internal string Lifetime { get; }

    internal string? ServiceKeyExpression { get; }

    internal INamedTypeSymbol TemplateServiceType { get; }

    internal INamedTypeSymbol TemplateImplementationType { get; }

    internal string? TemplateServiceKeyExpression { get; }

    internal string? FactoryInvocationExpression { get; }

    internal RegistrationSyntaxInput ToRegistrationSyntaxInput() =>
        new(
            ServiceType,
            ImplementationType,
            Identity,
            Lifetime,
            ServiceKeyExpression,
            TemplateServiceType,
            TemplateImplementationType,
            TemplateServiceKeyExpression,
            FactoryInvocationExpression: FactoryInvocationExpression);
}

internal interface IOpenGenericActivationClosurePlanner
{
    GeneratedActivationClosurePlan Plan(
        IReadOnlyList<OpenGenericRegistrationInput> templates,
        IReadOnlyList<ClosedGenericRequestInput> requests);
}

internal interface IActivationClosureValidator
{
    void Validate(IReadOnlyList<GeneratedClosedGenericActivation> activations);
}
