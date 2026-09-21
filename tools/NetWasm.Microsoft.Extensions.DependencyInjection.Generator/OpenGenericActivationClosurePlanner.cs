using System.Globalization;
using Microsoft.CodeAnalysis;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class OpenGenericActivationClosurePlanner : IOpenGenericActivationClosurePlanner
{
    private static readonly SymbolDisplayFormat IdentityFormat = SymbolDisplayFormat.FullyQualifiedFormat;
    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public GeneratedActivationClosurePlan Plan(
        IReadOnlyList<OpenGenericRegistrationInput> templates,
        IReadOnlyList<ClosedGenericRequestInput> requests)
    {
        Guard.NotNull(templates, nameof(templates));
        Guard.NotNull(requests, nameof(requests));

        if (templates.Count == 0 && requests.Count != 0)
        {
            throw new GeneratorDiagnosticException(
                "NWDI011",
                "A constructed service request has no package-owned open-generic template.");
        }

        var resolvedTemplates = templates.Select(ValidateTemplate).ToArray();
        var pending = new Queue<ClosedGenericRequestInput>(requests);
        var visitedRequests = new HashSet<string>(StringComparer.Ordinal);
        var plannedTemplateClosures = new HashSet<string>(StringComparer.Ordinal);
        var activations = new List<GeneratedClosedGenericActivation>();
        var sequences = new Dictionary<string, GeneratedSequenceModel>(StringComparer.Ordinal);
        while (pending.Count != 0)
        {
            var request = pending.Dequeue();
            Guard.NotNull(request, nameof(requests));
            ValidateRequest(request);
            if (!visitedRequests.Add(RequestIdentity(request)))
            {
                continue;
            }

            var serviceDefinition = request.ServiceType.OriginalDefinition;
            var matches = resolvedTemplates
                .Where(template =>
                    SymbolEqualityComparer.Default.Equals(template.ServiceDefinition, serviceDefinition) &&
                    OpenGenericServiceKeyPolicy.Matches(template.Registration.ServiceKeyExpression, request.ServiceKeyExpression))
                .ToArray();
            if (matches.Length == 0)
            {
                throw new GeneratorDiagnosticException(
                    "NWDI011",
                    $"No open-generic template is registered for '{serviceDefinition.ToDisplayString(IdentityFormat)}'.");
            }

            var typeArguments = request.ServiceType.TypeArguments.ToArray();
            ValidateTypeArguments(serviceDefinition, typeArguments);
            foreach (var template in matches)
            {
                if (template.ImplementationDefinition.Arity != typeArguments.Length)
                {
                    throw new GeneratorDiagnosticException(
                        "NWDI013",
                        $"Implementation '{template.ImplementationDefinition.ToDisplayString(IdentityFormat)}' does not match the service request arity.");
                }

                ValidateTypeArguments(template.ImplementationDefinition, typeArguments);
                var closedImplementation = Construct(template.ImplementationDefinition, typeArguments, "implementation template");
                var registrationKey = template.Registration.ServiceKeyExpression;
                var templateClosureIdentity = template.Index.ToString(CultureInfo.InvariantCulture) + "|" +
                    request.ServiceType.ToDisplayString(IdentityFormat) + "|" + registrationKey;
                if (!plannedTemplateClosures.Add(templateClosureIdentity))
                {
                    continue;
                }

                var activation = new GeneratedClosedGenericActivation(
                    BuildIdentity(request.ServiceType, closedImplementation, registrationKey, template.Registration.Identity),
                    request.ServiceType,
                    closedImplementation,
                    template.Registration.Lifetime,
                    registrationKey,
                    template.ServiceDefinition,
                    template.ImplementationDefinition,
                    registrationKey,
                    BuildFactoryInvocation(
                        template.Registration.FactoryTypeName,
                        template.ImplementationDefinition,
                        typeArguments));
                activations.Add(activation);
                AddConstructorClosure(activation, resolvedTemplates, pending, sequences);
            }
        }

        return new GeneratedActivationClosurePlan(
            activations.AsReadOnly(),
            sequences.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray());
    }

    private static ResolvedTemplate ValidateTemplate(OpenGenericRegistrationInput registration, int index)
    {
        Guard.NotNull(registration, nameof(registration));
        var service = registration.ServiceType.OriginalDefinition;
        var implementation = registration.ImplementationType.OriginalDefinition;
        EnsureGenericDefinition(service, service.ToDisplayString(IdentityFormat), "NWDI014");
        EnsureGenericDefinition(implementation, implementation.ToDisplayString(IdentityFormat), "NWDI014");
        if (service.Arity != implementation.Arity)
        {
            throw new GeneratorDiagnosticException(
                "NWDI013",
                $"Open-generic service '{service.ToDisplayString(IdentityFormat)}' and implementation '{implementation.ToDisplayString(IdentityFormat)}' have different arities.");
        }

        return new ResolvedTemplate(index, registration, service, implementation);
    }

    private static void ValidateRequest(ClosedGenericRequestInput request)
    {
        if (!request.ServiceType.IsGenericType ||
            request.ServiceType.IsUnboundGenericType ||
            GenericTypeShape.ContainsTypeParameter(request.ServiceType))
        {
            throw new GeneratorDiagnosticException(
                "NWDI015",
                $"Closed-generic service request '{request.ServiceType.ToDisplayString(IdentityFormat)}' is not fully constructed.");
        }
    }

    private static void AddConstructorClosure(
        GeneratedClosedGenericActivation activation,
        IReadOnlyList<ResolvedTemplate> templates,
        Queue<ClosedGenericRequestInput> pending,
        Dictionary<string, GeneratedSequenceModel> sequences)
    {
        foreach (var constructor in activation.ImplementationType.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            foreach (var parameter in constructor.Parameters)
            {
                var requestKey = ReadParameterKey(parameter, activation.ServiceKeyExpression);
                var requestedType = parameter.Type;
                if (TryGetSequenceElement(requestedType, out var elementType))
                {
                    var sequenceDisplay = requestedType.ToDisplayString(TypeFormat);
                    var sequenceIdentity = sequenceDisplay + "|" + requestKey;
                    sequences[sequenceIdentity] = new GeneratedSequenceModel(
                        sequenceDisplay,
                        elementType.ToDisplayString(TypeFormat),
                        requestKey);
                    requestedType = elementType;
                }

                if (requestedType is INamedTypeSymbol named &&
                    named.IsGenericType &&
                    !named.IsUnboundGenericType &&
                    !GenericTypeShape.ContainsTypeParameter(named) &&
                    templates.Any(template =>
                        SymbolEqualityComparer.Default.Equals(template.ServiceDefinition, named.OriginalDefinition) &&
                        OpenGenericServiceKeyPolicy.Matches(template.Registration.ServiceKeyExpression, requestKey)))
                {
                    pending.Enqueue(new ClosedGenericRequestInput(named, requestKey));
                }
            }
        }
    }

    private static bool TryGetSequenceElement(ITypeSymbol type, out ITypeSymbol elementType)
    {
        if (type is INamedTypeSymbol named &&
            named.TypeArguments.Length == 1 &&
            named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
        {
            elementType = named.TypeArguments[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static string? ReadParameterKey(IParameterSymbol parameter, string? inheritedKey)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(candidate =>
            candidate.AttributeClass?.Name is "FromKeyedServicesAttribute" or "FromKeyedServices");
        if (attribute is null)
        {
            return null;
        }

        return attribute.ConstructorArguments.Length == 0
            ? inheritedKey
            : FormatKeyConstant(attribute.ConstructorArguments[0]);
    }

    private static string? FormatKeyConstant(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        if (constant.Kind == TypedConstantKind.Enum && constant.Type is INamedTypeSymbol enumType)
        {
            return "(" + enumType.ToDisplayString(TypeFormat) + ")" +
                Convert.ToInt64(constant.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }

        return constant.Type?.SpecialType switch
        {
            SpecialType.System_String => global::Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((string)constant.Value!, quote: true),
            SpecialType.System_Char => global::Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((char)constant.Value!, quote: true),
            SpecialType.System_Boolean => (bool)constant.Value! ? "true" : "false",
            SpecialType.System_SByte => "(sbyte)" + Convert.ToString(constant.Value, CultureInfo.InvariantCulture),
            SpecialType.System_Byte => "(byte)" + Convert.ToString(constant.Value, CultureInfo.InvariantCulture),
            SpecialType.System_Int16 => "(short)" + Convert.ToString(constant.Value, CultureInfo.InvariantCulture),
            SpecialType.System_UInt16 => "(ushort)" + Convert.ToString(constant.Value, CultureInfo.InvariantCulture),
            SpecialType.System_Int32 => Convert.ToString(constant.Value, CultureInfo.InvariantCulture),
            SpecialType.System_UInt32 => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) + "U",
            SpecialType.System_Int64 => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) + "L",
            SpecialType.System_UInt64 => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) + "UL",
            _ => throw new GeneratorDiagnosticException("NWDI007", "Generated keyed-service attributes require a supported constant key."),
        };
    }

    private static string RequestIdentity(ClosedGenericRequestInput request) =>
        request.ServiceType.ToDisplayString(IdentityFormat) + "|" + request.ServiceKeyExpression;

    private static void EnsureGenericDefinition(INamedTypeSymbol symbol, string metadataName, string diagnosticCode)
    {
        if (!symbol.IsGenericType || symbol.TypeParameters.Length == 0)
        {
            throw new GeneratorDiagnosticException(diagnosticCode, $"Type '{metadataName}' must be an open-generic definition.");
        }
    }

    private static void ValidateTypeArguments(INamedTypeSymbol definition, ITypeSymbol[] arguments)
    {
        for (var index = 0; index < definition.TypeParameters.Length; index++)
        {
            var parameter = definition.TypeParameters[index];
            var argument = arguments[index];
            if (parameter.HasReferenceTypeConstraint && IsValueType(argument))
            {
                throw new GeneratorDiagnosticException("NWDI016", $"Type argument {index} violates the reference-type constraint.");
            }

            if (parameter.HasValueTypeConstraint && !IsValueType(argument))
            {
                throw new GeneratorDiagnosticException("NWDI016", $"Type argument {index} violates the value-type constraint.");
            }

            if (parameter.HasConstructorConstraint && !HasPublicParameterlessConstructor(argument))
            {
                throw new GeneratorDiagnosticException("NWDI016", $"Type argument {index} violates the public-constructor constraint.");
            }
        }
    }

    private static bool HasPublicParameterlessConstructor(ITypeSymbol argument)
    {
        if (IsValueType(argument))
        {
            return true;
        }

        return argument is INamedTypeSymbol named &&
            named.InstanceConstructors.Any(constructor =>
                constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.Length == 0);
    }

    private static bool IsValueType(ITypeSymbol type) =>
        type.IsValueType || type.TypeKind is TypeKind.Struct or TypeKind.Enum;

    private static INamedTypeSymbol Construct(
        INamedTypeSymbol definition,
        IReadOnlyList<ITypeSymbol> arguments,
        string role)
    {
        try
        {
            return definition.Construct(arguments.ToArray());
        }
        catch (ArgumentException exception)
        {
            throw new GeneratorDiagnosticException("NWDI016", $"The {role} does not satisfy its generic constraints.", exception);
        }
    }

    private static string BuildIdentity(
        INamedTypeSymbol service,
        INamedTypeSymbol implementation,
        string? serviceKeyExpression,
        string? templateIdentity)
    {
        var identity = templateIdentity ??
            service.ToDisplayString(IdentityFormat) + "->" + implementation.ToDisplayString(IdentityFormat);
        return serviceKeyExpression is null ? identity : identity + "[key=" + serviceKeyExpression + "]";
    }

    private static string? BuildFactoryInvocation(
        string? factoryTypeName,
        INamedTypeSymbol implementationDefinition,
        IReadOnlyList<ITypeSymbol> typeArguments)
    {
        if (factoryTypeName is null)
        {
            return null;
        }

        if (implementationDefinition.InstanceConstructors
            .SelectMany(constructor => constructor.Parameters)
            .Any(parameter => parameter.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == "Microsoft.Extensions.Logging.LoggerCategoryNameAttribute")))
        {
            // The category literal depends on the closed type argument, so emit
            // this activation in the consuming compilation instead of reusing
            // the package's open-template factory.
            return null;
        }

        return factoryTypeName + "<" +
            string.Join(", ", typeArguments.Select(argument => argument.ToDisplayString(TypeFormat))) +
            ">.Create()";
    }

    private sealed record ResolvedTemplate(
        int Index,
        OpenGenericRegistrationInput Registration,
        INamedTypeSymbol ServiceDefinition,
        INamedTypeSymbol ImplementationDefinition);
}

