using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed record RegistrationSyntaxInput(
    INamedTypeSymbol ServiceType,
    INamedTypeSymbol ImplementationType,
    string? Identity,
    string Lifetime,
    string? ServiceKeyExpression = null,
    INamedTypeSymbol? TemplateServiceType = null,
    INamedTypeSymbol? TemplateImplementationType = null,
    string? TemplateServiceKeyExpression = null,
    bool IsOpenGenericTemplate = false,
    string? FactoryInvocationExpression = null);

/// <summary>Validated syntax facts; constructor policy and default formatting are deliberately separate actors.</summary>
internal sealed class RegistrationSyntaxFacts
{
    internal RegistrationSyntaxFacts(
        RegistrationSyntaxInput input,
        IReadOnlyList<IMethodSymbol> publicConstructors,
        IReadOnlyList<IMethodSymbol> markedConstructors)
    {
        Input = input;
        PublicConstructors = publicConstructors;
        MarkedConstructors = markedConstructors;
    }

    internal RegistrationSyntaxInput Input { get; }

    internal IReadOnlyList<IMethodSymbol> PublicConstructors { get; }

    internal IReadOnlyList<IMethodSymbol> MarkedConstructors { get; }
}

internal interface IRegistrationSyntaxReader
{
    RegistrationSyntaxFacts Read(RegistrationSyntaxInput input);
}
