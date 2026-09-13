// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The DOM shape follows dotnet/runtime's System.Private.Xml sources at
// 811225a482702af7ecc35d817966bc70b88a3a23.  NetWasm keeps the managed tree
// and reader/writer bridges, while deliberately omitting URI/file factories,
// reflection and ambient resource access.  Loads and saves consume only the
// application-owned streams, readers and writers supplied by the caller.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.XPath;
using System.Xml.Schema;

namespace System.Xml;

public enum XmlNodeChangedAction
{
    Insert,
    Remove,
    Change,
}

public delegate void XmlNodeChangedEventHandler(object? sender, XmlNodeChangedEventArgs e);

public sealed class XmlNodeChangedEventArgs : EventArgs
{
    public XmlNodeChangedEventArgs(XmlNodeChangedAction action, XmlNode? node, XmlNode? oldParent, XmlNode? newParent)
    {
        Action = action;
        Node = node;
        OldParent = oldParent;
        NewParent = newParent;
    }

    public XmlNodeChangedAction Action { get; }
    public XmlNode? Node { get; }
    public XmlNode? OldParent { get; }
    public XmlNode? NewParent { get; }
}

public abstract class XmlNodeList : IEnumerable<XmlNode>, IEnumerable, IDisposable
{
    public abstract int Count { get; }
    public abstract XmlNode? Item(int index);
    [IndexerName("ItemOf")]
    public XmlNode? this[int index] => Item(index);

    public IEnumerator<XmlNode> GetEnumerator()
    {
        for (var index = 0; index < Count; index++)
        {
            var item = Item(index);
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public virtual void Dispose() { }
}

internal sealed class SnapshotXmlNodeList(IList<XmlNode> nodes) : XmlNodeList
{
    private readonly IList<XmlNode> _nodes = nodes;
    public override int Count => _nodes.Count;
    public override XmlNode? Item(int index) => index >= 0 && index < _nodes.Count ? _nodes[index] : null;
}

public abstract class XmlNamedNodeMap : IEnumerable<XmlNode>, IEnumerable
{
    public abstract int Count { get; }
    public abstract XmlNode? GetNamedItem(string name);
    public abstract XmlNode? Item(int index);
    [IndexerName("ItemOf")]
    public XmlNode? this[int index] => Item(index);

    public IEnumerator<XmlNode> GetEnumerator()
    {
        for (var index = 0; index < Count; index++)
        {
            var item = Item(index);
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class XmlAttributeCollection : XmlNamedNodeMap
{
    private readonly List<XmlAttribute> _items = [];
    private readonly XmlElement _owner;

    internal XmlAttributeCollection(XmlElement owner) => _owner = owner;

    public override int Count => _items.Count;
    public override XmlNode? Item(int index) => index >= 0 && index < _items.Count ? _items[index] : null;

    public override XmlNode? GetNamedItem(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        for (var index = 0; index < _items.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(_items[index].Name, name))
            {
                return _items[index];
            }
        }

        return null;
    }

    public XmlAttribute? GetNamedItem(string localName, string? namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(localName);
        var ns = namespaceURI ?? string.Empty;
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            if (StringComparer.Ordinal.Equals(item.LocalName, localName) &&
                StringComparer.Ordinal.Equals(item.NamespaceURI, ns))
            {
                return item;
            }
        }

        return null;
    }

    public XmlAttribute? SetNamedItem(XmlAttribute node)
    {
        ArgumentNullException.ThrowIfNull(node);
        for (var index = 0; index < _items.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(_items[index].Name, node.Name))
            {
                var old = _items[index];
                _items[index] = node;
                node.SetParent(_owner);
                old.SetParent(null);
                _owner.Document.NotifyChanged(XmlNodeChangedAction.Change, node, _owner, _owner);
                return old;
            }
        }

        _items.Add(node);
        node.SetParent(_owner);
        _owner.Document.NotifyChanged(XmlNodeChangedAction.Insert, node, null, _owner);
        return null;
    }

    public XmlAttribute? RemoveNamedItem(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        for (var index = 0; index < _items.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(_items[index].Name, name))
            {
                var old = _items[index];
                _items.RemoveAt(index);
                old.SetParent(null);
                _owner.Document.NotifyChanged(XmlNodeChangedAction.Remove, old, _owner, null);
                return old;
            }
        }

        throw new KeyNotFoundException("The requested XML attribute does not exist.");
    }

    internal void AddLoaded(XmlAttribute attribute)
    {
        _items.Add(attribute);
        attribute.SetParent(_owner);
    }

    internal void CopyTo(XmlAttributeCollection target)
    {
        for (var index = 0; index < _items.Count; index++)
        {
            var source = _items[index];
            var copy = target._owner.Document.CreateAttribute(source.Prefix, source.LocalName, source.NamespaceURI);
            copy.Value = source.Value;
            target.AddLoaded(copy);
        }
    }
}

public abstract class XmlNode
{
    private readonly List<XmlNode> _children = [];
    private XmlNode? _parent;

