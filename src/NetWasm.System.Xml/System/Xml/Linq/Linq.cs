// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Xml.Linq;

public sealed class XNamespace : IEquatable<XNamespace>
{
    private static readonly Dictionary<string, XNamespace> s_cache = new(StringComparer.Ordinal);
    private XNamespace(string namespaceName) => NamespaceName = namespaceName;
    public string NamespaceName { get; }
    public static XNamespace None => Get(string.Empty);
    public static XNamespace Xml => Get("http://www.w3.org/XML/1998/namespace");
    public static XNamespace Get(string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);
        if (!s_cache.TryGetValue(namespaceName, out var value)) s_cache[namespaceName] = value = new XNamespace(namespaceName);
        return value;
    }
    public static implicit operator XNamespace(string namespaceName) => Get(namespaceName);
    public XName GetName(string localName) => XName.Get(localName, NamespaceName);
    public static XName operator +(XNamespace ns, string localName) => ns.GetName(localName);
    public bool Equals(XNamespace? other) => other is not null && StringComparer.Ordinal.Equals(NamespaceName, other.NamespaceName);
    public override bool Equals(object? obj) => obj is XNamespace other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(NamespaceName);
    public override string ToString() => NamespaceName;
}

public sealed class XName : IEquatable<XName>
{
    private static readonly Dictionary<string, XName> s_cache = new(StringComparer.Ordinal);
    private XName(string localName, XNamespace ns) { LocalName = localName; Namespace = ns; }
    public string LocalName { get; }
    public XNamespace Namespace { get; }
    public string NamespaceName => Namespace.NamespaceName;
    public string ExpandedName => NamespaceName.Length == 0 ? LocalName : "{" + NamespaceName + "}" + LocalName;
    public static XName Get(string expandedName)
    {
        ArgumentNullException.ThrowIfNull(expandedName);
        if (expandedName.StartsWith("{", StringComparison.Ordinal))
        {
            var close = expandedName.IndexOf('}');
            if (close > 0) return Get(expandedName.Substring(close + 1), expandedName.Substring(1, close - 1));
        }
        return Get(expandedName, string.Empty);
    }
    public static XName Get(string localName, string namespaceName) => Get(localName, XNamespace.Get(namespaceName));
    internal static XName Get(string localName, XNamespace ns)
    {
        ArgumentNullException.ThrowIfNull(localName);
        var key = ns.NamespaceName + "\0" + localName;
        if (!s_cache.TryGetValue(key, out var value)) s_cache[key] = value = new XName(localName, ns);
        return value;
    }
    public static implicit operator XName(string expandedName) => Get(expandedName);
    public bool Equals(XName? other) => other is not null && StringComparer.Ordinal.Equals(LocalName, other.LocalName) && Namespace.Equals(other.Namespace);
    public override bool Equals(object? obj) => obj is XName other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(LocalName, Namespace);
    public override string ToString() => ExpandedName;
}

public abstract class XObject
{
    private Dictionary<Type, object?>? _annotations;
    public object? Annotation(Type type) => _annotations is not null && _annotations.TryGetValue(type, out var value) ? value : null;
    public T? Annotation<T>() => (T?)Annotation(typeof(T));
    public void AddAnnotation(object annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        (_annotations ??= new Dictionary<Type, object?>())[annotation.GetType()] = annotation;
    }
    public void RemoveAnnotations<T>() => _annotations?.Remove(typeof(T));
}

public abstract class XNode : XObject
{
    internal XElement? ParentElement { get; set; }
    public XElement? Parent => ParentElement;
    public abstract string ToString(SaveOptions options);
    public override string ToString() => ToString(SaveOptions.None);
    public abstract void WriteTo(XmlWriter writer);
}

public sealed class XAttribute : XObject
{
    public XAttribute(XName name, object? value) { Name = name ?? throw new ArgumentNullException(nameof(name)); Value = ConvertValue(value); }
    public XAttribute(string name, object? value) : this(XName.Get(name), value) { }
    public XName Name { get; }
    public string Value { get; set; }
    public bool IsNamespaceDeclaration => Name.LocalName == "xmlns" || Name.NamespaceName == "http://www.w3.org/2000/xmlns/";
    public XElement? Parent { get; internal set; }
    public override string ToString() => Name + "=\"" + Value.Replace("\"", "&quot;", StringComparison.Ordinal) + "\"";
    private static string ConvertValue(object? value) => value switch { null => string.Empty, string text => text, _ => value.ToString() ?? string.Empty };
}

