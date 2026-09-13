// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public interface IXmlSerializable
{
    XmlSchema? GetSchema();
    void ReadXml(XmlReader reader);
    void WriteXml(XmlWriter writer);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public sealed class XmlRootAttribute : Attribute
{
    public XmlRootAttribute() { }
    public XmlRootAttribute(string elementName) => ElementName = elementName;
    public string? ElementName { get; set; }
    public string? Namespace { get; set; }
    public bool IsNullable { get; set; } = true;
    public string? DataType { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public sealed class XmlTypeAttribute : Attribute
{
    public string? TypeName { get; set; }
    public string? Namespace { get; set; }
    public bool AnonymousType { get; set; }
    public bool IncludeInSchema { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class XmlElementAttribute : Attribute
{
    public XmlElementAttribute() { }
    public XmlElementAttribute(string elementName) => ElementName = elementName;
    public string? ElementName { get; set; }
    public string? Namespace { get; set; }
    public bool IsNullable { get; set; } = true;
    public string? DataType { get; set; }
    public int Order { get; set; } = -1;
}

public sealed class XmlSerializer
{
    private readonly Type _type;
    private readonly string _rootName;
    private readonly string? _rootNamespace;

    public XmlSerializer(Type type)
        : this(type, null)
    {
    }

    public XmlSerializer(Type type, XmlRootAttribute? root)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!PrimitiveCodec.IsSupported(type))
        {
            throw new PlatformNotSupportedException("Only statically known primitive XML serializer contracts are available in the reflection-free profile.");
        }

        _type = type;
        _rootName = string.IsNullOrEmpty(root?.ElementName) ? PrimitiveCodec.DefaultRootName(type) : root!.ElementName!;
        _rootNamespace = root?.Namespace;
    }

    public void Serialize(Stream stream, object? value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var writer = XmlWriter.Create(stream);
        Serialize(writer, value);
        writer.Flush();
    }

    public void Serialize(TextWriter textWriter, object? value)
    {
        ArgumentNullException.ThrowIfNull(textWriter);
        var writer = XmlWriter.Create(textWriter);
        Serialize(writer, value);
        writer.Flush();
    }

    public void Serialize(XmlWriter writer, object? value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null || !PrimitiveCodec.IsValueOfType(value, _type))
        {
            throw new InvalidOperationException("The value does not match the statically registered primitive XML serializer contract.");
        }

        writer.WriteStartElement(_rootName);
        writer.WriteString(PrimitiveCodec.Format(value, _type));
        writer.WriteEndElement();
    }

    public object Deserialize(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Deserialize(XmlReader.Create(stream));
    }

    public object Deserialize(TextReader textReader)
    {
        ArgumentNullException.ThrowIfNull(textReader);
        return Deserialize(XmlReader.Create(textReader));
    }

    public object Deserialize(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        while (reader.Read() && reader.NodeType != XmlNodeType.Element) { }
        if (reader.NodeType != XmlNodeType.Element || !string.Equals(reader.LocalName, _rootName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The XML root does not match the statically registered primitive serializer contract.");
        }

        return PrimitiveCodec.Parse(reader.ReadContentAsString(), _type);
    }
}

internal static class PrimitiveCodec
{
    public static bool IsSupported(Type type) =>
        type == typeof(string) || type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) ||
        type == typeof(decimal) || type == typeof(char) || type == typeof(DateTime);

    public static bool IsValueOfType(object value, Type type) =>
        (type == typeof(string) && value is string) || (type == typeof(bool) && value is bool) ||
        (type == typeof(byte) && value is byte) || (type == typeof(sbyte) && value is sbyte) ||
        (type == typeof(short) && value is short) || (type == typeof(ushort) && value is ushort) ||
        (type == typeof(int) && value is int) || (type == typeof(uint) && value is uint) ||
        (type == typeof(long) && value is long) || (type == typeof(ulong) && value is ulong) ||
        (type == typeof(float) && value is float) || (type == typeof(double) && value is double) ||
        (type == typeof(decimal) && value is decimal) || (type == typeof(char) && value is char) ||
        (type == typeof(DateTime) && value is DateTime);

    public static string DefaultRootName(Type type) =>
        type == typeof(string) ? "string" : type == typeof(bool) ? "boolean" : type.Name;

    public static string Format(object value, Type type) => type == typeof(string) ? (string)value :
        type == typeof(bool) ? ((bool)value ? "true" : "false") :
        type == typeof(char) ? ((char)value).ToString() :
        type == typeof(DateTime) ? ((DateTime)value).ToString("O", CultureInfo.InvariantCulture) :
        ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture)!;

    public static object Parse(string text, Type type)
    {
        if (type == typeof(string)) return text;
        if (type == typeof(bool) && bool.TryParse(text, out var boolean)) return boolean;
        if (type == typeof(byte) && byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteValue)) return byteValue;
        if (type == typeof(sbyte) && sbyte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sbyteValue)) return sbyteValue;
        if (type == typeof(short) && short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortValue)) return shortValue;
        if (type == typeof(ushort) && ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ushortValue)) return ushortValue;
        if (type == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)) return intValue;
        if (type == typeof(uint) && uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uintValue)) return uintValue;
        if (type == typeof(long) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue)) return longValue;
        if (type == typeof(ulong) && ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongValue)) return ulongValue;
        if (type == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue)) return floatValue;
        if (type == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)) return doubleValue;
        if (type == typeof(decimal) && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)) return decimalValue;
        if (type == typeof(char) && text.Length == 1) return text[0];
        if (type == typeof(DateTime) && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeValue)) return dateTimeValue;
        throw new FormatException("The XML value is not valid for the statically registered primitive serializer contract.");
    }
}
