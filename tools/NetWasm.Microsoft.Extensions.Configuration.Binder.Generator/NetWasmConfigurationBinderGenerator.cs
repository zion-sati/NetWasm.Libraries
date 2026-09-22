using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetWasm.Microsoft.Extensions.Configuration.Binder.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class NetWasmConfigurationBinderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol?> requests = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax,
                static (syntaxContext, _) => ReadRequest(syntaxContext))
            .Where(static type => type is not null);

        context.RegisterSourceOutput(
            requests.Collect(),
            static (productionContext, types) => Emit(productionContext, types));
    }

    private static INamedTypeSymbol? ReadRequest(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || method.TypeArguments.Length != 1)
        {
            return null;
        }

        string containingType = method.ContainingType.ToDisplayString();
        bool isBinder = containingType == "Microsoft.Extensions.Configuration.ConfigurationBinder" &&
            (method.Name == "Get" || method.Name == "Bind");
        bool isOptionsConfiguration =
            (containingType == "Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions" && method.Name == "Configure") ||
            (containingType == "Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions" && method.Name is "Bind" or "BindConfiguration");
        if (!isBinder && !isOptionsConfiguration) return null;

        return method.TypeArguments[0] as INamedTypeSymbol;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> input)
    {
        var types = new List<INamedTypeSymbol>();
        foreach (INamedTypeSymbol? type in input)
        {
            if (type is null || types.Any(existing => SymbolEqualityComparer.Default.Equals(existing, type))) continue;
            if (type.TypeKind != TypeKind.Class)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("NWCFG010", "NetWasm configuration binding", "Only class configuration targets are supported by the NetWasm generator.", "NetWasm.Configuration", DiagnosticSeverity.Error, true),
                    Location.None));
                continue;
            }

            types.Add(type);
        }

        if (types.Count == 0) return;

        var emitter = new Emitter(types);
        context.AddSource("NetWasm.Configuration.Binding.g.cs", emitter.Emit());
    }

    private sealed class Emitter
    {
        private readonly List<INamedTypeSymbol> _roots;
        private readonly List<INamedTypeSymbol> _objects = new();
        private readonly Dictionary<string, int> _ids = new();

        public Emitter(List<INamedTypeSymbol> roots)
        {
            _roots = roots;
            foreach (INamedTypeSymbol root in roots) VisitObject(root);
        }

        public string Emit()
        {
            var writer = new StringBuilder();
            writer.AppendLine("#nullable enable");
            writer.AppendLine("namespace Generated;");
            writer.AppendLine("internal static class NetWasmGeneratedConfigurationBinding");
            writer.AppendLine("{");
            writer.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
            writer.AppendLine("    internal static void Register()");
            writer.AppendLine("    {");
            foreach (INamedTypeSymbol root in _roots)
            {
                string typeName = Display(root);
                writer.Append("        global::Microsoft.Extensions.Configuration.ConfigurationBinder.Register<")
                    .Append(typeName).Append(">(Bind_").Append(_ids[Identity(root)]).Append(", Apply_").Append(_ids[Identity(root)]).AppendLine(");");
            }

            writer.AppendLine("    }");
            foreach (INamedTypeSymbol objectType in _objects) EmitObject(writer, objectType);
            writer.AppendLine("}");
            return writer.ToString();
        }

        private void VisitObject(INamedTypeSymbol type)
        {
            string identity = Identity(type);
            if (_ids.ContainsKey(identity)) return;
            _ids.Add(identity, _objects.Count);
            _objects.Add(type);
            foreach (IPropertySymbol property in Properties(type))
            {
                ITypeSymbol? element = CollectionElement(property.Type);
                ITypeSymbol? nested = element is null ? NestedType(property.Type) : NestedType(element);
                if (nested is INamedTypeSymbol named) VisitObject(named);
            }
        }

        private void EmitObject(StringBuilder writer, INamedTypeSymbol type)
        {
            int id = _ids[Identity(type)];
            writer.Append("    private static ").Append(Display(type)).Append(" Bind_").Append(id)
                .AppendLine("(global::Microsoft.Extensions.Configuration.IConfiguration configuration, global::Microsoft.Extensions.Configuration.BinderOptions options)");
            writer.AppendLine("    {");
            writer.Append("        var result = new ").Append(Display(type)).AppendLine("();");
            writer.Append("        Apply_").Append(id).AppendLine("(configuration, options, result);");
            writer.AppendLine("        return result;");
            writer.AppendLine("    }");
            writer.Append("    private static void Apply_").Append(id)
                .AppendLine("(global::Microsoft.Extensions.Configuration.IConfiguration configuration, global::Microsoft.Extensions.Configuration.BinderOptions options, ")
                .Append(Display(type)).AppendLine(" result)");
            writer.AppendLine("    {");
            int propertyIndex = 0;
            foreach (IPropertySymbol property in Properties(type))
            {
                if (property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "Microsoft.Extensions.Configuration.ConfigurationIgnoreAttribute")) continue;
                string key = PropertyKey(property);
                string suffix = id + "_" + propertyIndex++;
                string section = "section_" + suffix;
                writer.Append("        var ").Append(section).Append(" = configuration.GetSection(\"")
                    .Append(Escape(key)).AppendLine("\");");
                if (CollectionElement(property.Type) is not null || NestedType(property.Type) is not null)
                {
                    writer.Append("        if (global::Microsoft.Extensions.Configuration.ConfigurationExtensions.Exists(")
                        .Append(section).AppendLine("))");
                }
                else
                {
                    writer.Append("        if (global::Microsoft.Extensions.Configuration.ConfigurationBinder.ValueIsPresent(")
                        .Append(section).AppendLine("))");
                }
                writer.AppendLine("        {");
                if (CollectionElement(property.Type) is ITypeSymbol element)
                {
                    writer.Append("            var values_").Append(suffix).Append(" = new global::System.Collections.Generic.List<")
                        .Append(Display(element)).AppendLine(">();");
                    writer.Append("            foreach (global::Microsoft.Extensions.Configuration.IConfigurationSection child in ")
                        .Append(section).AppendLine(".GetChildren())");
                    writer.AppendLine("            {");
                    if (NestedType(element) is INamedTypeSymbol nested)
                    {
                        writer.Append("                values_").Append(suffix).Append(".Add(Bind_").Append(_ids[Identity(nested)]).AppendLine("(child, options));");
                    }
                    else
                    {
                        writer.Append("                values_").Append(suffix).Append(".Add(global::Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<")
                            .Append(Display(element)).AppendLine(">(child, string.Empty, default!));");
                    }
                    writer.AppendLine("            }");
                    writer.Append("            result.").Append(property.Name).Append(" = ");
                    if (property.Type is IArrayTypeSymbol) writer.Append("values_").Append(suffix).AppendLine(".ToArray();");
                    else writer.Append("values_").Append(suffix).AppendLine(";");
                }
                else if (NestedType(property.Type) is INamedTypeSymbol nested)
                {
                    if (property.GetMethod is not null && property.GetMethod.DeclaredAccessibility == Accessibility.Public)
                    {
                        writer.Append("            var existing_").Append(suffix).Append(" = result.").Append(property.Name).AppendLine(";");
                        writer.Append("            if (existing_").Append(suffix).AppendLine(" is null)");
                        writer.AppendLine("            {");
                        writer.Append("                result.").Append(property.Name).Append(" = Bind_").Append(_ids[Identity(nested)])
                            .Append('(').Append(section).AppendLine(", options);");
                        writer.AppendLine("            }");
                        writer.AppendLine("            else");
                        writer.AppendLine("            {");
                        writer.Append("                Apply_").Append(_ids[Identity(nested)]).Append('(').Append(section)
                            .Append(", options, existing_").Append(suffix).AppendLine(");");
                        writer.AppendLine("            }");
                    }
                    else
                    {
                        writer.Append("            result.").Append(property.Name).Append(" = Bind_").Append(_ids[Identity(nested)])
                            .Append('(').Append(section).AppendLine(", options);");
                    }
                }
                else
                {
                    writer.Append("            result.").Append(property.Name).Append(" = global::Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<")
                        .Append(Display(property.Type)).Append(">(").Append(section).AppendLine(", string.Empty, default!);");
                }
                writer.AppendLine("        }");
            }

            writer.AppendLine("    }");
        }

        private static IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol type)
        {
            var properties = new List<IPropertySymbol>();
            var names = new HashSet<string>(System.StringComparer.Ordinal);
            for (INamedTypeSymbol? current = type;
                 current is not null && current.SpecialType != SpecialType.System_Object;
                 current = current.BaseType)
            {
                foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (!property.IsStatic && !property.IsIndexer && property.SetMethod is not null &&
                        property.SetMethod.DeclaredAccessibility == Accessibility.Public && names.Add(property.Name))
                        properties.Add(property);
                }
            }

            return properties.OrderBy(property => property.Name, System.StringComparer.Ordinal);
        }

        private static ITypeSymbol? NestedType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named && named.SpecialType != SpecialType.System_String &&
                named.TypeKind == TypeKind.Class && named.Name != "List" && named.Name != "IList" &&
                named.Name != "IEnumerable" && named.Name != "IReadOnlyList") return named;
            return null;
        }

        private static ITypeSymbol? CollectionElement(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol array) return array.ElementType;
            if (type is INamedTypeSymbol named && named.TypeArguments.Length == 1 &&
                (named.Name == "List" || named.Name == "IList" || named.Name == "IEnumerable" || named.Name == "IReadOnlyList"))
                return named.TypeArguments[0];
            return null;
        }

        private static string PropertyKey(IPropertySymbol property)
        {
            foreach (AttributeData attribute in property.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "Microsoft.Extensions.Configuration.ConfigurationKeyNameAttribute" &&
                    attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string name)
                    return name;
            }
            return property.Name;
        }

        private static string Display(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private static string Identity(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
