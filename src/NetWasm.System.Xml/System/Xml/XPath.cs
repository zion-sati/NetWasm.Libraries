// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file contains the reflection-free, in-memory XPath profile.  It keeps
// the public contracts small enough for the selected NetWasm XML surface while
// retaining ordinary XPath evaluation semantics for DOM-backed documents.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Xml.XPath;

using XmlNode = System.Xml.XmlNode;

public enum XPathNodeType
{
    Root,
    Element,
    Attribute,
    Namespace,
    Text,
    SignificantWhitespace,
    Whitespace,
    ProcessingInstruction,
    Comment,
    All,
}

public enum XPathResultType
{
    Number,
    String,
    Boolean,
    NodeSet,
    Navigator,
    Any,
    Error,
}

public abstract class XPathItem
{
    public abstract bool IsNode { get; }
    public abstract string Value { get; }
    public virtual object TypedValue => Value;
    public virtual Type ValueType => typeof(string);
    public virtual bool ValueAsBoolean => XPathConversions.ToBoolean(this);
    public virtual DateTime ValueAsDateTime => DateTime.Parse(Value, CultureInfo.InvariantCulture);
    public virtual double ValueAsDouble => XPathConversions.ToNumber(this);
    public virtual int ValueAsInt => checked((int)ValueAsDouble);
    public virtual long ValueAsLong => checked((long)ValueAsDouble);
    public virtual string ValueAsString => Value;
}

public abstract class XPathNodeIterator : IEnumerable<XPathNavigator>, IEnumerable
{
    public abstract XPathNavigator? Current { get; }
    public abstract int CurrentPosition { get; }
    public abstract XPathNodeIterator Clone();
    public abstract bool MoveNext();