    protected XmlNode(XmlDocument? ownerDocument) => OwnerDocument = ownerDocument;

    public virtual string Name => string.Empty;
    public virtual string Value { get => InnerText; set => InnerText = value; }
    public virtual string LocalName => Name;
    public virtual string NamespaceURI => string.Empty;
    public virtual string Prefix => string.Empty;
    public virtual XmlNodeType NodeType => XmlNodeType.None;
    public XmlDocument Document => this as XmlDocument ?? OwnerDocument ?? throw new InvalidOperationException("The node is not attached to a document.");
    public XmlDocument? OwnerDocument { get; internal set; }
    public XmlNode? ParentNode => _parent;
    public XmlNode? FirstChild => _children.Count == 0 ? null : _children[0];
    public XmlNode? LastChild => _children.Count == 0 ? null : _children[^1];
    public bool HasChildNodes => _children.Count != 0;
    public XmlNodeList ChildNodes => new SnapshotXmlNodeList(_children);
    public virtual XmlNamedNodeMap? Attributes => null;
    public XmlNode? NextSibling => Sibling(1);
    public XmlNode? PreviousSibling => Sibling(-1);
    public virtual string BaseURI => string.Empty;

    public virtual string InnerText
    {
        get
        {
            var builder = new StringBuilder();
            AppendText(builder);
            return builder.ToString();
        }
        set
        {
            RemoveAllChildren();
            if (!string.IsNullOrEmpty(value))
            {
                AppendChild(Document.CreateTextNode(value));
            }
        }
    }

