using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class ConstructorSelection
{
    internal ConstructorSelection(IReadOnlyList<IMethodSymbol> ordered, bool isPreferred)
    {
        Ordered = ordered;
        IsPreferred = isPreferred;
    }

    internal IReadOnlyList<IMethodSymbol> Ordered { get; }

    internal bool IsPreferred { get; }
}

internal interface IConstructorSelector
{
    ConstructorSelection Select(RegistrationSyntaxFacts facts);
}

internal sealed class ConstructorSelector : IConstructorSelector
{
    public ConstructorSelection Select(RegistrationSyntaxFacts facts)
    {
        Guard.NotNull(facts, nameof(facts));
        if (facts.MarkedConstructors.Count > 1)
        {
            throw new GeneratorDiagnosticException(
                "NWDI003",
                $"Implementation '{facts.Input.ImplementationType.ToDisplayString()}' has multiple ActivatorUtilities constructors.");
        }

        var ordered = facts.PublicConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .ThenBy(BuildSignature, StringComparer.Ordinal)
            .ToArray();
        var preferred = facts.MarkedConstructors.Count == 1;
        var selected = preferred ? facts.MarkedConstructors[0] : ordered[0];
        if (!preferred)
        {
            return new ConstructorSelection(ordered, isPreferred: false);
        }

        var candidates = new List<IMethodSymbol>(ordered.Length) { selected };
        candidates.AddRange(ordered.Where(constructor => !SymbolEqualityComparer.Default.Equals(constructor, selected)));
        return new ConstructorSelection(candidates, isPreferred: true);
    }

    private static string BuildSignature(IMethodSymbol constructor) =>
        string.Join(",", constructor.Parameters.Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
}