    public IEnumerator<XPathNavigator> GetEnumerator()
    {
        var iterator = Clone();
        while (iterator.MoveNext() && iterator.Current is { } current)
        {
            yield return current.Clone();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class XPathExpression
{
    private readonly string _expression;
    private XmlNamespaceManager? _context;
    private XPathResultType _returnType;

    private XPathExpression(string expression)
    {
        _expression = expression;
        _returnType = XPathResultType.Any;
    }

    public string Expression => _expression;
    public XPathResultType ReturnType => _returnType;

    public static XPathExpression Compile(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        if (xpath.Trim().Length == 0)
        {
            throw new XPathException("The XPath expression cannot be empty.");
        }

        XPathEngine.Parse(xpath);
        return new XPathExpression(xpath);
    }

    public void SetContext(XmlNamespaceManager? nsManager) => _context = nsManager;
    public void SetContext(string prefix, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(namespaceURI);
        var manager = _context ?? new XmlNamespaceManager(new NameTable());
        manager.AddNamespace(prefix, namespaceURI);
        _context = manager;
    }

    internal XmlNamespaceManager? Context => _context;
}

public sealed class XPathException : SystemException
{
    public XPathException(string message) : base(message) { }
    public XPathException(string message, Exception innerException) : base(message, innerException) { }
}

public abstract class XsltContext : XmlNamespaceManager
{
    protected XsltContext(XmlNameTable nameTable) : base(nameTable) { }
    public abstract bool Whitespace { get; }
    public abstract int CompareDocument(string baseUri, string nextbaseUri);
    public abstract bool PreserveWhitespace(XmlNode node);
    public abstract IXsltContextFunction? ResolveFunction(string prefix, string name, XPathResultType[] argTypes);
    public abstract IXsltContextVariable? ResolveVariable(string prefix, string name);
}

public interface IXsltContextFunction
{
    XPathResultType[] ArgTypes { get; }
    XPathResultType ReturnType { get; }
    object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext);
}

public interface IXsltContextVariable
{
    bool IsLocal { get; }
    bool IsParam { get; }
    XPathResultType VariableType { get; }
    object Evaluate(XsltContext xsltContext, XPathNavigator contextNode);
}

public abstract class XPathNavigator : XPathItem, ICloneable
{
    public override bool IsNode => true;
    public abstract string BaseURI { get; }
    public abstract bool CanCanonicalize { get; }
    public abstract bool HasAttributes { get; }
    public abstract bool HasChildren { get; }
    public abstract bool IsEmptyElement { get; }
    public abstract string LocalName { get; }
    public abstract string Name { get; }
    public abstract string NamespaceURI { get; }
    public abstract XPathNodeType NodeType { get; }
    public abstract string Prefix { get; }
    public abstract string XmlLang { get; }
    public abstract XPathNavigator Clone();
    object ICloneable.Clone() => Clone();
    public abstract bool MoveTo(XPathNavigator other);
    public abstract bool MoveToFirstAttribute();
    public abstract bool MoveToNextAttribute();
    public abstract bool MoveToFirstChild();
    public abstract bool MoveToNext();
    public abstract bool MoveToParent();
    public abstract bool MoveToId(string id);
    public abstract bool MoveToFirstNamespace(System.Xml.XPath.XPathNamespaceScope scope);
    public abstract bool MoveToNextNamespace(System.Xml.XPath.XPathNamespaceScope scope);
    public abstract void MoveToRoot();

    public virtual XmlNamespaceManager? GetNamespace(string prefix) => null;
    public virtual XPathNodeIterator Select(string xpath) => Select(XPathExpression.Compile(xpath));
    public virtual XPathNodeIterator Select(XPathExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return XPathEngine.Select(this, expression);
    }

    public virtual object Evaluate(string xpath) => Evaluate(XPathExpression.Compile(xpath));
    public virtual object Evaluate(XPathExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return XPathEngine.Evaluate(this, expression);
    }

    public virtual XPathNodeIterator SelectChildren(string name, string namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(namespaceURI);
        var expression = XPathExpression.Compile("child::" + name);
        expression.SetContext(string.Empty, namespaceURI);
        return Select(expression);
    }

    public virtual XPathNodeIterator SelectAncestors(string name, string namespaceURI, bool matchSelf) =>
        Select(XPathExpression.Compile((matchSelf ? "ancestor-or-self::" : "ancestor::") + name));

    public virtual XmlReader ReadSubtree() => XmlReader.Create(new StringReader(Value));
    public virtual void WriteSubtree(XmlWriter writer) => writer.WriteString(Value);
}

public enum XPathNamespaceScope
{
    All,
    ExcludeXml,
    Local,
}

public sealed class XPathDocument
{
    private readonly System.Xml.XmlDocument _document;

    public XPathDocument(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _document = new System.Xml.XmlDocument();
        _document.Load(reader);
    }

    public XPathDocument(Stream stream) : this(XmlReader.Create(stream)) { }
    public XPathDocument(TextReader reader) : this(XmlReader.Create(reader)) { }
    public XPathNavigator CreateNavigator() => new DomXPathNavigator(_document);
}

internal sealed class ListXPathNodeIterator(IReadOnlyList<XPathNavigator> nodes) : XPathNodeIterator
{
    private readonly IReadOnlyList<XPathNavigator> _nodes = nodes;
    private int _index = -1;

    public override XPathNavigator? Current => _index >= 0 && _index < _nodes.Count ? _nodes[_index] : null;
    public override int CurrentPosition => _index + 1;
    public override XPathNodeIterator Clone()
    {
        var clone = new ListXPathNodeIterator(_nodes);
        clone._index = _index;
        return clone;
    }

    public override bool MoveNext()
    {
        if (_index + 1 >= _nodes.Count)
        {
            _index = _nodes.Count;
            return false;
        }

        _index++;
        return true;
    }
}

internal sealed class DomXPathNavigator : XPathNavigator
{
    private XmlNode _node;
    private XmlNode? _attribute;

    public DomXPathNavigator(XmlNode node)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    private XmlNode CurrentNode => _attribute ?? _node;
    internal XmlNode Node => CurrentNode;
    private XmlNode? Parent => _attribute is not null ? _node : CurrentNode.ParentNode;

    public override string BaseURI => CurrentNode.BaseURI;
    public override bool CanCanonicalize => false;
    public override bool HasAttributes => CurrentNode.Attributes is { Count: > 0 };
    public override bool HasChildren => CurrentNode.HasChildNodes;
    public override bool IsEmptyElement => CurrentNode is XmlElement element && !element.HasChildNodes;
    public override string LocalName => CurrentNode.LocalName;
    public override string Name => CurrentNode.Name;
    public override string NamespaceURI => CurrentNode.NamespaceURI;
    public override XPathNodeType NodeType => CurrentNode.NodeType switch
    {
        XmlNodeType.Document => XPathNodeType.Root,
        XmlNodeType.Attribute => XPathNodeType.Attribute,
        XmlNodeType.Text or XmlNodeType.CDATA => XPathNodeType.Text,
        XmlNodeType.Whitespace => XPathNodeType.Whitespace,
        XmlNodeType.SignificantWhitespace => XPathNodeType.SignificantWhitespace,
        XmlNodeType.Comment => XPathNodeType.Comment,
        XmlNodeType.ProcessingInstruction => XPathNodeType.ProcessingInstruction,
        _ => XPathNodeType.Element,
    };
    public override string Prefix => CurrentNode.Prefix;
    public override string Value => CurrentNode is XmlElement or XmlDocument ? CurrentNode.InnerText : CurrentNode.Value;
    public override string XmlLang => string.Empty;

    public override XPathNavigator Clone() => new DomXPathNavigator(_node) { _attribute = _attribute };
    public override bool MoveTo(XPathNavigator other)
    {
        if (other is not DomXPathNavigator candidate)
        {
            return false;
        }

        _node = candidate._node;
        _attribute = candidate._attribute;
        return true;
    }

    public override bool MoveToFirstAttribute()
    {
        if (CurrentNode.Attributes?.Item(0) is not XmlNode attribute)
        {
            return false;
        }

        _attribute = attribute;
        return true;
    }

    public override bool MoveToNextAttribute()
    {
        if (_attribute is null || _node.Attributes is not XmlAttributeCollection attributes)
        {
            return false;
        }

        for (var index = 0; index < attributes.Count - 1; index++)
        {
            if (ReferenceEquals(attributes.Item(index), _attribute))
            {
                _attribute = attributes.Item(index + 1);
                return _attribute is not null;
            }
        }

        return false;
    }

    public override bool MoveToFirstChild()
    {
        if (_attribute is not null || CurrentNode.FirstChild is not { } child)
        {
            return false;
        }

        _node = child;
        return true;
    }

    public override bool MoveToNext()
    {
        if (_attribute is not null || CurrentNode.NextSibling is not { } sibling)
        {
            return false;
        }

        _node = sibling;
        return true;
    }

    public override bool MoveToParent()
    {
        if (Parent is not { } parent)
        {
            return false;
        }

        _node = parent;
        _attribute = null;
        return true;
    }

    public override bool MoveToId(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        var nodes = XPathEngine.SelectNodes(_node, "//*[@id='" + XPathEngine.EscapeLiteral(id) + "']");
        if (nodes.Count == 0)
        {
            return false;
        }

        _node = nodes[0];
        _attribute = null;
        return true;
    }

    public override bool MoveToFirstNamespace(XPathNamespaceScope scope) => false;
    public override bool MoveToNextNamespace(XPathNamespaceScope scope) => false;
    public override void MoveToRoot()
    {
        _attribute = null;
        while (_node.ParentNode is { } parent)
        {
            _node = parent;
        }
    }
}

internal static class XPathConversions
{
    public static bool ToBoolean(object value) => value switch
    {
        bool boolean => boolean,
        XPathItem item => item.IsNode || item.Value.Length != 0,
        IReadOnlyList<XPathNavigator> nodes => nodes.Count != 0,
        double number => number != 0 && !double.IsNaN(number),
        string text => text.Length != 0,
        _ => value is not null,
    };

    public static double ToNumber(object value) => value switch
    {
        double number => number,
        float number => number,
        int number => number,
        long number => number,
        bool boolean => boolean ? 1 : 0,
        XPathItem item => ParseNumber(item.Value),
        IReadOnlyList<XPathNavigator> nodes => nodes.Count == 0 ? double.NaN : ParseNumber(nodes[0].Value),
        string text => ParseNumber(text),
        _ => double.NaN,
    };

    public static string ToString(object value) => value switch
    {
        string text => text,
        bool boolean => boolean ? "true" : "false",
        double number => number.ToString("G", CultureInfo.InvariantCulture),
        XPathItem item => item.Value,
        IReadOnlyList<XPathNavigator> nodes => nodes.Count == 0 ? string.Empty : nodes[0].Value,
        _ => value?.ToString() ?? string.Empty,
    };

    private static double ParseNumber(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN;
}

internal static class XPathEngine
{
    internal sealed class Context
    {
        public Context(XPathNavigator navigator, IReadOnlyList<XPathNavigator>? nodes = null, int position = 1, XmlNamespaceManager? namespaceManager = null)
        {
            Navigator = navigator;
            Nodes = nodes ?? [navigator];
            Position = position;
            NamespaceManager = namespaceManager;
        }

        public XPathNavigator Navigator { get; }
        public IReadOnlyList<XPathNavigator> Nodes { get; }
        public int Position { get; }
        public int Size => Nodes.Count;
        public XmlNamespaceManager? NamespaceManager { get; }
    }

    internal abstract class Expr
    {
        public abstract object Eval(Context context);
    }

    private sealed class LiteralExpr(object value) : Expr
    {
        private readonly object _value = value;
        public override object Eval(Context context) => _value;
    }

    private sealed class UnaryExpr(Expr inner) : Expr
    {
        public override object Eval(Context context) => -XPathConversions.ToNumber(inner.Eval(context));
    }

    private sealed class BinaryExpr(Expr left, string op, Expr right) : Expr
    {
        public override object Eval(Context context)
        {
            var a = left.Eval(context);
            var b = right.Eval(context);
            return op switch
            {
                "+" => XPathConversions.ToNumber(a) + XPathConversions.ToNumber(b),
                "-" => XPathConversions.ToNumber(a) - XPathConversions.ToNumber(b),
                "*" => XPathConversions.ToNumber(a) * XPathConversions.ToNumber(b),
                "div" => XPathConversions.ToNumber(a) / XPathConversions.ToNumber(b),
                "mod" => XPathConversions.ToNumber(a) % XPathConversions.ToNumber(b),
                "=" => Compare(a, b, equal: true),
                "!=" => !Compare(a, b, equal: true),
                "<" => XPathConversions.ToNumber(a) < XPathConversions.ToNumber(b),
                "<=" => XPathConversions.ToNumber(a) <= XPathConversions.ToNumber(b),
                ">" => XPathConversions.ToNumber(a) > XPathConversions.ToNumber(b),
                ">=" => XPathConversions.ToNumber(a) >= XPathConversions.ToNumber(b),
                "or" => XPathConversions.ToBoolean(a) || XPathConversions.ToBoolean(b),
                "and" => XPathConversions.ToBoolean(a) && XPathConversions.ToBoolean(b),
                _ => throw new XPathException("Unsupported XPath operator.")
            };
        }

        private static bool Compare(object left, object right, bool equal)
        {
            if (left is IReadOnlyList<XPathNavigator> leftNodes)
            {
                if (right is IReadOnlyList<XPathNavigator> rightNodes)
                {
                    foreach (var leftNode in leftNodes)
                        foreach (var rightNode in rightNodes)
                            if (StringComparer.Ordinal.Equals(leftNode.Value, rightNode.Value)) return equal;
                    return !equal;
                }

                var expected = right is bool ? XPathConversions.ToBoolean(right).ToString().ToLowerInvariant() :
                    right is double ? XPathConversions.ToNumber(right).ToString(CultureInfo.InvariantCulture) : XPathConversions.ToString(right);
                var found = false;
                foreach (var node in leftNodes)
                {
                    if (StringComparer.Ordinal.Equals(node.Value, expected))
                    {
                        found = true;
                        break;
                    }
                }

                return found == equal;
            }

            if (right is IReadOnlyList<XPathNavigator> rightNodeSet)
            {
                return Compare(rightNodeSet, left, equal);
            }

            if (left is bool || right is bool)
            {
                return (XPathConversions.ToBoolean(left) == XPathConversions.ToBoolean(right)) == equal;
            }

            if (left is double || right is double)
            {
                return (XPathConversions.ToNumber(left).Equals(XPathConversions.ToNumber(right))) == equal;
            }

            return StringComparer.Ordinal.Equals(XPathConversions.ToString(left), XPathConversions.ToString(right)) == equal;
        }
    }

    private sealed class FunctionExpr(string name, IReadOnlyList<Expr> args) : Expr
    {
        public override object Eval(Context context)
        {
            var values = new object[args.Count];
            for (var index = 0; index < values.Length; index++) values[index] = args[index].Eval(context);
            return name switch
            {
                "true" => true,
                "false" => false,
                "not" => !XPathConversions.ToBoolean(values[0]),
                "boolean" => XPathConversions.ToBoolean(values[0]),
                "number" => XPathConversions.ToNumber(values[0]),
                "string" => XPathConversions.ToString(values.Length == 0 ? context.Navigator : values[0]),
                "count" => values[0] is IReadOnlyList<XPathNavigator> set ? (double)set.Count : 0d,
                "position" => (double)context.Position,
                "last" => (double)context.Size,
                "name" => Name(context, values),
                "local-name" => LocalName(context, values),
                "namespace-uri" => Namespace(context, values),
                "contains" => XPathConversions.ToString(values[0]).Contains(XPathConversions.ToString(values[1]), StringComparison.Ordinal),
                "starts-with" => XPathConversions.ToString(values[0]).StartsWith(XPathConversions.ToString(values[1]), StringComparison.Ordinal),
                "concat" => Concat(values),
                "string-length" => (double)XPathConversions.ToString(values.Length == 0 ? context.Navigator : values[0]).Length,
                "normalize-space" => NormalizeSpace(XPathConversions.ToString(values.Length == 0 ? context.Navigator : values[0])),
                "substring" => Substring(values),
                "sum" => Sum(values),
                _ => throw new XPathException("Unsupported XPath function: " + name),
            };
        }

        private static string Name(Context context, object[] values) => Navigator(values, context).Name;
        private static string LocalName(Context context, object[] values) => Navigator(values, context).LocalName;
        private static string Namespace(Context context, object[] values) => Navigator(values, context).NamespaceURI;
        private static XPathNavigator Navigator(object[] values, Context context) => values.Length != 0 && values[0] is IReadOnlyList<XPathNavigator> set && set.Count != 0 ? set[0] : context.Navigator;
        private static string Concat(object[] values)
        {
            var builder = new StringBuilder();
            foreach (var value in values) builder.Append(XPathConversions.ToString(value));
            return builder.ToString();
        }

        private static string NormalizeSpace(string value)
        {
            var builder = new StringBuilder();
            var pendingSpace = false;
            foreach (var character in value.Trim())
            {
                if (char.IsWhiteSpace(character)) pendingSpace = true;
                else
                {
                    if (pendingSpace && builder.Length != 0) builder.Append(' ');
                    builder.Append(character);
                    pendingSpace = false;
                }
            }
            return builder.ToString();
        }

        private static string Substring(object[] values)
        {
            var value = XPathConversions.ToString(values[0]);
            var start = Math.Max(0, (int)Math.Round(XPathConversions.ToNumber(values[1])) - 1);
            if (start >= value.Length) return string.Empty;
            if (values.Length < 3) return value.Substring(start);
            var length = Math.Max(0, (int)Math.Round(XPathConversions.ToNumber(values[2])));
            return value.Substring(start, Math.Min(length, value.Length - start));
        }

        private static double Sum(object[] values)
        {
            if (values.Length == 0 || values[0] is not IReadOnlyList<XPathNavigator> nodes) return 0;
            var total = 0d;
            foreach (var node in nodes) total += XPathConversions.ToNumber(node.Value);
            return total;
        }
    }

    private sealed class PathExpr(IReadOnlyList<Step> steps, bool absolute) : Expr
    {
        public override object Eval(Context context)
        {
            var current = new List<XPathNavigator>();
            var root = context.Navigator.Clone();
            root.MoveToRoot();
            current.Add(absolute ? root : context.Navigator.Clone());
            foreach (var step in steps)
            {
                var next = new List<XPathNavigator>();
                foreach (var node in current)
                {
                    foreach (var candidate in step.Select(node, context.NamespaceManager)) next.Add(candidate);
                }

                current = Distinct(next);
            }
            return current;
        }
    }

    private sealed class Step(string axis, string test, IReadOnlyList<Expr> predicates)
    {
        public IEnumerable<XPathNavigator> Select(XPathNavigator source, XmlNamespaceManager? namespaceManager)
        {
            var output = new List<XPathNavigator>();
            switch (axis)
            {
                case "self": output.Add(source.Clone()); break;
                case "parent":
                    var parent = source.Clone(); if (parent.MoveToParent()) output.Add(parent); break;
                case "attribute":
                    var attribute = source.Clone();
                    if (attribute.MoveToFirstAttribute())
                    {
                        do output.Add(attribute.Clone()); while (attribute.MoveToNextAttribute());
                    }
                    break;
                case "ancestor":
                case "ancestor-or-self":
                    var ancestor = source.Clone();
                    if (axis == "ancestor-or-self") output.Add(ancestor.Clone());
                    while (ancestor.MoveToParent()) output.Insert(0, ancestor.Clone());
                    break;
                case "descendant": output.AddRange(Descendants(source, includeSelf: false)); break;
                case "descendant-or-self": output.AddRange(Descendants(source, includeSelf: true)); break;
                case "following-sibling":
                    var sibling = source.Clone(); while (sibling.MoveToNext()) output.Add(sibling.Clone()); break;
                case "preceding-sibling":
                    var preceding = source.Clone(); var reverse = new List<XPathNavigator>();
                    while (preceding.MoveToParent()) { var parentNode = preceding.Clone(); if (!parentNode.MoveToFirstChild()) break; while (parentNode.MoveToNext()) { if (StringComparer.Ordinal.Equals(parentNode.Name, source.Name)) reverse.Add(parentNode.Clone()); } break; }
                    output.AddRange(reverse);
                    break;
                case "child":
                default:
                    var child = source.Clone(); if (child.MoveToFirstChild()) { do output.Add(child.Clone()); while (child.MoveToNext()); }
                    break;
            }

            var matching = new List<XPathNavigator>();
            foreach (var candidate in output)
            {
                if (Matches(candidate, namespaceManager)) matching.Add(candidate);
            }

            return ApplyPredicates(matching);
        }

        public bool Matches(XPathNavigator navigator, XmlNamespaceManager? namespaceManager)
        {
            if (test == "*" || test == "node()") return true;
            if (test == "text()") return navigator.NodeType is XPathNodeType.Text or XPathNodeType.Whitespace or XPathNodeType.SignificantWhitespace;
            if (test == "comment()") return navigator.NodeType == XPathNodeType.Comment;
            if (test.StartsWith("processing-instruction", StringComparison.Ordinal)) return navigator.NodeType == XPathNodeType.ProcessingInstruction;
            if (test.StartsWith("@", StringComparison.Ordinal)) test = test.Substring(1);
            var separator = test.IndexOf(':');
            if (separator >= 0)
            {
                var prefix = test.Substring(0, separator);
                var localName = test.Substring(separator + 1);
                if (namespaceManager?.LookupNamespace(prefix) is not { } uri) return false;
                return (localName == "*" || StringComparer.Ordinal.Equals(navigator.LocalName, localName)) &&
                    StringComparer.Ordinal.Equals(navigator.NamespaceURI, uri);
            }

            return navigator.NodeType == XPathNodeType.Element || navigator.NodeType == XPathNodeType.Attribute ?
                StringComparer.Ordinal.Equals(navigator.Name, test) || StringComparer.Ordinal.Equals(navigator.LocalName, test) : false;
        }

        private IEnumerable<XPathNavigator> ApplyPredicates(List<XPathNavigator> source)
        {
            var current = source;
            foreach (var predicate in predicates)
            {
                var filtered = new List<XPathNavigator>();
                for (var index = 0; index < current.Count; index++)
                {
                    var value = predicate.Eval(new Context(current[index], current, index + 1));
                    if (value is double number ? number == index + 1 : XPathConversions.ToBoolean(value)) filtered.Add(current[index]);
                }
                current = filtered;
            }
            return current;
        }

        private static IEnumerable<XPathNavigator> Descendants(XPathNavigator source, bool includeSelf)
        {
            if (includeSelf) yield return source.Clone();
            var child = source.Clone();
            if (!child.MoveToFirstChild()) yield break;
            do
            {
                yield return child.Clone();
                foreach (var descendant in Descendants(child, includeSelf: false)) yield return descendant;
            } while (child.MoveToNext());
        }
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;
        public Parser(string text) => _text = text;

        public Expr ParseExpression()
        {
            var expression = ParseUnion();
            Skip();
            if (_position != _text.Length) throw Error();
            return expression;
        }

        private Expr ParseUnion()
        {
            var left = ParseOr();
            while (Take('|')) left = new UnionExpr(left, ParseOr());
            return left;
        }

        private Expr ParseOr() => ParseBinary(ParseAnd, "or");
        private Expr ParseAnd() => ParseBinary(ParseComparison, "and");
        private Expr ParseComparison()
        {
            var left = ParseAdditive();
            foreach (var op in new[] { "!=", "<=", ">=", "=", "<", ">" })
            {
                if (Take(op)) return new BinaryExpr(left, op, ParseAdditive());
            }
            return left;
        }

        private Expr ParseAdditive() => ParseBinary(ParseMultiplicative, "+", "-");
        private Expr ParseMultiplicative() => ParseBinary(ParseUnary, "*", "div", "mod");
        private Expr ParseBinary(Func<Expr> operand, params string[] operators)
        {
            var left = operand();
            while (true)
            {
                var found = string.Empty;
                foreach (var op in operators) if (Take(op)) { found = op; break; }
                if (found.Length == 0) return left;
                left = new BinaryExpr(left, found, operand());
            }
        }

        private Expr ParseUnary()
        {
            if (Take('-')) return new UnaryExpr(ParseUnary());
            return ParsePrimary();
        }

        private Expr ParsePrimary()
        {
            Skip();
            if (Take('(')) { var inner = ParseUnion(); Expect(')'); return inner; }
            if (PeekQuote(out var quote)) return new LiteralExpr(ReadQuoted(quote));
            if (ReadNumber(out var number)) return new LiteralExpr(number);
            var functionStart = _position;
            var functionName = ReadTokenName();
            if (IsFunction(functionName) && Take('('))
            {
                var args = new List<Expr>();
                if (!Take(')'))
                {
                    do args.Add(ParseUnion()); while (Take(','));
                    Expect(')');
                }

                return new FunctionExpr(functionName, args);
            }

            _position = functionStart;
            return ParsePath();
        }

        private static bool IsFunction(string name) => name is "true" or "false" or "not" or "boolean" or "number" or "string" or "count" or "position" or "last" or "name" or "local-name" or "namespace-uri" or "contains" or "starts-with" or "concat" or "string-length" or "normalize-space" or "substring" or "sum";

        private Expr ParsePath()
        {
            var descendant = Take("//");
            var absolute = descendant || Take('/');
            var steps = new List<Step>();
            if (descendant) steps.Add(new Step("descendant-or-self", "node()", []));
            if (absolute && _position == _text.Length) return new PathExpr(steps, true);
            while (true)
            {
                var axis = "child";
                if (Take('.')) { steps.Add(new Step("self", "node()", [])); }
                else if (Take("..")) { steps.Add(new Step("parent", "node()", [])); }
                else
                {
                    var name = ReadName();
                    if (name.Length == 0) throw Error();
                    if (Take("::")) { axis = name; name = ReadName(); }
                    else if (name.StartsWith("@", StringComparison.Ordinal)) { axis = "attribute"; name = name.Substring(1); }
                    var predicates = new List<Expr>();
                    while (Take('[')) { predicates.Add(ParseUnion()); Expect(']'); }
                    steps.Add(new Step(axis, name, predicates));
                }

                if (Take("//")) { steps.Add(new Step("descendant-or-self", "node()", [])); continue; }
                if (!Take('/')) break;
            }
            return new PathExpr(steps, absolute);
        }

        private string ReadName()
        {
            Skip(); var start = _position;
            if (Take('@')) { }
            while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || "_-.::*".Contains(_text[_position], StringComparison.Ordinal))) _position++;
            var name = _text.Substring(start, _position - start);
            Skip();
            if (_position < _text.Length && _text[_position] == '(')
            {
                _position++;
                var depth = 1;
                while (_position < _text.Length && depth != 0)
                {
                    if (_text[_position] == '(') depth++;
                    else if (_text[_position] == ')') depth--;
                    _position++;
                }

                if (depth != 0) throw Error();
                return _text.Substring(start, _position - start);
            }
            return name;
        }

        private string ReadTokenName()
        {
            var start = _position; while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || "-_".Contains(_text[_position], StringComparison.Ordinal))) _position++; return _text.Substring(start, _position - start);
        }

