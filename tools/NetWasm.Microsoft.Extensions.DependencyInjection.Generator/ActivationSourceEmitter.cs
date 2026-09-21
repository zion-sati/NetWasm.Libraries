using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class ActivationSourceEmitter : IActivationSourceEmitter
{
    private readonly IGeneratedCallSiteBuilder _callSiteBuilder;

    internal ActivationSourceEmitter(IGeneratedCallSiteBuilder callSiteBuilder)
    {
        _callSiteBuilder = callSiteBuilder ?? throw new ArgumentNullException(nameof(callSiteBuilder));
    }

    public string Emit(ActivationModel model)
    {
        Guard.NotNull(model, nameof(model));
        var callSite = _callSiteBuilder.Build(model);
        var facts = callSite.Activation.Facts;
        var constructors = facts.Constructors;
        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection.Generated;");
        builder.Append("namespace ").Append(facts.NamespaceName).AppendLine(";");
        builder.Append(facts.IsOpenGenericTemplate ? "public" : "internal")
            .Append(" static class ")
            .Append(facts.TypeName)
            .Append(facts.GenericTypeParameterList)
            .AppendLine();
        foreach (var constraint in facts.GenericConstraintClauses)
        {
            builder.Append("    ").AppendLine(constraint);
        }

        builder.AppendLine("{");
        builder.AppendLine("    public static GeneratedActivationDescriptor Create()");
        builder.AppendLine("    {");
        if (facts.FactoryInvocationExpression is not null)
        {
            builder.Append("        return ").Append(facts.FactoryInvocationExpression).AppendLine(";");
        }
        else
        {
            EmitDescriptor(
                builder,
                facts,
                constructors[0],
                callSite.Parameters,
                includeAlternatives: constructors.Count > 1,
                indent: "        ");
        }

        builder.AppendLine("    }");
        if (facts.IsOpenGenericTemplate)
        {
            builder.AppendLine("}");
            return builder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        }

        builder.AppendLine();
        builder.AppendLine("    public static IServiceCollection Register(IServiceCollection services)");
        builder.AppendLine("    {");
        builder.Append("        return services.AddGenerated(Create(), ServiceLifetime.").Append(facts.Lifetime).AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static void EmitDescriptor(
        StringBuilder builder,
        RegistrationFacts facts,
        GeneratedConstructorModel constructor,
        IReadOnlyList<GeneratedCallSiteParameter> callSiteParameters,
        bool includeAlternatives,
        string indent)
    {
        builder.Append(indent).AppendLine("return new GeneratedActivationDescriptor(");
        builder.Append(indent).Append("    ").Append(StringLiteral(constructor.Identity)).AppendLine(",");
        builder.Append(indent).Append("    typeof(").Append(facts.ServiceTypeDisplay).AppendLine("),");
        builder.Append(indent).Append("    typeof(").Append(facts.ImplementationTypeDisplay).AppendLine("),");
        EmitParameters(builder, callSiteParameters, indent + "    ");
        builder.AppendLine(",");
        EmitFactory(builder, callSiteParameters, facts.ImplementationTypeDisplay, indent + "    ");
        builder.AppendLine(",");
        if (includeAlternatives)
        {
            builder.Append(indent).AppendLine("    new GeneratedActivationDescriptor[]");
            builder.Append(indent).AppendLine("    {");
            for (var index = 1; index < facts.Constructors.Count; index++)
            {
                EmitAlternative(builder, facts, facts.Constructors[index], indent + "        ");
                if (index != facts.Constructors.Count - 1)
                {
                    builder.AppendLine(",");
                }
            }

            builder.Append(indent).AppendLine("    }");
        }
        else
        {
            builder.Append(indent).AppendLine("    Array.Empty<GeneratedActivationDescriptor>()");
        }

        builder.Append(indent).Append(", isPreferred: ").Append(constructor.IsPreferred ? "true" : "false").Append(", assignableTypes: ");
        EmitAssignableTypes(builder, facts.AssignableTypeDisplays, string.Empty);
        if (facts.ServiceKeyExpression is not null)
        {
            builder.Append(", serviceKey: ").Append(facts.ServiceKeyExpression);
        }

        builder.Append(", diagnosticServiceType: ").Append(StringLiteral(facts.ServiceTypeDisplay));
        builder.Append(", diagnosticImplementationType: ").Append(StringLiteral(facts.ImplementationTypeDisplay));
        if (facts.TemplateServiceTypeDisplay is not null)
        {
            builder.Append(", templateServiceType: typeof(").Append(facts.TemplateServiceTypeDisplay).Append(')');
            builder.Append(", templateImplementationType: typeof(").Append(facts.TemplateImplementationTypeDisplay).Append(')');
            if (facts.TemplateServiceKeyExpression is not null)
            {
                builder.Append(", templateServiceKey: ").Append(facts.TemplateServiceKeyExpression);
            }
        }

        builder.AppendLine();
        builder.Append(indent).AppendLine(");");
    }

    private static void EmitAlternative(
        StringBuilder builder,
        RegistrationFacts facts,
        GeneratedConstructorModel constructor,
        string indent)
    {
        builder.Append(indent).Append("new GeneratedActivationDescriptor(").AppendLine();
        builder.Append(indent).Append("    ").Append(StringLiteral(constructor.Identity)).AppendLine(",");
        builder.Append(indent).Append("    typeof(").Append(facts.ServiceTypeDisplay).AppendLine("),");
        builder.Append(indent).Append("    typeof(").Append(facts.ImplementationTypeDisplay).AppendLine("),");
        var parameters = constructor.Parameters
            .Select(parameter => new GeneratedCallSiteParameter(
                parameter.TypeDisplay,
                parameter.HasDefaultValue,
                parameter.DefaultExpression,
                parameter.ServiceKeyExpression,
                parameter.LookupMode,
                parameter.ConstantExpression))
            .ToArray();
        EmitParameters(builder, parameters, indent + "    ");
        builder.AppendLine(",");
        EmitFactory(builder, parameters, facts.ImplementationTypeDisplay, indent + "    ");
        builder.Append(indent).Append(')');
    }

    private static void EmitFactory(
        StringBuilder builder,
        IReadOnlyList<GeneratedCallSiteParameter> parameters,
        string implementationTypeDisplay,
        string indent)
    {
        builder.Append(indent).Append("static values => new ").Append(implementationTypeDisplay).Append('(');
        var valueIndex = 0;
        for (var index = 0; index < parameters.Count; index++)
        {
            if (index != 0)
            {
                builder.Append(", ");
            }

            var parameter = parameters[index];
            if (parameter.ConstantExpression is not null)
            {
                builder.Append(parameter.ConstantExpression);
            }
            else
            {
                builder.Append('(').Append(parameter.TypeDisplay).Append(")values[").Append(valueIndex).Append("]!");
                valueIndex++;
            }
        }

        builder.Append(')');
    }

    private static void EmitParameters(
        StringBuilder builder,
        IReadOnlyList<GeneratedCallSiteParameter> parameters,
        string indent)
    {
        var serviceParameters = parameters.Where(parameter => parameter.ConstantExpression is null).ToArray();
        builder.Append(indent).AppendLine("new GeneratedParameter[]");
        builder.Append(indent).AppendLine("{");
        for (var index = 0; index < serviceParameters.Length; index++)
        {
            var parameter = serviceParameters[index];
            builder.Append(indent).Append("    new GeneratedParameter(typeof(").Append(parameter.TypeDisplay).Append(')');
            if (parameter.ServiceKeyExpression is not null)
            {
                builder.Append(", serviceKey: ").Append(parameter.ServiceKeyExpression);
                builder.Append(", lookupMode: ServiceKeyLookupMode.ExplicitKey");
            }
            else if (parameter.LookupMode == "InheritKey")
            {
                builder.Append(", serviceKey: null, lookupMode: ServiceKeyLookupMode.InheritKey");
            }

            if (parameter.HasDefaultValue)
            {
                builder.Append(", hasDefaultValue: true, defaultValue: ").Append(parameter.DefaultExpression);
            }

            builder.AppendLine(index == serviceParameters.Length - 1 ? ")" : "),");
        }

        builder.Append(indent).Append('}');
    }

    private static void EmitAssignableTypes(StringBuilder builder, IReadOnlyList<string> types, string indent)
    {
        if (types.Count == 0)
        {
            builder.Append(indent).Append("Array.Empty<Type>()");
            return;
        }

        builder.Append(indent).AppendLine("new Type[]");
        builder.Append(indent).AppendLine("{");
        for (var index = 0; index < types.Count; index++)
        {
            builder.Append(indent).Append("    typeof(").Append(types[index]).Append(')');
            builder.AppendLine(index == types.Count - 1 ? string.Empty : ",");
        }

        builder.Append(indent).Append('}');
    }

    private static string StringLiteral(string value) => SymbolDisplay.FormatLiteral(value, quote: true);
}
