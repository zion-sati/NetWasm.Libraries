using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed record GeneratedApplicationInputs(
    IReadOnlyList<RegistrationSyntaxInput> Registrations,
    IReadOnlyList<GeneratedSequenceModel> Sequences,
    IReadOnlyList<OpenGenericRegistrationInput> OpenGenericRegistrations,
    IReadOnlyList<ClosedGenericRequestInput> ClosedGenericRequests);

internal interface IRegistrationInvocationReader
{
    GeneratedApplicationInputs Read(
        Compilation compilation,
        ImmutableArray<InvocationExpressionSyntax> invocations);
}

/// <summary>Reads ordinary MEDI calls into the existing generation pipeline's semantic inputs.</summary>
internal sealed class RegistrationInvocationReader : IRegistrationInvocationReader
{
    private const string ServiceCollectionExtensions =
        "Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";
    private const string ServiceCollectionDescriptorExtensions =
        "Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions";
    private const string ServiceProviderExtensions =
        "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions";
    private const string ServiceProviderKeyedExtensions =
        "Microsoft.Extensions.DependencyInjection.ServiceProviderKeyedServiceExtensions";
    private const string ServiceProviderContract = "System.IServiceProvider";
    private const string KeyedServiceProviderContract =
        "Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider";

    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public GeneratedApplicationInputs Read(
        Compilation compilation,
        ImmutableArray<InvocationExpressionSyntax> invocations)
    {
        Guard.NotNull(compilation, nameof(compilation));
        var registrations = new Dictionary<string, RegistrationSyntaxInput>(StringComparer.Ordinal);
        var sequences = new Dictionary<string, GeneratedSequenceModel>(StringComparer.Ordinal);
        var openGenericRegistrations = new Dictionary<string, OpenGenericRegistrationInput>(StringComparer.Ordinal);
        var closedGenericRequests = new Dictionary<string, ClosedGenericRequestInput>(StringComparer.Ordinal);
        foreach (var invocationSyntax in invocations)
        {
            var semanticModel = compilation.GetSemanticModel(invocationSyntax.SyntaxTree);
            if (semanticModel.GetOperation(invocationSyntax) is not IInvocationOperation invocation)
            {
                continue;
            }

            ReadServiceRequest(invocation, sequences, closedGenericRequests);
            if (!TryReadRegistration(invocation, out var registration, out var openGenericRegistration))
            {
                continue;
            }

            if (openGenericRegistration is not null)
            {
                openGenericRegistrations[OpenRegistrationKey(openGenericRegistration)] = openGenericRegistration;
                continue;
            }

            var key = RegistrationKey(registration);
            registrations[key] = registration;
            AddConstructorRequests(registration, sequences, closedGenericRequests);
        }

        return new GeneratedApplicationInputs(
            registrations.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(),
            sequences.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(),
            openGenericRegistrations.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(),
            closedGenericRequests.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray());
    }

    private static bool TryReadRegistration(
        IInvocationOperation invocation,
        out RegistrationSyntaxInput registration,
        out OpenGenericRegistrationInput? openGenericRegistration)
    {
        registration = null!;
        openGenericRegistration = null;
        var target = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        var containingType = target.ContainingType.ToDisplayString();
        if (containingType is not (ServiceCollectionExtensions or ServiceCollectionDescriptorExtensions) ||
            !TryGetLifetime(target.Name, out var lifetime))
        {
            return false;
        }

        if (target.Parameters.Any(parameter =>
            parameter.Name.IndexOf("Factory", StringComparison.Ordinal) >= 0 ||
            parameter.Name.IndexOf("Instance", StringComparison.Ordinal) >= 0))
        {
            return false;
        }

        INamedTypeSymbol serviceType;
        INamedTypeSymbol implementationType;
        var typeArguments = invocation.TargetMethod.TypeArguments;
        if (typeArguments.Length == 2)
        {
            serviceType = RequireNamedType(typeArguments[0], invocation.Syntax);
            implementationType = RequireNamedType(typeArguments[1], invocation.Syntax);
        }
        else if (typeArguments.Length == 1)
        {
            serviceType = RequireNamedType(typeArguments[0], invocation.Syntax);
            implementationType = serviceType;
        }
        else
        {
            serviceType = ReadTypeArgument(invocation, "serviceType", "service");
            implementationType = TryReadTypeArgument(invocation, "implementationType", out var implementation)
                ? implementation
                : serviceType;
        }

        var serviceKeyExpression = target.Name.IndexOf("Keyed", StringComparison.Ordinal) >= 0
            ? ReadServiceKeyExpression(invocation)
            : null;
        if (serviceType.IsUnboundGenericType || implementationType.IsUnboundGenericType)
        {
            if (!serviceType.IsUnboundGenericType || !implementationType.IsUnboundGenericType)
            {
                throw new GeneratorDiagnosticException(
                    "NWDI013",
                    "Open-generic registrations require both service and implementation to be generic definitions.");
            }

            openGenericRegistration = new OpenGenericRegistrationInput(
                serviceType.OriginalDefinition,
                implementationType.OriginalDefinition,
                lifetime,
                ServiceKeyExpression: serviceKeyExpression);
            return true;
        }

        registration = new RegistrationSyntaxInput(
            serviceType,
            implementationType,
            Identity: null,
            lifetime,
            serviceKeyExpression);
        return true;
    }