    public virtual string OuterXml
    {
        get
        {
            using var output = new StringWriter();
            using var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true });
            WriteTo(writer);
            writer.Flush();
            return output.ToString();
        }
    }

    public event XmlNodeChangedEventHandler? NodeChanging;
    public event XmlNodeChangedEventHandler? NodeChanged;
    public event XmlNodeChangedEventHandler? NodeInserting;
    public event XmlNodeChangedEventHandler? NodeInserted;
    public event XmlNodeChangedEventHandler? NodeRemoving;
    public event XmlNodeChangedEventHandler? NodeRemoved;

    public virtual XmlNode AppendChild(XmlNode newChild)
    {
        ArgumentNullException.ThrowIfNull(newChild);
        EnsureCanInsert(newChild);
        if (newChild.ParentNode is not null)
        {
            newChild.ParentNode.RemoveChild(newChild);
        }

        var document = Document;
        var args = new XmlNodeChangedEventArgs(XmlNodeChangedAction.Insert, newChild, null, this);
        NodeInserting?.Invoke(this, args);
        document.NotifyChanging(args);
        newChild.OwnerDocument = document;
        newChild._parent = this;
        _children.Add(newChild);
        NodeInserted?.Invoke(this, args);
        document.NotifyChanged(args);
        return newChild;
    }

    public virtual XmlNode InsertBefore(XmlNode newChild, XmlNode? referenceChild)
    {
        ArgumentNullException.ThrowIfNull(newChild);
        EnsureCanInsert(newChild);
        if (referenceChild is null)
        {
            return AppendChild(newChild);
        }

        var index = _children.IndexOf(referenceChild);
        if (index < 0)
        {
            throw new ArgumentException("The reference child is not part of this node.", nameof(referenceChild));
        }

        if (newChild.ParentNode is not null)
        {
            newChild.ParentNode.RemoveChild(newChild);
        }

        var document = Document;
        var args = new XmlNodeChangedEventArgs(XmlNodeChangedAction.Insert, newChild, null, this);
        NodeInserting?.Invoke(this, args);
        document.NotifyChanging(args);
        newChild.OwnerDocument = document;
        newChild._parent = this;
        _children.Insert(index, newChild);
        NodeInserted?.Invoke(this, args);
        document.NotifyChanged(args);
        return newChild;
    }

    public virtual XmlNode RemoveChild(XmlNode oldChild)
    {
        ArgumentNullException.ThrowIfNull(oldChild);
        var index = _children.IndexOf(oldChild);
        if (index < 0)
        {
            throw new ArgumentException("The node is not a child of this node.", nameof(oldChild));
        }

        var document = Document;
        var args = new XmlNodeChangedEventArgs(XmlNodeChangedAction.Remove, oldChild, this, null);
        NodeRemoving?.Invoke(this, args);
        document.NotifyChanging(args);
        _children.RemoveAt(index);
        oldChild._parent = null;
        NodeRemoved?.Invoke(this, args);
        document.NotifyChanged(args);
        return oldChild;
    }

    public virtual XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
    {
        ArgumentNullException.ThrowIfNull(newChild);
        ArgumentNullException.ThrowIfNull(oldChild);
        var index = _children.IndexOf(oldChild);
        if (index < 0)
        {
            throw new ArgumentException("The old child is not part of this node.", nameof(oldChild));
        }

        EnsureCanInsert(newChild);
        if (newChild.ParentNode is not null)
        {
            newChild.ParentNode.RemoveChild(newChild);
        }

        RemoveChild(oldChild);
        return InsertBefore(newChild, index < _children.Count ? _children[index] : null);
    }

    public virtual void Normalize()
    {
        for (var index = 0; index < _children.Count; index++)
        {
            _children[index].Normalize();
            if (_children[index] is XmlText text && index + 1 < _children.Count && _children[index + 1] is XmlText next)
            {
                text.AppendValue(next.Value);
                RemoveChild(next);
                index--;
            }
        }
    }

    public virtual XmlNode CloneNode(bool deep)
    {
        var clone = CloneCore(Document);
        CopyCloneData(clone);

        if (deep)
        {
            for (var index = 0; index < _children.Count; index++)
            {
                clone.AppendChild(_children[index].CloneNode(true));
            }
        }

        return clone;
    }

    public virtual XmlNodeList SelectNodes(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        return XmlDomXPath.Select(this, xpath);
    }

    public virtual XmlNode? SelectSingleNode(string xpath)
    {
        var nodes = SelectNodes(xpath);
        return nodes.Count == 0 ? null : nodes[0];
    }

    public abstract void WriteTo(XmlWriter writer);

    internal virtual XmlNode CloneCore(XmlDocument owner) => new XmlDocumentFragment(owner);
    internal virtual void CopyCloneData(XmlNode clone) { }
    internal IList<XmlNode> MutableChildren => _children;
    internal void SetParent(XmlNode? parent) => _parent = parent;

    private XmlNode? Sibling(int delta)
    {
        if (_parent is null)
        {
            return null;
        }

        var siblings = _parent._children;
        var index = siblings.IndexOf(this) + delta;
        return index >= 0 && index < siblings.Count ? siblings[index] : null;
    }

    private void EnsureCanInsert(XmlNode node)
    {
        if (ReferenceEquals(node, this))
        {
            throw new InvalidOperationException("A node cannot be inserted into itself.");
        }

        for (var parent = this; parent is not null; parent = parent.ParentNode)
        {
            if (ReferenceEquals(parent, node))
            {
                throw new InvalidOperationException("A node cannot be inserted below one of its descendants.");
            }
        }
    }

    private void AppendText(StringBuilder builder)
    {
        if (this is XmlText text)
        {
            builder.Append(text.Value);
            return;
        }

        for (var index = 0; index < _children.Count; index++)
        {
            _children[index].AppendText(builder);
        }
    }

    private void RemoveAllChildren()
    {
        while (_children.Count != 0)
        {
            RemoveChild(_children[^1]);
        }
    }

    internal void NotifyChanging(XmlNodeChangedEventArgs args)
    {
        NodeChanging?.Invoke(this, args);
        OwnerDocument?.NotifyChanging(args);
    }

    internal void NotifyChanged(XmlNodeChangedEventArgs args)
    {
        NodeChanged?.Invoke(this, args);
        OwnerDocument?.NotifyChanged(args);
    }
}