        private bool PeekQuote(out char quote)
        {
            Skip(); quote = _position < _text.Length ? _text[_position] : '\0'; return quote is '\'' or '"';
        }

        private string ReadQuoted(char quote)
        {
            _position++; var start = _position; while (_position < _text.Length && _text[_position] != quote) _position++; if (_position >= _text.Length) throw Error(); var value = _text.Substring(start, _position - start); _position++; return value;
        }

        private bool ReadNumber(out double value)
        {
            Skip();
            var start = _position;
            if (_position >= _text.Length || (!char.IsDigit(_text[_position]) && (_text[_position] != '.' || _position + 1 >= _text.Length || !char.IsDigit(_text[_position + 1]))))
            {
                value = 0;
                return false;
            }

            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.')) _position++;
            return double.TryParse(_text.Substring(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private bool Take(char character) { Skip(); if (_position < _text.Length && _text[_position] == character) { _position++; return true; } return false; }
        private bool Take(string token) { Skip(); if (_text.Substring(_position).StartsWith(token, StringComparison.Ordinal)) { _position += token.Length; return true; } return false; }
        private void Expect(char character) { if (!Take(character)) throw Error(); }
        private void Skip() { while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++; }
        private XPathException Error() => new("Invalid XPath expression.");
    }

    private sealed class UnionExpr(Expr left, Expr right) : Expr
    {
        public override object Eval(Context context)
        {
            var output = new List<XPathNavigator>();
            if (left.Eval(context) is IReadOnlyList<XPathNavigator> a) output.AddRange(a);
            if (right.Eval(context) is IReadOnlyList<XPathNavigator> b) output.AddRange(b);
            return Distinct(output);
        }
    }

    internal static Expr Parse(string xpath) => new Parser(xpath).ParseExpression();
    internal static XPathNodeIterator Select(XPathNavigator navigator, XPathExpression expression)
    {
        var value = Parse(expression.Expression).Eval(new Context(navigator, namespaceManager: expression.Context));
        var nodes = value as IReadOnlyList<XPathNavigator> ?? [];
        return new ListXPathNodeIterator(nodes);
    }

    internal static object Evaluate(XPathNavigator navigator, XPathExpression expression) => Parse(expression.Expression).Eval(new Context(navigator, namespaceManager: expression.Context));

    internal static IReadOnlyList<XmlNode> SelectNodes(XmlNode node, string expression)
    {
        var navigator = new DomXPathNavigator(node);
        var result = Select(navigator, XPathExpression.Compile(expression));
        var nodes = new List<XmlNode>();
        while (result.MoveNext() && result.Current is DomXPathNavigator current)
        {
            nodes.Add(current.Node);
        }
        return nodes;
    }

    internal static string EscapeLiteral(string value) => value.Replace("'", "&apos;", StringComparison.Ordinal);

    private static List<XPathNavigator> Distinct(IEnumerable<XPathNavigator> nodes)
    {
        var result = new List<XPathNavigator>();
        foreach (var node in nodes)
        {
            var duplicate = false;
            foreach (var existing in result)
            {
                if (existing.IsSamePosition(node))
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate) result.Add(node);
        }
        return result;
    }
}

internal static class XPathNavigatorExtensions
{
    public static bool IsSamePosition(this XPathNavigator left, XPathNavigator right)
    {
        var a = left.Clone(); var b = right.Clone();
        if (a.NodeType != b.NodeType || !StringComparer.Ordinal.Equals(a.Name, b.Name) || !StringComparer.Ordinal.Equals(a.Value, b.Value)) return false;
        return StringComparer.Ordinal.Equals(a.NamespaceURI, b.NamespaceURI);
    }
}
