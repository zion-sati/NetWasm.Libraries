using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

/// <summary>Adapts Roslyn registration symbols into validated, policy-neutral syntax facts.</summary>
internal sealed class RegistrationSyntaxAdapter : IRegistrationSyntaxReader
{
    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public RegistrationSyntaxFacts Read(RegistrationSyntaxInput input)
    {
        Guard.NotNull(input, nameof(input));
        var implementation = input.ImplementationType;
        if (input.Lifetime is not ("Singleton" or "Scoped" or "Transient"))
        {
            throw new GeneratorDiagnosticException("NWDI006", $"Unsupported generated service lifetime '{input.Lifetime}'.");
        }

        if (implementation.IsAbstract || implementation.TypeKind == TypeKind.Interface)
        {
            throw new GeneratorDiagnosticException("NWDI001", $"Implementation '{implementation.ToDisplayString(TypeFormat)}' is not activatable.");
        }

        var constructors = implementation.InstanceConstructors
            .Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public)
            .ToArray();
        if (constructors.Length == 0)
        {
            throw new GeneratorDiagnosticException("NWDI002", $"Implementation '{implementation.ToDisplayString(TypeFormat)}' has no public constructor.");
        }

        var markedConstructors = constructors.Where(HasActivatorUtilitiesConstructorAttribute).ToArray();
        return new RegistrationSyntaxFacts(input, constructors, markedConstructors);
    }

    private static bool HasActivatorUtilitiesConstructorAttribute(IMethodSymbol constructor) =>
        constructor.GetAttributes().Any(attribute =>
            attribute.AttributeClass!.Name is "ActivatorUtilitiesConstructorAttribute" or "ActivatorUtilitiesConstructor");
}