public sealed class XmlDocument : XmlNode
{
    private readonly NameTable _nameTable = new();

    public XmlDocument() : base(null) => OwnerDocument = this;

    public override XmlNodeType NodeType => XmlNodeType.Document;
    public override string Name => "#document";
    public XmlElement? DocumentElement
    {
        get
        {
            for (var index = 0; index < MutableChildren.Count; index++)
            {
                if (MutableChildren[index] is XmlElement element)
                {
                    return element;
                }
            }

            return null;
        }
    }

    public XmlNameTable NameTable => _nameTable;
    public new event XmlNodeChangedEventHandler? NodeChanging;
    public new event XmlNodeChangedEventHandler? NodeChanged;
    public new event XmlNodeChangedEventHandler? NodeInserting;
    public new event XmlNodeChangedEventHandler? NodeInserted;
    public new event XmlNodeChangedEventHandler? NodeRemoving;
    public new event XmlNodeChangedEventHandler? NodeRemoved;

    public XmlElement CreateElement(string name) => CreateElement(string.Empty, name, string.Empty);
    public XmlElement CreateElement(string prefix, string localName, string? namespaceURI) =>
        new(this, prefix ?? string.Empty, localName, namespaceURI ?? string.Empty);
    public XmlAttribute CreateAttribute(string name) => CreateAttribute(string.Empty, name, string.Empty);
    public XmlAttribute CreateAttribute(string prefix, string localName, string? namespaceURI) =>
        new(this, prefix ?? string.Empty, localName, namespaceURI ?? string.Empty);
    public XmlText CreateTextNode(string text) => new(this, text ?? string.Empty);
    public XmlCDataSection CreateCDataSection(string data) => new(this, data ?? string.Empty);
    public XmlComment CreateComment(string data) => new(this, data ?? string.Empty);
    public XmlProcessingInstruction CreateProcessingInstruction(string target, string data) =>
        new(this, target, data ?? string.Empty);
    public XmlDeclaration CreateXmlDeclaration(string version, string? encoding, string? standalone) =>
        new(this, version, encoding, standalone);
    public XmlDocumentFragment CreateDocumentFragment() => new(this);

    public void Load(Stream inStream)
    {
        ArgumentNullException.ThrowIfNull(inStream);
        Load(XmlReader.Create(inStream));
    }

    public void Load(TextReader txtReader)
    {
        ArgumentNullException.ThrowIfNull(txtReader);
        Load(XmlReader.Create(txtReader));
    }

