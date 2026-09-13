using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed record GeneratedApplicationMetadataInputs(
    IReadOnlyList<OpenGenericRegistrationInput> OpenGenericRegistrations,
    IReadOnlyList<ClosedGenericRequestInput> ClosedGenericRequests);

internal interface IGeneratedApplicationMetadataReader
{
    GeneratedApplicationMetadataInputs Read(Compilation compilation);
}

internal interface IGeneratedApplicationMetadataEmitter
{
    string Emit(
        IReadOnlyList<OpenGenericRegistrationInput> templates,
        IReadOnlyList<ClosedGenericRequestInput> requests);
}

internal static class GeneratedApplicationMetadataNames
{
    internal const string OpenGenericRegistrationAttribute =
        "NetWasm.Generated.DependencyInjection.OpenGenericRegistrationAttribute";
    internal const string ClosedGenericRequestAttribute =
        "NetWasm.Generated.DependencyInjection.ClosedGenericRequestAttribute";
}

/// <summary>Reads compile-time DI closure facts published by referenced libraries.</summary>
internal sealed class GeneratedApplicationMetadataReader : IGeneratedApplicationMetadataReader
{
    public GeneratedApplicationMetadataInputs Read(Compilation compilation)
    {
        Guard.NotNull(compilation, nameof(compilation));
        var templates = new Dictionary<string, OpenGenericRegistrationInput>(StringComparer.Ordinal);
        var requests = new Dictionary<string, ClosedGenericRequestInput>(StringComparer.Ordinal);
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            foreach (var attribute in assembly.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName == GeneratedApplicationMetadataNames.OpenGenericRegistrationAttribute)
                {
                    var template = ReadTemplate(attribute);
                    templates[TemplateIdentity(template)] = template;
                }
                else if (attributeName == GeneratedApplicationMetadataNames.ClosedGenericRequestAttribute)
                {
                    var request = ReadRequest(attribute);
                    requests[RequestIdentity(request)] = request;
                }
            }
        }

        return new GeneratedApplicationMetadataInputs(
            templates.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(),
            requests.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray());
    }

    private static OpenGenericRegistrationInput ReadTemplate(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length != 6 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol serviceType ||
            attribute.ConstructorArguments[1].Value is not INamedTypeSymbol implementationType ||
            attribute.ConstructorArguments[2].Value is not INamedTypeSymbol factoryType ||
            attribute.ConstructorArguments[3].Value is not string lifetime)
        {
            throw new GeneratorDiagnosticException("NWDI018", "A referenced open-generic activation template has invalid metadata.");
        }

        return new OpenGenericRegistrationInput(
            serviceType.OriginalDefinition,
            implementationType.OriginalDefinition,
            lifetime,
            ReadOptionalString(attribute.ConstructorArguments[4]),
            ReadOptionalString(attribute.ConstructorArguments[5]),
            GetFactoryTypeName(factoryType));
    }

    private static ClosedGenericRequestInput ReadRequest(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length != 2 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol serviceType)
        {
            throw new GeneratorDiagnosticException("NWDI018", "A referenced closed-generic service request has invalid metadata.");
        }

        return new ClosedGenericRequestInput(serviceType, ReadOptionalString(attribute.ConstructorArguments[1]));
    }

    private static string? ReadOptionalString(TypedConstant value)
    {
        if (value.IsNull)
        {
            return null;
        }

        return value.Value as string ??
            throw new GeneratorDiagnosticException("NWDI018", "A referenced DI closure metadata string has an invalid value.");
    }

    private static string GetFactoryTypeName(INamedTypeSymbol factoryType)
    {
        var typeName = factoryType.MetadataName;
        var aritySeparator = typeName.IndexOf('`');
        if (aritySeparator >= 0)
        {
            typeName = typeName.Substring(0, aritySeparator);
        }

        var namespaceName = factoryType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : factoryType.ContainingNamespace.ToDisplayString() + ".";
        return "global::" + namespaceName + typeName;
    }

    private static string TemplateIdentity(OpenGenericRegistrationInput template) =>
        SymbolIdentity(template.ServiceType) + "|" + SymbolIdentity(template.ImplementationType) + "|" +
        template.ServiceKeyExpression;

    private static string RequestIdentity(ClosedGenericRequestInput request) =>
        SymbolIdentity(request.ServiceType) + "|" + request.ServiceKeyExpression;

    private static string SymbolIdentity(INamedTypeSymbol symbol) =>
        symbol.ContainingAssembly.Identity + "|" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}