public sealed class XElement : XNode, IEnumerable<XElement>
{
    private readonly List<XNode> _nodes = [];
    private readonly List<XAttribute> _attributes = [];
    public XElement(XName name, params object?[] content) { Name = name ?? throw new ArgumentNullException(nameof(name)); Add(content); }
    public XElement(string name, params object?[] content) : this(XName.Get(name), content) { }
    public XName Name { get; }
    public IEnumerable<XNode> Nodes() => _nodes.ToArray();
    public IEnumerable<XElement> Elements() { foreach (var node in _nodes) if (node is XElement element) yield return element; }
    public IEnumerable<XElement> Elements(XName name) { foreach (var element in Elements()) if (element.Name.Equals(name)) yield return element; }
    public IEnumerable<XElement> Descendants() { foreach (var element in Elements()) { yield return element; foreach (var descendant in element.Descendants()) yield return descendant; } }
    public IEnumerable<XElement> Descendants(XName name) { foreach (var element in Descendants()) if (element.Name.Equals(name)) yield return element; }
    public IEnumerable<XAttribute> Attributes() => _attributes.ToArray();
    public XAttribute? Attribute(XName name) { foreach (var attribute in _attributes) if (attribute.Name.Equals(name)) return attribute; return null; }
    public string Value { get { var builder = new StringBuilder(); AppendText(this, builder); return builder.ToString(); } set { _nodes.Clear(); Add(value); } }
    public void Add(params object?[] content)
    {
        foreach (var item in content)
        {
            switch (item)
            {
                case null: break;
                case XAttribute attribute: attribute.Parent = this; _attributes.Add(attribute); break;
                case XNode node: node.ParentElement = this; _nodes.Add(node); break;
                case IEnumerable<XNode> nodes: foreach (var child in nodes) Add(child); break;
                default: _nodes.Add(new XText(item.ToString() ?? string.Empty) { ParentElement = this }); break;
            }
        }
    }
    public void SetAttributeValue(XName name, object? value) { var attribute = Attribute(name); if (value is null) { if (attribute is not null) _attributes.Remove(attribute); } else if (attribute is null) Add(new XAttribute(name, value)); else attribute.Value = value.ToString() ?? string.Empty; }
    public static XElement Parse(string text) { var document = new XmlDocument(); document.LoadXml(text); return FromXmlElement(document.DocumentElement!); }
    public static XElement Load(XmlReader reader) { var document = new XmlDocument(); document.Load(reader); return FromXmlElement(document.DocumentElement!); }
    public override string ToString(SaveOptions options) { using var text = new StringWriter(); using var writer = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true }); WriteTo(writer); writer.Flush(); return text.ToString(); }
    public override void WriteTo(XmlWriter writer) { writer.WriteStartElement(Name.NamespaceName.Length == 0 ? string.Empty : Name.NamespaceName, Name.LocalName); foreach (var attribute in _attributes) writer.WriteAttributeString(attribute.Name.LocalName, attribute.Value); foreach (var node in _nodes) node.WriteTo(writer); writer.WriteEndElement(); }
    public IEnumerator<XElement> GetEnumerator() => Elements().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal static XElement FromXmlElement(XmlElement element)
    {
        var result = new XElement(XName.Get(element.LocalName, element.NamespaceURI));
        if (element.Attributes is XmlAttributeCollection attributes) for (var index = 0; index < attributes.Count; index++) if (attributes.Item(index) is XmlAttribute attribute) result.Add(new XAttribute(XName.Get(attribute.LocalName, attribute.NamespaceURI), attribute.Value));
        foreach (var child in element.ChildNodes) switch (child)
            {
                case XmlElement childElement: result.Add(FromXmlElement(childElement)); break;
                case XmlCDataSection cdata: result.Add(new XCData(cdata.Value)); break;
                case XmlComment comment: result.Add(new XComment(comment.Value)); break;
                case XmlText text: result.Add(new XText(text.Value)); break;
            }
        return result;
    }
    private static void AppendText(XElement element, StringBuilder builder) { foreach (var node in element._nodes) if (node is XText text) builder.Append(text.Value); else if (node is XElement child) AppendText(child, builder); }
}

public sealed class XDocument : XNode
{
    public XDocument(params object?[] content) { foreach (var item in content) if (item is XElement element) { Root = element; element.ParentElement = null; } }
    public XElement? Root { get; private set; }
    public static XDocument Parse(string text) => new(XElement.Parse(text));
    public static XDocument Load(XmlReader reader) => new(XElement.Load(reader));
    public void Save(Stream stream) { using var writer = XmlWriter.Create(stream); WriteTo(writer); writer.Flush(); }
    public void Save(TextWriter writer) { using var output = XmlWriter.Create(writer); WriteTo(output); output.Flush(); }
    public override string ToString(SaveOptions options) => Root?.ToString(options) ?? string.Empty;
    public override void WriteTo(XmlWriter writer) => Root?.WriteTo(writer);
}

public class XText : XNode
{
    public XText(string value) => Value = value ?? string.Empty;
    public string Value { get; set; }
    public override string ToString(SaveOptions options) => Value;
    public override void WriteTo(XmlWriter writer) => writer.WriteString(Value);
}
public sealed class XCData : XText
{
    public XCData(string value) : base(value) { }
    public override void WriteTo(XmlWriter writer) => writer.WriteCData(Value);
}
public sealed class XComment : XNode
{
    public XComment(string value) => Value = value ?? string.Empty;
    public string Value { get; }
    public override string ToString(SaveOptions options) => "<!--" + Value + "-->";
    public override void WriteTo(XmlWriter writer) => writer.WriteComment(Value);
}
public sealed class XProcessingInstruction : XNode
{
    public XProcessingInstruction(string target, string data) { Target = target; Data = data; }
    public string Target { get; }
    public string Data { get; set; }
    public override string ToString(SaveOptions options) => "<?" + Target + " " + Data + "?>";
    public override void WriteTo(XmlWriter writer) => writer.WriteProcessingInstruction(Target, Data);
}
public sealed class XStreamingElement : XNode
{
    private readonly object?[] _content;
    public XStreamingElement(XName name, params object?[] content) { Name = name; _content = content; }
    public XName Name { get; }
    public void Add(params object?[] content) { var merged = new object?[_content.Length + content.Length]; Array.Copy(_content, merged, _content.Length); Array.Copy(content, 0, merged, _content.Length, content.Length); }
    public override string ToString(SaveOptions options) { var element = new XElement(Name, _content); return element.ToString(options); }
    public override void WriteTo(XmlWriter writer) => new XElement(Name, _content).WriteTo(writer);
}

public enum SaveOptions { None, DisableFormatting, OmitDuplicateNamespaces }