    public void Load(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        while (MutableChildren.Count != 0)
        {
            RemoveChild(MutableChildren[^1]);
        }

        var parents = new List<XmlNode> { this };
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        var element = CreateElement(reader.Prefix, reader.LocalName.Length == 0 ? reader.Name : reader.LocalName, reader.NamespaceURI);
                        if (reader.Prefix.Length == 0 && reader.LocalName.Length == 0)
                        {
                            element = CreateElement(reader.Name);
                        }

                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                var attribute = CreateAttribute(reader.Prefix, reader.LocalName.Length == 0 ? reader.Name : reader.LocalName, reader.NamespaceURI);
                                attribute.Value = reader.Value;
                                element.Attributes.SetNamedItem(attribute);
                            } while (reader.MoveToNextAttribute());
                        }

                        reader.MoveToElement();
                        parents[^1].AppendChild(element);
                        if (!reader.IsEmptyElement)
                        {
                            parents.Add(element);
                        }

                        break;
                    }
                case XmlNodeType.EndElement:
                    if (parents.Count > 1)
                    {
                        parents.RemoveAt(parents.Count - 1);
                    }

                    break;
                case XmlNodeType.Text:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    parents[^1].AppendChild(CreateTextNode(reader.Value));
                    break;
                case XmlNodeType.CDATA:
                    parents[^1].AppendChild(CreateCDataSection(reader.Value));
                    break;
                case XmlNodeType.Comment:
                    parents[^1].AppendChild(CreateComment(reader.Value));
                    break;
                case XmlNodeType.ProcessingInstruction:
                    parents[^1].AppendChild(CreateProcessingInstruction(reader.Name, reader.Value));
                    break;
                case XmlNodeType.XmlDeclaration:
                    parents[^1].AppendChild(CreateXmlDeclaration("1.0", null, null));
                    break;
            }
        }
    }

    public void LoadXml(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        using var reader = XmlReader.Create(new StringReader(xml));
        Load(reader);
    }

    public void Validate(XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler = null)
    {
        ArgumentNullException.ThrowIfNull(schemas);
        if (DocumentElement is null || !schemas.Validate(DocumentElement, validationEventHandler))
        {
            throw new XmlSchemaException("The XML document does not satisfy the supplied schema set.");
        }
    }

    public void Save(Stream outStream)
    {
        ArgumentNullException.ThrowIfNull(outStream);
        using var writer = XmlWriter.Create(outStream);
        if (FirstChild is not XmlDeclaration)
        {
            writer.WriteStartDocument();
        }
        WriteTo(writer);
        writer.Flush();
    }

    public void Save(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        using var output = XmlWriter.Create(writer);
        if (FirstChild is not XmlDeclaration)
        {
            output.WriteStartDocument();
        }
        WriteTo(output);
        output.Flush();
    }

    public void Save(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (FirstChild is not XmlDeclaration)
        {
            writer.WriteStartDocument();
        }
        WriteTo(writer);
        writer.Flush();
    }

    public XmlNode? ReadNode(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.NodeType == XmlNodeType.None && !reader.Read())
        {
            return null;
        }

        var fragment = new XmlDocument();
        using var subtree = reader.ReadSubtree();
        fragment.Load(subtree);
        return fragment.FirstChild ?? CreateDocumentFragment();
    }

    public XmlNode ImportNode(XmlNode node, bool deep)
    {
        ArgumentNullException.ThrowIfNull(node);
        return ImportNodeCore(node, deep);
    }

    public override void WriteTo(XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        for (var index = 0; index < MutableChildren.Count; index++)
        {
            MutableChildren[index].WriteTo(writer);
        }
    }

    internal new void NotifyChanging(XmlNodeChangedEventArgs args)
    {
        NodeChanging?.Invoke(this, args);
        if (args.Action == XmlNodeChangedAction.Insert)
        {
            NodeInserting?.Invoke(this, args);
        }
        else if (args.Action == XmlNodeChangedAction.Remove)
        {
            NodeRemoving?.Invoke(this, args);
        }
    }

    internal new void NotifyChanged(XmlNodeChangedEventArgs args)
    {
        NodeChanged?.Invoke(this, args);
        if (args.Action == XmlNodeChangedAction.Insert)
        {
            NodeInserted?.Invoke(this, args);
        }
        else if (args.Action == XmlNodeChangedAction.Remove)
        {
            NodeRemoved?.Invoke(this, args);
        }
    }

    internal void NotifyChanged(XmlNodeChangedAction action, XmlNode node, XmlNode? oldParent, XmlNode? newParent) =>
        NotifyChanged(new XmlNodeChangedEventArgs(action, node, oldParent, newParent));

    internal override XmlNode CloneCore(XmlDocument owner) => new XmlDocument();

    private XmlNode ImportNodeCore(XmlNode node, bool deep)
    {
        XmlNode clone = node switch
        {
            XmlElement element => CreateElement(element.Prefix, element.LocalName, element.NamespaceURI),
            XmlAttribute attribute => CreateAttribute(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI),
            XmlCDataSection cdata => CreateCDataSection(cdata.Value),
            XmlText text => CreateTextNode(text.Value),
            XmlComment comment => CreateComment(comment.Value),
            XmlProcessingInstruction pi => CreateProcessingInstruction(pi.Target, pi.Data),
            XmlDeclaration declaration => CreateXmlDeclaration(declaration.Version, declaration.Encoding, declaration.Standalone),
            XmlDocumentFragment => CreateDocumentFragment(),
            _ => CreateDocumentFragment(),
        };

        if (clone is XmlElement targetElement && node is XmlElement sourceElement)
        {
            sourceElement.CopyAttributesTo(targetElement);
        }

        if (clone is XmlAttribute targetAttribute && node is XmlAttribute sourceAttribute)
        {
            targetAttribute.Value = sourceAttribute.Value;
        }

        if (deep)
        {
            for (var index = 0; index < node.MutableChildren.Count; index++)
            {
                clone.AppendChild(ImportNodeCore(node.MutableChildren[index], true));
            }
        }

        return clone;
    }
}