/// <summary>Emits immutable assembly metadata for downstream application closure.</summary>
internal sealed class GeneratedApplicationMetadataEmitter : IGeneratedApplicationMetadataEmitter
{
    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public string Emit(
        IReadOnlyList<OpenGenericRegistrationInput> templates,
        IReadOnlyList<ClosedGenericRequestInput> requests)
    {
        Guard.NotNull(templates, nameof(templates));
        Guard.NotNull(requests, nameof(requests));
        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        foreach (var template in templates)
        {
            Guard.NotNull(template, nameof(templates));
            if (template.FactoryTypeName is null)
            {
                throw new GeneratorDiagnosticException("NWDI018", "An open-generic activation template is missing its generated factory identity.");
            }

            builder.Append("[assembly: global::")
                .Append(GeneratedApplicationMetadataNames.OpenGenericRegistrationAttribute)
                .Append("(typeof(")
                .Append(template.ServiceType.ConstructUnboundGenericType().ToDisplayString(TypeFormat))
                .Append("), typeof(")
                .Append(template.ImplementationType.ConstructUnboundGenericType().ToDisplayString(TypeFormat))
                .Append("), typeof(")
                .Append(template.FactoryTypeName)
                .Append(UnboundGenericSuffix(template.ImplementationType.Arity))
                .Append("), ")
                .Append(StringLiteral(template.Lifetime))
                .Append(", ")
                .Append(OptionalStringLiteral(template.Identity))
                .Append(", ")
                .Append(OptionalStringLiteral(template.ServiceKeyExpression))
                .AppendLine(")]");
        }

        foreach (var request in requests)
        {
            Guard.NotNull(request, nameof(requests));
            builder.Append("[assembly: global::")
                .Append(GeneratedApplicationMetadataNames.ClosedGenericRequestAttribute)
                .Append("(typeof(")
                .Append(request.ServiceType.ToDisplayString(TypeFormat))
                .Append("), ")
                .Append(OptionalStringLiteral(request.ServiceKeyExpression))
                .AppendLine(")]");
        }

        builder.AppendLine("namespace NetWasm.Generated.DependencyInjection;");
        builder.AppendLine("[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]");
        builder.AppendLine("internal sealed class OpenGenericRegistrationAttribute : global::System.Attribute");
        builder.AppendLine("{");
        builder.AppendLine("    public OpenGenericRegistrationAttribute(global::System.Type serviceType, global::System.Type implementationType, global::System.Type factoryType, string lifetime, string? identity, string? serviceKeyExpression) { }");
        builder.AppendLine("}");
        builder.AppendLine("[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]");
        builder.AppendLine("internal sealed class ClosedGenericRequestAttribute : global::System.Attribute");
        builder.AppendLine("{");
        builder.AppendLine("    public ClosedGenericRequestAttribute(global::System.Type serviceType, string? serviceKeyExpression) { }");
        builder.AppendLine("}");
        return Guard.NormalizeLineEndings(builder.ToString());
    }

    private static string UnboundGenericSuffix(int arity) =>
        "<" + new string(',', arity - 1) + ">";

    private static string OptionalStringLiteral(string? value) => value is null ? "null" : StringLiteral(value);

    private static string StringLiteral(string value) => SymbolDisplay.FormatLiteral(value, quote: true);
}
