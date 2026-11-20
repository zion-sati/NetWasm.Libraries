// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Extensions.Configuration;

// The conversions of a configuration value, each what upstream's binder gets from the type's TypeConverter with an
// invariant string: integers also in hexadecimal ("0x", "&h" or "#"), dates and times empty as their minimum, enums by
// name or number ignoring case, a byte[] from base64. A failure is upstream's InvalidOperationException naming the
// value, its path and the type. Generated binding calls the one each property needs, so a program compiles only those.
public static partial class ConfigurationBinder
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool ParseBoolean(string value, string? path) =>
        Convert(value, path, static text => bool.Parse(text.Trim()));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static char ParseChar(string value, string? path) => Convert(value, path, static text =>
    {
        if (text.Length > 1) text = text.Trim();
        return text.Length switch
        {
            0 => '\0',
            1 => text[0],
            _ => throw new FormatException($"{text} is not a valid value for Char."),
        };
    });

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static sbyte ParseSByte(string value, string? path) =>
        Integer(value, path, static (text, styles) => sbyte.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static byte ParseByte(string value, string? path) =>
        Integer(value, path, static (text, styles) => byte.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static short ParseInt16(string value, string? path) =>
        Integer(value, path, static (text, styles) => short.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ushort ParseUInt16(string value, string? path) =>
        Integer(value, path, static (text, styles) => ushort.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int ParseInt32(string value, string? path) =>
        Integer(value, path, static (text, styles) => int.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static uint ParseUInt32(string value, string? path) =>
        Integer(value, path, static (text, styles) => uint.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static long ParseInt64(string value, string? path) =>
        Integer(value, path, static (text, styles) => long.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ulong ParseUInt64(string value, string? path) =>
        Integer(value, path, static (text, styles) => ulong.Parse(text, styles, CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static float ParseSingle(string value, string? path) => Convert(value, path, static text =>
        Number(text, "Single", static trimmed => float.Parse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture)));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static double ParseDouble(string value, string? path) => Convert(value, path, static text =>
        Number(text, "Double", static trimmed => double.Parse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture)));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static decimal ParseDecimal(string value, string? path) => Convert(value, path, static text =>
        Number(text, "Decimal", static trimmed => decimal.Parse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture)));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static DateTime ParseDateTime(string value, string? path) => Convert(value, path, static text =>
        text.Trim() is { Length: > 0 } trimmed ? DateTime.Parse(trimmed, CultureInfo.InvariantCulture) : DateTime.MinValue);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static DateTimeOffset ParseDateTimeOffset(string value, string? path) => Convert(value, path, static text =>
        text.Trim() is { Length: > 0 } trimmed
            ? DateTimeOffset.Parse(trimmed, CultureInfo.InvariantCulture)
            : DateTimeOffset.MinValue);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static DateOnly ParseDateOnly(string value, string? path) => Convert(value, path, static text =>
        text.Trim() is { Length: > 0 } trimmed ? DateOnly.Parse(trimmed, CultureInfo.InvariantCulture) : DateOnly.MinValue);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TimeOnly ParseTimeOnly(string value, string? path) => Convert(value, path, static text =>
        text.Trim() is { Length: > 0 } trimmed ? TimeOnly.Parse(trimmed, CultureInfo.InvariantCulture) : TimeOnly.MinValue);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TimeSpan ParseTimeSpan(string value, string? path) =>
        Convert(value, path, static text => TimeSpan.Parse(text.Trim(), CultureInfo.InvariantCulture));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Guid ParseGuid(string value, string? path) => Convert(value, path, static text => new Guid(text.Trim()));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Uri? ParseUri(string value, string? path) =>
        Convert(value, path, static text => text.Length == 0 ? null : new Uri(text, UriKind.RelativeOrAbsolute));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Version ParseVersion(string value, string? path) => Convert(value, path, static text => Version.Parse(text));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static byte[] ParseBase64(string value, string? path) =>
        Convert(value, path, static text => System.Convert.FromBase64String(text));

    // EnumConverter's: a name, a number or a comma-separated list of either, ignoring case.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TEnum ParseEnum<TEnum>(string value, string? path)
        where TEnum : struct, Enum => Convert(value, path, static text => Enum.Parse<TEnum>(text.Trim(), ignoreCase: true));

    // A value of T for GetValue<T>, where the generator registers T's conversion, as for an enum.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterValue<T>(Func<string, string?, T> parse)
    {
        ArgumentNullException.ThrowIfNull(parse);
        ValueRegistry<T>.Parse = parse;
    }

    // GetValue<T>'s conversion. Each test of T is a constant once T is closed, so an instantiation keeps only its own.
    private static T? ConvertValue<T>(string value, string? path)
    {
        if (typeof(T) == typeof(string) || typeof(T) == typeof(object)) return (T)(object)value;
        if (ValueRegistry<T>.Parse is { } registered) return registered(value, path);
        // A nullable value type's empty value is null, as upstream's binder reads it.
        if (Nullable.GetUnderlyingType(typeof(T)) is not null && value.Length == 0) return default;
        if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?)) return (T)(object)ParseBoolean(value, path);
        if (typeof(T) == typeof(char) || typeof(T) == typeof(char?)) return (T)(object)ParseChar(value, path);
        if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?)) return (T)(object)ParseSByte(value, path);
        if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?)) return (T)(object)ParseByte(value, path);
        if (typeof(T) == typeof(short) || typeof(T) == typeof(short?)) return (T)(object)ParseInt16(value, path);
        if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?)) return (T)(object)ParseUInt16(value, path);
        if (typeof(T) == typeof(int) || typeof(T) == typeof(int?)) return (T)(object)ParseInt32(value, path);
        if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?)) return (T)(object)ParseUInt32(value, path);
        if (typeof(T) == typeof(long) || typeof(T) == typeof(long?)) return (T)(object)ParseInt64(value, path);
        if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?)) return (T)(object)ParseUInt64(value, path);
        if (typeof(T) == typeof(float) || typeof(T) == typeof(float?)) return (T)(object)ParseSingle(value, path);
        if (typeof(T) == typeof(double) || typeof(T) == typeof(double?)) return (T)(object)ParseDouble(value, path);
        if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?)) return (T)(object)ParseDecimal(value, path);
        if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?)) return (T)(object)ParseDateTime(value, path);
        if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
            return (T)(object)ParseDateTimeOffset(value, path);
        if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?)) return (T)(object)ParseDateOnly(value, path);
        if (typeof(T) == typeof(TimeOnly) || typeof(T) == typeof(TimeOnly?)) return (T)(object)ParseTimeOnly(value, path);
        if (typeof(T) == typeof(TimeSpan) || typeof(T) == typeof(TimeSpan?)) return (T)(object)ParseTimeSpan(value, path);
        if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?)) return (T)(object)ParseGuid(value, path);
        if (typeof(T) == typeof(Uri)) return (T)(object?)ParseUri(value, path)!;
        if (typeof(T) == typeof(Version)) return (T)(object)ParseVersion(value, path);
        if (typeof(T) == typeof(byte[])) return (T)(object)ParseBase64(value, path);
        throw new NotSupportedException(
            $"ConfigurationBinder.GetValue<T> converts {typeof(T)} only once the NetWasm configuration binding generator registers it.");
    }

    private static T Integer<T>(string value, string? path, Func<string, NumberStyles, T> parse) => Convert(value, path, text =>
        Number(text, typeof(T).Name, trimmed =>
            trimmed.StartsWith('#') ? parse(trimmed[1..], NumberStyles.HexNumber)
            : trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("&h", StringComparison.OrdinalIgnoreCase)
                ? parse(trimmed[2..], NumberStyles.HexNumber)
                : parse(trimmed, NumberStyles.Integer)));

    // BaseNumberConverter's: the trimmed text, a failure reported as an ArgumentException naming the type.
    private static T Number<T>(string value, string typeName, Func<string, T> parse)
    {
        string text = value.Trim();
        try
        {
            return parse(text);
        }
        catch (Exception exception)
        {
            throw new ArgumentException($"{text} is not a valid value for {typeName}.", nameof(value), exception);
        }
    }

    private static T Convert<T>(string value, string? path, Func<string, T> parse)
    {
        try
        {
            return parse(value);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Failed to convert configuration value '{value}' at '{path}' to type '{typeof(T)}'.", exception);
        }
    }

    private static class ValueRegistry<T>
    {
        internal static Func<string, string?, T>? Parse;
    }
}
