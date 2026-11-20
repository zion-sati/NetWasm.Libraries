// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

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
        IncrementalValuesProvider<Request?> requests = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax,
                static (syntaxContext, _) => ReadRequest(syntaxContext))
            .Where(static request => request is not null);

        context.RegisterSourceOutput(
            requests.Collect(),
            static (productionContext, input) => Emit(productionContext, input));
    }

    // A type to bind, or a type GetValue<T> reads that only generated code converts, as an enum.
    private sealed class Request
    {
        public Request(INamedTypeSymbol type, bool value)
        {
            Type = type;
            Value = value;
        }

        public INamedTypeSymbol Type { get; }

        public bool Value { get; }
    }

    private static Request? ReadRequest(GeneratorSyntaxContext context)
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
        if (containingType == "Microsoft.Extensions.Configuration.ConfigurationBinder" && method.Name == "GetValue" &&
            method.TypeArguments[0] is INamedTypeSymbol valueType && EnumOf(valueType) is not null)
        {
            return new Request(valueType, value: true);
        }
        if (!isBinder && !isOptionsConfiguration) return null;

        return method.TypeArguments[0] is INamedTypeSymbol type ? new Request(type, value: false) : null;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<Request?> input)
    {
        var types = new List<INamedTypeSymbol>();
        var values = new List<INamedTypeSymbol>();
        foreach (Request? request in input)
        {
            if (request is { Value: true } &&
                !values.Any(existing => SymbolEqualityComparer.Default.Equals(existing, request.Type)))
            {
                values.Add(request.Type);
                continue;
            }
            INamedTypeSymbol? type = request?.Type;
            if (type is null || request!.Value || types.Any(existing => SymbolEqualityComparer.Default.Equals(existing, type))) continue;
            if (type.TypeKind != TypeKind.Class)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("NWCFG010", "NetWasm configuration binding", "Only class configuration targets are supported by the NetWasm generator.", "NetWasm.Configuration", DiagnosticSeverity.Error, true),
                    Location.None));
                continue;
            }

            types.Add(type);
        }

        if (types.Count == 0 && values.Count == 0) return;

        var emitter = new Emitter(types, values);
        context.AddSource("NetWasm.Configuration.Binding.g.cs", emitter.Emit());
        foreach (string unsupported in emitter.Unsupported)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("NWCFG011", "NetWasm configuration binding",
                    "The NetWasm configuration binding generator cannot convert a configuration value to '{0}'",
                    "NetWasm.Configuration", DiagnosticSeverity.Error, true),
                Location.None, unsupported));
        }
    }

    // The enum a type is, or that a nullable type wraps.
    private static INamedTypeSymbol? EnumOf(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol { TypeKind: TypeKind.Enum } named => named,
        INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable =>
            nullable.TypeArguments[0] as INamedTypeSymbol is { TypeKind: TypeKind.Enum } underlying ? underlying : null,
        _ => null,
    };

    private sealed class Emitter
    {
        private readonly List<INamedTypeSymbol> _roots;
        private readonly List<INamedTypeSymbol> _values;
        private readonly List<INamedTypeSymbol> _objects = new();
        private readonly Dictionary<string, int> _ids = new();

        public Emitter(List<INamedTypeSymbol> roots, List<INamedTypeSymbol> values)
        {
            _roots = roots;
            _values = values;
            foreach (INamedTypeSymbol root in roots) VisitObject(root);
        }

        // The value types nothing here can convert, by name.
        public SortedSet<string> Unsupported { get; } = new(System.StringComparer.Ordinal);

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
            foreach (INamedTypeSymbol value in _values)
            {
                writer.Append("        global::Microsoft.Extensions.Configuration.ConfigurationBinder.RegisterValue<")
                    .Append(Display(value)).Append(">(static (value, path) => ").Append(Parse(value, "value", "path")).AppendLine(");");
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
                ITypeSymbol? element = CollectionElement(property.Type) ?? DictionaryTypes(property.Type)?.Value;
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
                if (CollectionElement(property.Type) is not null || DictionaryTypes(property.Type) is not null ||
                    NestedType(property.Type) is not null)
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
                if (DictionaryTypes(property.Type) is { } dictionary)
                {
                    EmitDictionary(writer, property, section, suffix, dictionary.Key, dictionary.Value);
                }
                else if (CollectionElement(property.Type) is ITypeSymbol element)
                {
                    // As upstream reads a byte[]: base64 when the section has a value, otherwise its elements.
                    bool base64 = Name(property.Type) == "byte[]";
                    if (base64)
                    {
                        writer.Append("            if (").Append(section).AppendLine(".Value is { } base64)");
                        writer.AppendLine("            {");
                        writer.Append("                result.").Append(property.Name)
                            .Append(" = global::Microsoft.Extensions.Configuration.ConfigurationBinder.ParseBase64(base64, ")
                            .Append(section).AppendLine(".Path);");
                        writer.AppendLine("            }");
                        writer.AppendLine("            else");
                        writer.AppendLine("            {");
                    }
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
                        writer.AppendLine("                var value = child.Value;");
                        writer.Append("                values_").Append(suffix).Append(".Add(value is null ? default! : ")
                            .Append(Parse(element, "value", "child.Path")).AppendLine(");");
                    }
                    writer.AppendLine("            }");
                    writer.Append("            result.").Append(property.Name).Append(" = ");
                    if (property.Type is IArrayTypeSymbol) writer.Append("values_").Append(suffix).AppendLine(".ToArray();");
                    else writer.Append("values_").Append(suffix).AppendLine(";");
                    if (base64) writer.AppendLine("            }");
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
                    writer.Append("            var value_").Append(suffix).Append(" = ").Append(section).AppendLine(".Value;");
                    writer.Append("            result.").Append(property.Name).Append(" = value_").Append(suffix)
                        .Append(" is null ? default! : ").Append(Parse(property.Type, "value_" + suffix, section + ".Path"))
                        .AppendLine(";");
                }
                writer.AppendLine("        }");
            }

            writer.AppendLine("    }");
        }

        // As upstream binds a dictionary: into the one the property holds, or a new one, each child by its key.
        private void EmitDictionary(
            StringBuilder writer, IPropertySymbol property, string section, string suffix, ITypeSymbol key, ITypeSymbol value)
        {
            string concrete = "global::System.Collections.Generic.Dictionary<" + Display(key) + ", " + Display(value) + ">";
            bool readable = property.GetMethod is { DeclaredAccessibility: Accessibility.Public } &&
                property.Type is INamedTypeSymbol { Name: "Dictionary" or "IDictionary" };
            writer.Append("            var map_").Append(suffix).Append(" = ")
                .Append(readable ? "result." + property.Name + " ?? " : "").Append("new ").Append(concrete).AppendLine("();");
            writer.Append("            foreach (global::Microsoft.Extensions.Configuration.IConfigurationSection child in ")
                .Append(section).AppendLine(".GetChildren())");
            writer.AppendLine("            {");
            writer.Append("                var key = ").Append(Parse(key, "child.Key", "child.Path")).AppendLine(";");
            if (NestedType(value) is INamedTypeSymbol nested)
            {
                writer.Append("                if (map_").Append(suffix).AppendLine(".TryGetValue(key, out var existing) && existing is not null)");
                writer.Append("                    Apply_").Append(_ids[Identity(nested)]).AppendLine("(child, options, existing);");
                writer.AppendLine("                else");
                writer.Append("                    map_").Append(suffix).Append("[key] = Bind_").Append(_ids[Identity(nested)])
                    .AppendLine("(child, options);");
            }
            else
            {
                writer.AppendLine("                var value = child.Value;");
                writer.AppendLine("                if (value is not null)");
                writer.Append("                    map_").Append(suffix).Append("[key] = ").Append(Parse(value, "value", "child.Path"))
                    .AppendLine(";");
            }
            writer.AppendLine("            }");
            writer.Append("            result.").Append(property.Name).Append(" = map_").Append(suffix).AppendLine(";");
        }

        // The conversion of a string to the type, each upstream's TypeConverter's; a nullable's empty value is null.
        private string Parse(ITypeSymbol type, string value, string path)
        {
            const string Binder = "global::Microsoft.Extensions.Configuration.ConfigurationBinder.";
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                return "(" + value + ".Length == 0 ? (" + Display(type) + ")null : " + Parse(nullable.TypeArguments[0], value, path) + ")";
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                return Binder + "ParseEnum<" + Display(type) + ">(" + value + ", " + path + ")";
            }
            string? parser = type.SpecialType switch
            {
                SpecialType.System_String or SpecialType.System_Object => null,
                SpecialType.System_Boolean => "ParseBoolean",
                SpecialType.System_Char => "ParseChar",
                SpecialType.System_SByte => "ParseSByte",
                SpecialType.System_Byte => "ParseByte",
                SpecialType.System_Int16 => "ParseInt16",
                SpecialType.System_UInt16 => "ParseUInt16",
                SpecialType.System_Int32 => "ParseInt32",
                SpecialType.System_UInt32 => "ParseUInt32",
                SpecialType.System_Int64 => "ParseInt64",
                SpecialType.System_UInt64 => "ParseUInt64",
                SpecialType.System_Single => "ParseSingle",
                SpecialType.System_Double => "ParseDouble",
                SpecialType.System_Decimal => "ParseDecimal",
                SpecialType.System_DateTime => "ParseDateTime",
                _ => Name(type) switch
                {
                    "System.DateTimeOffset" => "ParseDateTimeOffset",
                    "System.DateOnly" => "ParseDateOnly",
                    "System.TimeOnly" => "ParseTimeOnly",
                    "System.TimeSpan" => "ParseTimeSpan",
                    "System.Guid" => "ParseGuid",
                    "System.Uri" => "ParseUri",
                    "System.Version" => "ParseVersion",
                    "byte[]" => "ParseBase64",
                    _ => "",
                },
            };
            if (parser is null) return value;
            if (parser.Length == 0)
            {
                Unsupported.Add(Name(type));
                return "default!";
            }
            return Binder + parser + "(" + value + ", " + path + ")" + (parser == "ParseUri" ? "!" : "");
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
                named.Name != "IEnumerable" && named.Name != "IReadOnlyList" && DictionaryTypes(named) is null &&
                Name(named) is not ("System.Uri" or "System.Version")) return named;
            return null;
        }

        // A dictionary's key and value types: Dictionary, IDictionary or IReadOnlyDictionary.
        private static (ITypeSymbol Key, ITypeSymbol Value)? DictionaryTypes(ITypeSymbol type) =>
            type is INamedTypeSymbol { TypeArguments.Length: 2 } named &&
            named.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic" &&
            named.Name is "Dictionary" or "IDictionary" or "IReadOnlyDictionary"
                ? (named.TypeArguments[0], named.TypeArguments[1])
                : null;

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

        // A type's name without its nullable annotation, as "System.Uri" for Uri?.
        private static string Name(ITypeSymbol type) =>
            type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();

        private static string Identity(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