public class XmlElement : XmlNode
{
    private readonly XmlAttributeCollection _attributes;
    private readonly string _prefix;
    private readonly string _localName;
    private readonly string _namespaceUri;

    internal XmlElement(XmlDocument owner, string prefix, string localName, string namespaceUri) : base(owner)
    {
        _prefix = prefix;
        _localName = localName;
        _namespaceUri = namespaceUri;
        _attributes = new XmlAttributeCollection(this);
    }

    public override string Name => string.IsNullOrEmpty(_prefix) ? _localName : _prefix + ":" + _localName;
    public override string LocalName => _localName;
    public override string NamespaceURI => _namespaceUri;
    public override string Prefix => _prefix;
    public override XmlNodeType NodeType => XmlNodeType.Element;
    public override XmlAttributeCollection Attributes => _attributes;

    public string GetAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _attributes.GetNamedItem(name)?.InnerText ?? string.Empty;
    }

    public string GetAttribute(string localName, string? namespaceURI) =>
        _attributes.GetNamedItem(localName, namespaceURI)?.InnerText ?? string.Empty;

    public void SetAttribute(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        var existing = _attributes.GetNamedItem(name) as XmlAttribute;
        if (existing is not null)
        {
            existing.Value = value;
            return;
        }

        var attribute = Document.CreateAttribute(name);
        attribute.Value = value;
        _attributes.SetNamedItem(attribute);
    }

    public void SetAttribute(string localName, string? namespaceURI, string value)
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(value);
        var existing = _attributes.GetNamedItem(localName, namespaceURI);
        if (existing is not null)
        {
            existing.Value = value;
            return;
        }

        var attribute = Document.CreateAttribute(string.Empty, localName, namespaceURI);
        attribute.Value = value;
        _attributes.SetNamedItem(attribute);
    }

    public bool HasAttribute(string name) => _attributes.GetNamedItem(name) is not null;
    public bool HasAttribute(string localName, string? namespaceURI) => _attributes.GetNamedItem(localName, namespaceURI) is not null;

    public XmlNodeList GetElementsByTagName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var result = new List<XmlNode>();
        CollectElements(this, name, result);
        return new SnapshotXmlNodeList(result);
    }

    public XmlNodeList GetElementsByTagName(string localName, string? namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(localName);
        var result = new List<XmlNode>();
        CollectElements(this, localName, namespaceURI ?? string.Empty, result);
        return new SnapshotXmlNodeList(result);
    }

    public override void WriteTo(XmlWriter writer)
    {
        writer.WriteStartElement(_prefix, _localName, _namespaceUri);
        for (var index = 0; index < _attributes.Count; index++)
        {
            if (_attributes.Item(index) is XmlAttribute attribute)
            {
                attribute.WriteTo(writer);
            }
        }

        for (var index = 0; index < MutableChildren.Count; index++)
        {
            MutableChildren[index].WriteTo(writer);
        }

        writer.WriteEndElement();
    }

    internal void CopyAttributesTo(XmlElement target) => _attributes.CopyTo(target._attributes);

    public override XmlNode CloneNode(bool deep)
    {
        var clone = (XmlElement)CloneCore(Document);
        _attributes.CopyTo(clone._attributes);

        if (deep)
        {
            for (var index = 0; index < MutableChildren.Count; index++)
            {
                clone.AppendChild(MutableChildren[index].CloneNode(true));
            }
        }

        return clone;
    }

    internal override XmlNode CloneCore(XmlDocument owner)
    {
        var clone = owner.CreateElement(_prefix, _localName, _namespaceUri);
        _attributes.CopyTo(clone._attributes);

        return clone;
    }

    internal override void CopyCloneData(XmlNode clone) => _attributes.CopyTo(((XmlElement)clone)._attributes);

    private static void CollectElements(XmlNode parent, string name, List<XmlNode> result)
    {
        for (var index = 0; index < parent.MutableChildren.Count; index++)
        {
            var child = parent.MutableChildren[index];
            if (child is XmlElement element)
            {
                if (name == "*" || StringComparer.Ordinal.Equals(element.Name, name))
                {
                    result.Add(element);
                }

                CollectElements(element, name, result);
            }
        }
    }

    private static void CollectElements(XmlNode parent, string name, string ns, List<XmlNode> result)
    {
        for (var index = 0; index < parent.MutableChildren.Count; index++)
        {
            var child = parent.MutableChildren[index];
            if (child is XmlElement element)
            {
                if ((name == "*" || StringComparer.Ordinal.Equals(element.LocalName, name)) &&
                    StringComparer.Ordinal.Equals(element.NamespaceURI, ns))
                {
                    result.Add(element);
                }

                CollectElements(element, name, ns, result);
            }
        }
    }
}

