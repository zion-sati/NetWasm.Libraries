using Microsoft.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class GeneratedParameterModel
{
    internal GeneratedParameterModel(
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

internal sealed class GeneratedConstructorModel
{
    internal GeneratedConstructorModel(string identity, IReadOnlyList<GeneratedParameterModel> parameters, bool isPreferred)
    {
        Identity = identity;
        Parameters = parameters;
        IsPreferred = isPreferred;
    }

    internal string Identity { get; }
    internal IReadOnlyList<GeneratedParameterModel> Parameters { get; }
    internal bool IsPreferred { get; }
}

internal sealed class RegistrationFacts
{
    internal RegistrationFacts(
        string identity,
        string namespaceName,
        string typeName,
        string serviceTypeDisplay,
        string implementationTypeDisplay,
        string lifetime,
        IReadOnlyList<GeneratedParameterModel> parameters,
        IReadOnlyList<GeneratedConstructorModel>? constructors = null,
        IReadOnlyList<string>? assignableTypeDisplays = null,
        string? serviceKeyExpression = null,
        string? templateServiceTypeDisplay = null,
        string? templateImplementationTypeDisplay = null,
        string? templateServiceKeyExpression = null,
        bool isOpenGenericTemplate = false,
        string? genericTypeParameterList = null,
        IReadOnlyList<string>? genericConstraintClauses = null,
        string? factoryInvocationExpression = null)
    {
        Identity = identity;
        NamespaceName = namespaceName;
        TypeName = typeName;
        ServiceTypeDisplay = serviceTypeDisplay;
        ImplementationTypeDisplay = implementationTypeDisplay;
        Lifetime = lifetime;
        Parameters = parameters;
        Constructors = constructors ?? new[] { new GeneratedConstructorModel(identity, parameters, isPreferred: true) };
        AssignableTypeDisplays = assignableTypeDisplays ?? Array.Empty<string>();
        ServiceKeyExpression = serviceKeyExpression;
        TemplateServiceTypeDisplay = templateServiceTypeDisplay;
        TemplateImplementationTypeDisplay = templateImplementationTypeDisplay;
        TemplateServiceKeyExpression = templateServiceKeyExpression;
        IsOpenGenericTemplate = isOpenGenericTemplate;
        GenericTypeParameterList = genericTypeParameterList;
        GenericConstraintClauses = genericConstraintClauses ?? Array.Empty<string>();
        FactoryInvocationExpression = factoryInvocationExpression;
    }

    internal string Identity { get; }
    internal string NamespaceName { get; }
    internal string TypeName { get; }
    internal string ServiceTypeDisplay { get; }
    internal string ImplementationTypeDisplay { get; }
    internal string Lifetime { get; }
    internal IReadOnlyList<GeneratedParameterModel> Parameters { get; }
    internal IReadOnlyList<GeneratedConstructorModel> Constructors { get; }
    internal IReadOnlyList<string> AssignableTypeDisplays { get; }
    internal string? ServiceKeyExpression { get; }
    internal string? TemplateServiceTypeDisplay { get; }
    internal string? TemplateImplementationTypeDisplay { get; }
    internal string? TemplateServiceKeyExpression { get; }
    internal bool IsOpenGenericTemplate { get; }
    internal string? GenericTypeParameterList { get; }
    internal IReadOnlyList<string> GenericConstraintClauses { get; }
    internal string? FactoryInvocationExpression { get; }
}

internal sealed class ActivationModel
{
    internal ActivationModel(RegistrationFacts facts)
    {
        Facts = facts;
    }

    internal RegistrationFacts Facts { get; }
}

internal interface IActivationModelBuilder
{
    ActivationModel Build(RegistrationSyntaxFacts facts);
}

internal sealed class ActivationModelBuilder : IActivationModelBuilder
{
    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private readonly IConstructorSelector _constructorSelector;
    private readonly IOptionalDefaultExpressionFormatter _formatter;

    internal ActivationModelBuilder(IConstructorSelector constructorSelector, IOptionalDefaultExpressionFormatter formatter)
    {
        _constructorSelector = constructorSelector ?? throw new ArgumentNullException(nameof(constructorSelector));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    public ActivationModel Build(RegistrationSyntaxFacts facts)
    {
        Guard.NotNull(facts, nameof(facts));
        var input = facts.Input;
        var selection = _constructorSelector.Select(facts);
        var constructors = selection.Ordered
            .Select((constructor, index) => CreateConstructorModel(input, input.ImplementationType, constructor, selection.IsPreferred && index == 0))
            .ToArray();
        var identity = input.Identity ?? constructors[0].Identity;
        if (input.Identity is not null)
        {
            constructors[0] = new GeneratedConstructorModel(identity, constructors[0].Parameters, constructors[0].IsPreferred);
        }

        var implementation = input.ImplementationType;
        var serviceType = input.IsOpenGenericTemplate
            ? input.ServiceType.OriginalDefinition.Construct(implementation.TypeParameters.Cast<ITypeSymbol>().ToArray())
            : input.ServiceType;
        var namespaceName = implementation.ContainingNamespace.IsGlobalNamespace ? "Generated" : implementation.ContainingNamespace.ToDisplayString();
        var parameters = constructors[0].Parameters;
        return new ActivationModel(new RegistrationFacts(
            identity,
            namespaceName,
            CreateTypeName(implementation, identity),
            serviceType.ToDisplayString(TypeFormat),
            implementation.ToDisplayString(TypeFormat),
            input.Lifetime,
            parameters,
            constructors,
            GetAssignableTypeDisplays(implementation),
            input.ServiceKeyExpression,
            input.TemplateServiceType?.ConstructUnboundGenericType().ToDisplayString(TypeFormat),
            input.TemplateImplementationType?.ConstructUnboundGenericType().ToDisplayString(TypeFormat),
            input.TemplateServiceKeyExpression,
            input.IsOpenGenericTemplate,
            input.IsOpenGenericTemplate ? "<" + string.Join(", ", implementation.TypeParameters.Select(parameter => parameter.Name)) + ">" : null,
            input.IsOpenGenericTemplate ? BuildConstraintClauses(implementation.TypeParameters) : null,
            input.FactoryInvocationExpression));
    }

    private GeneratedConstructorModel CreateConstructorModel(RegistrationSyntaxInput input, INamedTypeSymbol implementation, IMethodSymbol constructor, bool isPreferred)
    {
        var parameters = constructor.Parameters.Select(parameter => new GeneratedParameterModel(
            parameter.Type.ToDisplayString(TypeFormat),
            parameter.HasExplicitDefaultValue,
            parameter.HasExplicitDefaultValue ? _formatter.Format(parameter.Type, parameter.ExplicitDefaultValue) : null,
            GetServiceKeyExpression(parameter),
            GetServiceKeyLookupMode(parameter),
            GetConstantExpression(parameter, implementation))).ToArray();
        return new GeneratedConstructorModel(
            BuildIdentity(input, implementation, constructor),
            parameters,
            isPreferred);
    }

    private static string GetServiceKeyLookupMode(IParameterSymbol parameter)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(IsFromKeyedServicesAttribute);
        if (attribute is null)
        {
            return "NullKey";
        }

        return attribute.ConstructorArguments.Length == 0 ? "InheritKey" : "ExplicitKey";
    }

    private static string? GetServiceKeyExpression(IParameterSymbol parameter)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(IsFromKeyedServicesAttribute);
        if (attribute is null || attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var argument = attribute.ConstructorArguments[0];
        return FormatKeyConstant(argument);
    }

    private static bool IsFromKeyedServicesAttribute(AttributeData attribute) =>
        attribute.AttributeClass!.Name is "FromKeyedServicesAttribute" or "FromKeyedServices";

    private static string? GetConstantExpression(IParameterSymbol parameter, INamedTypeSymbol implementation)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(candidate =>
            candidate.AttributeClass?.ToDisplayString() == "Microsoft.Extensions.Logging.LoggerCategoryNameAttribute");
        if (attribute is null)
        {
            return null;
        }

        if (parameter.Type.SpecialType != SpecialType.System_String || implementation.TypeArguments.Length != 1)
        {
            throw new GeneratorDiagnosticException(
                "NWDI019",
                "Logger category injection requires a string parameter on a single-argument generic implementation.");
        }

        var categoryNameFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.None);
        var categoryName = implementation.TypeArguments[0].ToDisplayString(categoryNameFormat);
        return global::Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(categoryName, quote: true);
    }

    private static string FormatKeyConstant(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        if (constant.Kind == TypedConstantKind.Enum && constant.Type is INamedTypeSymbol enumType)
        {
            var value = Convert.ToInt64(constant.Value, System.Globalization.CultureInfo.InvariantCulture);
            return "(" + enumType.ToDisplayString(TypeFormat) + ")" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var type = constant.Type!;
        return type.SpecialType switch
        {
            SpecialType.System_String => global::Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((string)constant.Value!, quote: true),
            SpecialType.System_Char => global::Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((char)constant.Value!, quote: true),
            SpecialType.System_Boolean => (bool)constant.Value! ? "true" : "false",
            SpecialType.System_SByte => "(sbyte)" + Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_Byte => "(byte)" + Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_Int16 => "(short)" + Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_UInt16 => "(ushort)" + Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_Int32 => Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture)!,
            SpecialType.System_UInt32 => Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture) + "U",
            SpecialType.System_Int64 => Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture) + "L",
            SpecialType.System_UInt64 => Convert.ToString(constant.Value, System.Globalization.CultureInfo.InvariantCulture) + "UL",
            _ => throw new GeneratorDiagnosticException("NWDI007", "Generated keyed-service attributes require a supported constant key."),
        };
    }

    private static string BuildIdentity(
        RegistrationSyntaxInput input,
        INamedTypeSymbol implementationType,
        IMethodSymbol constructor)
    {
        var service = input.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementation = implementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var parameters = string.Join(",", constructor.Parameters.Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        var serviceKey = input.ServiceKeyExpression is null
            ? "unkeyed"
            : "key:" + input.ServiceKeyExpression;
        return $"{service}->{implementation}({parameters})[{serviceKey}]";
    }

    private static string CreateTypeName(INamedTypeSymbol implementation, string identity)
    {
        using var sha256 = SHA256.Create();
        var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
        var suffix = string.Concat(digest.Take(16).Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
        return implementation.Name + "GeneratedActivation_" + suffix;
    }

    private static string[] GetAssignableTypeDisplays(INamedTypeSymbol implementation)
    {
        var displays = new List<string> { implementation.ToDisplayString(TypeFormat) };
        for (var baseType = implementation.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.SpecialType != SpecialType.System_Object)
            {
                displays.Add(baseType.ToDisplayString(TypeFormat));
            }
        }

        displays.AddRange(implementation.AllInterfaces.Select(@interface => @interface.ToDisplayString(TypeFormat)));
        return displays.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string[] BuildConstraintClauses(IReadOnlyList<ITypeParameterSymbol> parameters)
    {
        var clauses = new List<string>();
        foreach (var parameter in parameters)
        {
            var constraints = new List<string>();
            if (parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
            }
            else if (parameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(parameter.ConstraintTypes.Select(type => type.ToDisplayString(TypeFormat)));
            if (parameter.HasConstructorConstraint && !parameter.HasValueTypeConstraint && !parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count != 0)
            {
                clauses.Add("where " + parameter.Name + " : " + string.Join(", ", constraints));
            }
        }

        return clauses.ToArray();
    }
}