    private static bool TryGetLifetime(string methodName, out string lifetime)
    {
        if (methodName.EndsWith("Singleton", StringComparison.Ordinal))
        {
            lifetime = "Singleton";
            return true;
        }

        if (methodName.EndsWith("Scoped", StringComparison.Ordinal))
        {
            lifetime = "Scoped";
            return true;
        }

        if (methodName.EndsWith("Transient", StringComparison.Ordinal))
        {
            lifetime = "Transient";
            return true;
        }

        lifetime = string.Empty;
        return false;
    }

    private static INamedTypeSymbol ReadTypeArgument(
        IInvocationOperation invocation,
        params string[] parameterNames)
    {
        if (TryReadTypeArgument(invocation, parameterNames, out var type))
        {
            return type;
        }

        throw new GeneratorDiagnosticException(
            "NWDI010",
            "Implementation-type DI registrations require a statically known typeof(...) service and implementation type.");
    }

    private static bool TryReadTypeArgument(
        IInvocationOperation invocation,
        string parameterName,
        out INamedTypeSymbol type) =>
        TryReadTypeArgument(invocation, [parameterName], out type);

    private static bool TryReadTypeArgument(
        IInvocationOperation invocation,
        IReadOnlyList<string> parameterNames,
        out INamedTypeSymbol type)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter is null ||
                !parameterNames.Contains(argument.Parameter.Name, StringComparer.Ordinal))
            {
                continue;
            }

            var value = Unwrap(argument.Value);
            if (value is ITypeOfOperation typeOf)
            {
                type = RequireNamedType(typeOf.TypeOperand, argument.Syntax);
                return true;
            }
        }

        type = null!;
        return false;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static INamedTypeSymbol RequireNamedType(ITypeSymbol type, SyntaxNode syntax) =>
        type as INamedTypeSymbol ?? throw new GeneratorDiagnosticException(
            "NWDI010",
            $"DI registration type '{type.ToDisplayString()}' at '{syntax.GetLocation().GetLineSpan()}' is not a closed named type.");

    private static string? ReadServiceKeyExpression(IInvocationOperation invocation)
    {
        var argument = invocation.Arguments.FirstOrDefault(candidate => candidate.Parameter?.Name == "serviceKey")
            ?? throw new GeneratorDiagnosticException("NWDI007", "A keyed DI registration is missing its service key.");
        var value = Unwrap(argument.Value);
        if (value is IPropertyReferenceOperation property &&
            property.Property.Name == "AnyKey" &&
            property.Property.ContainingType.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.KeyedService")
        {
            return "global::Microsoft.Extensions.DependencyInjection.KeyedService.AnyKey";
        }

        if (!value.ConstantValue.HasValue)
        {
            throw new GeneratorDiagnosticException("NWDI007", "Generated keyed registrations require a compile-time constant service key.");
        }

        return FormatConstant(value.Type, value.ConstantValue.Value);
    }

    private static string FormatConstant(ITypeSymbol? type, object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (type?.TypeKind == TypeKind.Enum)
        {
            return "(" + type.ToDisplayString(TypeFormat) + ")" +
                Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        return value switch
        {
            string text => SymbolDisplay.FormatLiteral(text, quote: true),
            char character => SymbolDisplay.FormatLiteral(character, quote: true),
            bool boolean => boolean ? "true" : "false",
            sbyte number => "(sbyte)" + number.ToString(CultureInfo.InvariantCulture),
            byte number => "(byte)" + number.ToString(CultureInfo.InvariantCulture),
            short number => "(short)" + number.ToString(CultureInfo.InvariantCulture),
            ushort number => "(ushort)" + number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            uint number => number.ToString(CultureInfo.InvariantCulture) + "U",
            long number => number.ToString(CultureInfo.InvariantCulture) + "L",
            ulong number => number.ToString(CultureInfo.InvariantCulture) + "UL",
            float number => number.ToString("R", CultureInfo.InvariantCulture) + "F",
            double number => number.ToString("R", CultureInfo.InvariantCulture) + "D",
            decimal number => number.ToString(CultureInfo.InvariantCulture) + "M",
            _ => throw new GeneratorDiagnosticException("NWDI007", "Generated keyed registrations require a supported constant service key."),
        };
    }

    private static void AddConstructorRequests(
        RegistrationSyntaxInput registration,
        IDictionary<string, GeneratedSequenceModel> sequences,
        IDictionary<string, ClosedGenericRequestInput> requests)
    {
        foreach (var constructor in registration.ImplementationType.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            foreach (var parameter in constructor.Parameters)
            {
                var serviceKeyExpression = ReadParameterServiceKeyExpression(
                    parameter,
                    registration.ServiceKeyExpression);
                AddServiceRequest(parameter.Type, serviceKeyExpression, sequences, requests);
            }
        }
    }

    private static void ReadServiceRequest(
        IInvocationOperation invocation,
        IDictionary<string, GeneratedSequenceModel> sequences,
        IDictionary<string, ClosedGenericRequestInput> requests)
    {
        var target = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        if (!IsServiceRequestMethod(target))
        {
            return;
        }

        INamedTypeSymbol requestedType;
        if (invocation.TargetMethod.TypeArguments.Length == 1)
        {
            requestedType = RequireNamedType(invocation.TargetMethod.TypeArguments[0], invocation.Syntax);
        }
        else
        {
            requestedType = ReadTypeArgument(invocation, "serviceType");
        }

        var serviceKeyExpression = target.Name.IndexOf("Keyed", StringComparison.Ordinal) >= 0
            ? ReadServiceKeyExpression(invocation)
            : null;
        if (target.Name is "GetServices" or "GetKeyedServices")
        {
            var enumerable = invocation.SemanticModel?.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            if (enumerable is not null)
            {
                AddServiceRequest(enumerable.Construct(requestedType), serviceKeyExpression, sequences, requests);
            }

            return;
        }

        AddServiceRequest(requestedType, serviceKeyExpression, sequences, requests);
    }

    private static bool IsServiceRequestMethod(IMethodSymbol target)
    {
        if (target.Name is not ("GetService" or "GetRequiredService" or "GetServices" or
            "GetKeyedService" or "GetRequiredKeyedService" or "GetKeyedServices"))
        {
            return false;
        }

        var containingType = target.ContainingType.ToDisplayString();
        if (containingType is ServiceProviderExtensions or ServiceProviderKeyedExtensions)
        {
            return true;
        }

        return containingType is ServiceProviderContract or KeyedServiceProviderContract ||
            target.ContainingType.AllInterfaces.Any(@interface =>
                @interface.ToDisplayString() is ServiceProviderContract or KeyedServiceProviderContract);
    }

    private static void AddServiceRequest(
        ITypeSymbol candidate,
        string? serviceKeyExpression,
        IDictionary<string, GeneratedSequenceModel> sequences,
        IDictionary<string, ClosedGenericRequestInput> requests)
    {
        if (candidate is not INamedTypeSymbol named)
        {
            return;
        }

        if (named.TypeArguments.Length == 1 &&
            named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
        {
            var sequenceDisplay = named.ToDisplayString(TypeFormat);
            sequences[sequenceDisplay + "|" + serviceKeyExpression] = new GeneratedSequenceModel(
                sequenceDisplay,
                named.TypeArguments[0].ToDisplayString(TypeFormat),
                serviceKeyExpression);
            if (named.TypeArguments[0] is not INamedTypeSymbol elementType)
            {
                return;
            }

            named = elementType;
        }

        if (!named.IsGenericType || named.IsUnboundGenericType || GenericTypeShape.ContainsTypeParameter(named))
        {
            return;
        }

        var request = new ClosedGenericRequestInput(named, serviceKeyExpression);
        requests[ClosedRequestKey(request)] = request;
    }

    private static string? ReadParameterServiceKeyExpression(
        IParameterSymbol parameter,
        string? inheritedServiceKeyExpression)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(candidate =>
            candidate.AttributeClass?.Name is "FromKeyedServicesAttribute" or "FromKeyedServices");
        if (attribute is null)
        {
            return null;
        }

        if (attribute.ConstructorArguments.Length == 0)
        {
            return inheritedServiceKeyExpression;
        }

        var argument = attribute.ConstructorArguments[0];
        if (argument.IsNull)
        {
            return "null";
        }

        if (argument.Kind == TypedConstantKind.Enum && argument.Type is INamedTypeSymbol enumType)
        {
            return "(" + enumType.ToDisplayString(TypeFormat) + ")" +
                Convert.ToInt64(argument.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }

        return FormatConstant(argument.Type, argument.Value);
    }

    private static string RegistrationKey(RegistrationSyntaxInput registration) =>
        registration.ServiceType.ToDisplayString(TypeFormat) + "|" +
        registration.ImplementationType.ToDisplayString(TypeFormat) + "|" +
        registration.ServiceKeyExpression;

    private static string OpenRegistrationKey(OpenGenericRegistrationInput registration) =>
        registration.ServiceType.ToDisplayString(TypeFormat) + "|" +
        registration.ImplementationType.ToDisplayString(TypeFormat) + "|" +
        registration.ServiceKeyExpression;

    private static string ClosedRequestKey(ClosedGenericRequestInput request) =>
        request.ServiceType.ToDisplayString(TypeFormat) + "|" + request.ServiceKeyExpression;
}