public sealed class XmlAttribute : XmlNode
{
    private readonly string _prefix;
    private readonly string _localName;
    private readonly string _namespaceUri;
    private string _value = string.Empty;

    internal XmlAttribute(XmlDocument owner, string prefix, string localName, string namespaceUri) : base(owner)
    {
        _prefix = prefix;
        _localName = localName;
        _namespaceUri = namespaceUri;
    }

    public override string Name => string.IsNullOrEmpty(_prefix) ? _localName : _prefix + ":" + _localName;
    public override string LocalName => _localName;
    public override string NamespaceURI => _namespaceUri;
    public override string Prefix => _prefix;
    public override XmlNodeType NodeType => XmlNodeType.Attribute;
    public override string InnerText { get => _value; set => Value = value; }
    public override string Value
    {
        get => _value;
        set
        {
            var next = value ?? string.Empty;
            if (StringComparer.Ordinal.Equals(_value, next))
            {
                return;
            }

            var args = new XmlNodeChangedEventArgs(XmlNodeChangedAction.Change, this, ParentNode, ParentNode);
            NotifyChanging(args);
            _value = next;
            NotifyChanged(args);
        }
    }

    public override void WriteTo(XmlWriter writer) => writer.WriteAttributeString(_prefix, _localName, _namespaceUri, _value);
    public override XmlNode CloneNode(bool deep)
    {
        var clone = Document.CreateAttribute(_prefix, _localName, _namespaceUri);
        clone.Value = _value;
        if (deep)
        {
            for (var index = 0; index < MutableChildren.Count; index++)
            {
                clone.AppendChild(MutableChildren[index].CloneNode(true));
            }
        }

        return clone;
    }
    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateAttribute(_prefix, _localName, _namespaceUri);
    internal override void CopyCloneData(XmlNode clone) => ((XmlAttribute)clone).Value = _value;
}