internal static class OpenGenericServiceKeyPolicy
{
    private const string AnyKeyExpression =
        "global::Microsoft.Extensions.DependencyInjection.KeyedService.AnyKey";

    internal static bool Matches(string? templateKey, string? requestKey)
    {
        if (requestKey == AnyKeyExpression)
        {
            return templateKey is not null;
        }

        return string.Equals(templateKey, requestKey, StringComparison.Ordinal) ||
            (templateKey == AnyKeyExpression && requestKey is not null);
    }
}

internal sealed class ActivationClosureValidator : IActivationClosureValidator
{
    public void Validate(IReadOnlyList<GeneratedClosedGenericActivation> activations)
    {
        Guard.NotNull(activations, nameof(activations));
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var activation in activations)
        {
            Guard.NotNull(activation, nameof(activation));
            if (!identities.Add(activation.Identity))
            {
                throw new GeneratorDiagnosticException("NWDI017", $"Duplicate generated activation identity '{activation.Identity}'.");
            }

            if (GenericTypeShape.ContainsTypeParameter(activation.ServiceType) ||
                GenericTypeShape.ContainsTypeParameter(activation.ImplementationType))
            {
                throw new GeneratorDiagnosticException("NWDI015", "The generated activation closure contains an open type.");
            }
        }
    }
}

internal static class GenericTypeShape
{
    internal static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        return type switch
        {
            INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
            IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
            IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
            _ => false,
        };
    }
}
