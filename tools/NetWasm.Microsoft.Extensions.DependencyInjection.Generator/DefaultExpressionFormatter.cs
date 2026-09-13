using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal interface IOptionalDefaultExpressionFormatter
{
    string Format(ITypeSymbol type, object? value);
}

internal sealed class OptionalDefaultExpressionFormatter : IOptionalDefaultExpressionFormatter
{
    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public string Format(ITypeSymbol type, object? value)
    {
        Guard.NotNull(type, nameof(type));
        if (value is null)
        {
            return "null";
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            return "(" + type.ToDisplayString(TypeFormat) + ")" + Format(enumType.EnumUnderlyingType!, value);
        }

        return type.SpecialType switch
        {
            SpecialType.System_String => SymbolDisplay.FormatLiteral((string)value, quote: true),
            SpecialType.System_Char => SymbolDisplay.FormatLiteral((char)value, quote: true),
            SpecialType.System_Boolean => (bool)value ? "true" : "false",
            SpecialType.System_SByte => "(sbyte)" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_Byte => "(byte)" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_Int16 => "(short)" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_UInt16 => "(ushort)" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            SpecialType.System_Int32 => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
            SpecialType.System_UInt32 => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) + "U",
            SpecialType.System_Int64 => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) + "L",
            SpecialType.System_UInt64 => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) + "UL",
            SpecialType.System_Single => FormatSingle((float)value),
            SpecialType.System_Double => FormatDouble((double)value),
            SpecialType.System_Decimal => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) + "M",
            _ => throw new GeneratorDiagnosticException("NWDI005", $"Unsupported optional default type '{type.ToDisplayString(TypeFormat)}'."),
        };
    }

    internal static string FormatSingle(float value) =>
        float.IsNaN(value) ? "float.NaN" :
        float.IsPositiveInfinity(value) ? "float.PositiveInfinity" :
        float.IsNegativeInfinity(value) ? "float.NegativeInfinity" :
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "F";

    internal static string FormatDouble(double value) =>
        double.IsNaN(value) ? "double.NaN" :
        double.IsPositiveInfinity(value) ? "double.PositiveInfinity" :
        double.IsNegativeInfinity(value) ? "double.NegativeInfinity" :
        value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
}