public class XmlText : XmlNode
{
    private string _value;

    internal XmlText(XmlDocument owner, string value) : base(owner) => _value = value;
    public override XmlNodeType NodeType => XmlNodeType.Text;
    public override string Name => "#text";
    public override string InnerText { get => _value; set => Value = value; }
    public override string Value
    {
        get => _value;
        set
        {
            var next = value ?? string.Empty;
            if (StringComparer.Ordinal.Equals(_value, next))
            {
                return;
            }

            var args = new XmlNodeChangedEventArgs(XmlNodeChangedAction.Change, this, ParentNode, ParentNode);
            NotifyChanging(args);
            _value = next;
            NotifyChanged(args);
        }
    }

    public override void WriteTo(XmlWriter writer) => writer.WriteString(_value);
    internal void AppendValue(string value) => _value += value;
    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateTextNode(_value);
}

public sealed class XmlCDataSection : XmlText
{
    internal XmlCDataSection(XmlDocument owner, string value) : base(owner, value) { }
    public override XmlNodeType NodeType => XmlNodeType.CDATA;
    public override string Name => "#cdata-section";
    public override void WriteTo(XmlWriter writer) => writer.WriteCData(Value);
    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateCDataSection(Value);
}

public sealed class XmlComment : XmlNode
{
    private string _value;
    internal XmlComment(XmlDocument owner, string value) : base(owner) => _value = value;
    public override XmlNodeType NodeType => XmlNodeType.Comment;
    public override string Name => "#comment";
    public override string Value { get => _value; set => _value = value ?? string.Empty; }
    public override string InnerText { get => _value; set => Value = value; }
    public override void WriteTo(XmlWriter writer) => writer.WriteComment(_value);
    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateComment(_value);
}

public sealed class XmlProcessingInstruction : XmlNode
{
    internal XmlProcessingInstruction(XmlDocument owner, string target, string data) : base(owner)
    {
        Target = target;
        Data = data;
    }

    public string Target { get; }
    public string Data { get; set; }
    public override string Name => Target;
    public override XmlNodeType NodeType => XmlNodeType.ProcessingInstruction;
    public override string Value { get => Data; set => Data = value ?? string.Empty; }
    public override void WriteTo(XmlWriter writer) => writer.WriteProcessingInstruction(Target, Data);
    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateProcessingInstruction(Target, Data);
}

public sealed class XmlDeclaration : XmlNode
{
    internal XmlDeclaration(XmlDocument owner, string version, string? encoding, string? standalone) : base(owner)
    {
        Version = version;
        Encoding = encoding;
        Standalone = standalone;
    }

    public string Version { get; }
    public string? Encoding { get; }
    public string? Standalone { get; }
    public override string Name => "xml";
    public override XmlNodeType NodeType => XmlNodeType.XmlDeclaration;
    public override string Value => string.Empty;
    public override void WriteTo(XmlWriter writer) => writer.WriteStartDocument(string.Equals(Standalone, "yes", StringComparison.OrdinalIgnoreCase));
    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateXmlDeclaration(Version, Encoding, Standalone);
}

public sealed class XmlDocumentFragment : XmlNode
{
    internal XmlDocumentFragment(XmlDocument owner) : base(owner) { }
    public override string Name => "#document-fragment";
    public override XmlNodeType NodeType => XmlNodeType.Document;
    public override void WriteTo(XmlWriter writer)
    {
        for (var index = 0; index < MutableChildren.Count; index++)
        {
            MutableChildren[index].WriteTo(writer);
        }
    }

    internal override XmlNode CloneCore(XmlDocument owner) => owner.CreateDocumentFragment();
}

internal static class XmlDomXPath
{
    public static XmlNodeList Select(XmlNode context, string xpath)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(xpath);
        var nodes = XPathEngine.SelectNodes(context, xpath);
        return new SnapshotXmlNodeList(new List<XmlNode>(nodes));
    }
}
