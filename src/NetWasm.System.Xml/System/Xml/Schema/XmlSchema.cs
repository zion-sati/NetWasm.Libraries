// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace System.Xml.Schema;

public delegate void ValidationEventHandler(object? sender, ValidationEventArgs e);

public sealed class ValidationEventArgs : EventArgs
{
    public ValidationEventArgs(string message, XmlSchemaException exception)
    {
        Message = message;
        Exception = exception;
    }

    public string Message { get; }
    public XmlSchemaException Exception { get; }
    public XmlSeverityType Severity => XmlSeverityType.Error;
}

public enum XmlSeverityType { Error, Warning }

public class XmlSchemaException : SystemException
{
    public XmlSchemaException(string message) : base(message) { }
}

public enum XmlSchemaValidationFlags
{
    None = 0,
    ProcessInlineSchema = 1,
    ProcessSchemaLocation = 2,
    ReportValidationWarnings = 4,
    AllowXmlAttributes = 8,
}

public class XmlSchemaObject
{
    public string? Id { get; set; }
}

public sealed class XmlSchema : XmlSchemaObject
{
    private readonly Dictionary<string, XmlSchemaElement> _elements = new(StringComparer.Ordinal);

    public string? TargetNamespace { get; set; }
    public bool IsCompiled { get; internal set; }
    public IDictionary<string, XmlSchemaElement> Elements => _elements;

    public static XmlSchema? Read(XmlReader reader, ValidationEventHandler? validationEventHandler)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var schema = new XmlSchema();
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "element")
            {
                var name = reader.GetAttribute("name");
                if (!string.IsNullOrEmpty(name)) schema.Elements[name!] = new XmlSchemaElement { Name = name };
            }
        }
        return schema;
    }

    public static XmlSchema? Read(TextReader reader, ValidationEventHandler? validationEventHandler) =>
        Read(XmlReader.Create(reader), validationEventHandler);
}

public sealed class XmlSchemaElement : XmlSchemaObject
{
    public string? Name { get; set; }
    public string? FixedValue { get; set; }
    public string? DefaultValue { get; set; }
    public XmlSchemaType? SchemaType { get; set; }
}

public class XmlSchemaType : XmlSchemaObject
{
    public string? Name { get; set; }
}

public sealed class XmlSchemaSimpleType : XmlSchemaType
{
    public string? Datatype { get; set; }
}

public sealed class XmlSchemaAttribute : XmlSchemaObject
{
    public string? Name { get; set; }
    public string? FixedValue { get; set; }
}

public sealed class XmlSchemaSet : IEnumerable<XmlSchema>
{
    private readonly List<XmlSchema> _schemas = [];
    public int Count => _schemas.Count;
    public bool IsCompiled { get; private set; }
    public event ValidationEventHandler? ValidationEventHandler;

    public XmlSchema Add(XmlSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        _schemas.Add(schema);
        IsCompiled = false;
        return schema;
    }

    public XmlSchema Add(string? targetNamespace, XmlReader schemaReader)
    {
        var schema = XmlSchema.Read(schemaReader, Report) ?? throw new XmlSchemaException("The schema is empty.");
        schema.TargetNamespace = targetNamespace;
        return Add(schema);
    }

    public void Compile()
    {
        foreach (var schema in _schemas) schema.IsCompiled = true;
        IsCompiled = true;
    }

    public bool Contains(string? targetNamespace)
    {
        foreach (var schema in _schemas)
            if (StringComparer.Ordinal.Equals(schema.TargetNamespace ?? string.Empty, targetNamespace ?? string.Empty)) return true;
        return false;
    }

    public ICollection Schemas() => _schemas;
    public IEnumerator<XmlSchema> GetEnumerator() => _schemas.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal bool Validate(XmlElement root, ValidationEventHandler? callback)
    {
        foreach (var schema in _schemas)
        {
            if (schema.Elements.Count == 0 || schema.Elements.ContainsKey(root.LocalName)) return true;
        }
        var exception = new XmlSchemaException("The document element is not declared by the schema set.");
        callback?.Invoke(this, new ValidationEventArgs(exception.Message, exception));
        ValidationEventHandler?.Invoke(this, new ValidationEventArgs(exception.Message, exception));
        return false;
    }

    private void Report(object? sender, ValidationEventArgs args) => ValidationEventHandler?.Invoke(sender, args);
}

public sealed class XmlSchemaInference
{
    public XmlSchemaSet InferSchema(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var set = new XmlSchemaSet();
        var schema = new XmlSchema();
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && !schema.Elements.ContainsKey(reader.LocalName))
            {
                schema.Elements[reader.LocalName] = new XmlSchemaElement { Name = reader.LocalName };
            }
        }
        set.Add(schema);
        set.Compile();
        return set;
    }
}
