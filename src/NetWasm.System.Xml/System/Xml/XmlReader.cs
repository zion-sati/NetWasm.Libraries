// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The facade follows dotnet/runtime's System.Xml.ReaderWriter shape at commit
// 811225a482702af7ecc35d817966bc70b88a3a23. The retained implementation is a
// synchronous, in-memory parser with no URI, file, reflection, or async path.

using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

public abstract class XmlReader : IDisposable
{
    protected XmlReader() { }

    public static XmlReader Create(Stream input) => Create(input, new XmlReaderSettings());

    public static XmlReader Create(Stream input, XmlReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(settings);
        return new SimpleXmlReader(
            new StreamXmlTextSource(input, Encoding.UTF8),
            new StreamXmlAsyncTextSource(input, Encoding.UTF8),
            settings,
            input);
    }

    public static XmlReader Create(TextReader input) => Create(input, new XmlReaderSettings());

    public static XmlReader Create(TextReader input, XmlReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(settings);
        return new SimpleXmlReader(
            new TextReaderXmlTextSource(input),
            new TextReaderXmlAsyncTextSource(input),
            settings,
            input);
    }

    public abstract int AttributeCount { get; }
    public abstract string BaseURI { get; }
    public abstract bool CanResolveEntity { get; }
    public abstract int Depth { get; }
    public abstract bool EOF { get; }
    public abstract bool HasValue { get; }
    public abstract bool IsEmptyElement { get; }
    public abstract string LocalName { get; }
    public abstract string Name { get; }
    public abstract string NamespaceURI { get; }
    public abstract XmlNameTable NameTable { get; }
    public abstract XmlNodeType NodeType { get; }
    public abstract string Prefix { get; }
    public abstract ReadState ReadState { get; }
    public abstract XmlReaderSettings? Settings { get; }
    public abstract string Value { get; }

    public virtual string? GetAttribute(string name) => GetAttribute(name, string.Empty);

    public abstract string? GetAttribute(string name, string? namespaceURI);

    public virtual string? GetAttribute(int i) => throw new ArgumentOutOfRangeException(nameof(i));

    public abstract bool MoveToAttribute(string name, string? namespaceURI);
    public virtual void MoveToAttribute(string name) => MoveToAttribute(name, string.Empty);
    public abstract bool MoveToElement();
    public abstract bool MoveToFirstAttribute();
    public abstract bool MoveToNextAttribute();
    public abstract bool Read();

    public virtual Task<bool> ReadAsync() => Task.FromResult(Read());

    public virtual Task<bool> ReadAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<bool>(cancellationToken)
            : ReadAsync();
    public virtual string ReadContentAsString()
    {
        if (NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Attribute)
        {
            return Value;
        }

        if (NodeType != XmlNodeType.Element)
        {
            return string.Empty;
        }

        var depth = Depth;
        var builder = new StringBuilder();
        while (Read() && !(NodeType == XmlNodeType.EndElement && Depth == depth))
        {
            if (NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.SignificantWhitespace)
            {
                builder.Append(Value);
            }
        }

        return builder.ToString();
    }

    public virtual Task<string> ReadContentAsStringAsync() =>
        Task.FromResult(ReadContentAsString());

    public virtual Task SkipAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        Skip();
        return Task.CompletedTask;
    }

    public virtual void Skip()
    {
        if (NodeType == XmlNodeType.Element && !IsEmptyElement)
        {
            var depth = Depth;
            while (Read() && !(NodeType == XmlNodeType.EndElement && Depth == depth)) { }
        }
    }

    public virtual XmlReader ReadSubtree() => new XmlSubtreeReader(this);
    public virtual bool ReadAttributeValue() => false;
    public virtual void ResolveEntity() => throw new InvalidOperationException("The current node is not an entity reference.");
    public virtual string? LookupNamespace(string prefix) => null;
    public virtual void Close() => Dispose();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }
}

internal sealed class XmlAttributeValue(string name, string prefix, string localName, string namespaceUri, string value)
{
    public string Name { get; } = name;
    public string Prefix { get; } = prefix;
    public string LocalName { get; } = localName;
    public string NamespaceUri { get; } = namespaceUri;
    public string Value { get; } = value;
}

internal sealed class XmlElementFrame(string name, string prefix, string localName, string namespaceUri, int depth)
{
    public string Name { get; } = name;
    public string Prefix { get; } = prefix;
    public string LocalName { get; } = localName;
    public string NamespaceUri { get; } = namespaceUri;
    public int Depth { get; } = depth;
}

internal sealed class SimpleXmlReader : XmlReader, IXmlLineInfo
{
    private string _xml = string.Empty;
    private readonly IXmlTextSource? _source;
    private readonly IXmlAsyncTextSource? _asyncSource;
    private readonly XmlReaderSettings _settings;
    private readonly XmlNameTable _nameTable;
    private readonly List<XmlElementFrame> _elements = [];
    private readonly List<Dictionary<string, string>> _namespaces = [];
    private readonly List<XmlAttributeValue> _attributes = [];
    private readonly TextReader? _textReader;
    private readonly Stream? _stream;
    private int _position;
    private int _attributeIndex = -1;
    private int _lineNumber = 1;
    private int _linePosition = 1;
    private int _tokenLineNumber = 1;
    private int _tokenLinePosition = 1;
    private int _tokenDepth;
    private bool _closed;
    private XmlNodeType _nodeType;
    private string _name = string.Empty;
    private string _prefix = string.Empty;
    private string _localName = string.Empty;
    private string _namespaceUri = string.Empty;
    private string _value = string.Empty;
    private bool _emptyElement;
    private bool _loaded;
    private bool _asyncOperation;
    private readonly Dictionary<string, string> _entities = new(StringComparer.Ordinal);
    private readonly HashSet<string> _entityExpansionStack = new(StringComparer.Ordinal);
    private long _expandedEntityCharacters;

    public SimpleXmlReader(
        IXmlTextSource source,
        IXmlAsyncTextSource asyncSource,
        XmlReaderSettings settings,
        object input)
    {
        _source = source;
        _asyncSource = asyncSource;
        _settings = settings.CloneForReader();
        _nameTable = _settings.NameTable ?? new NameTable();
        _textReader = input as TextReader;
        _stream = input as Stream;
        if (!_settings.Async)
        {
            Load(source.ReadAll());
        }
    }

    public override int AttributeCount => _attributes.Count;
    public override string BaseURI => string.Empty;
    public override bool CanResolveEntity => _settings.DtdProcessing == DtdProcessing.Parse;
    public override int Depth => _attributeIndex >= 0 ? _tokenDepth + 1 : _tokenDepth;
    public override bool EOF => ReadState == ReadState.EndOfFile;
    public override bool HasValue => _nodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Attribute or XmlNodeType.Comment or XmlNodeType.ProcessingInstruction or XmlNodeType.XmlDeclaration or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace;
    public override bool IsEmptyElement => _emptyElement;
    public override string LocalName => _attributeIndex >= 0 ? _attributes[_attributeIndex].LocalName : _localName;
    public override string Name => _attributeIndex >= 0 ? _attributes[_attributeIndex].Name : _name;
    public override string NamespaceURI => _attributeIndex >= 0 ? _attributes[_attributeIndex].NamespaceUri : _namespaceUri;
    public override XmlNameTable NameTable => _nameTable;
    public override XmlNodeType NodeType => _attributeIndex >= 0 ? XmlNodeType.Attribute : _nodeType;
    public override string Prefix => _attributeIndex >= 0 ? _attributes[_attributeIndex].Prefix : _prefix;
    public override ReadState ReadState => _closed ? ReadState.Closed : _nodeType == XmlNodeType.None && _position >= _xml.Length ? ReadState.EndOfFile : _nodeType == XmlNodeType.None ? ReadState.Initial : ReadState.Interactive;
    public override XmlReaderSettings Settings => _settings;
    public override string Value => _attributeIndex >= 0 ? _attributes[_attributeIndex].Value : _value;
    public int LineNumber => _tokenLineNumber;
    public int LinePosition => _tokenLinePosition;

    public bool HasLineInfo() => true;

    public override bool Read()
    {
        EnsureLoaded();
        if (_closed)
        {
            throw new ObjectDisposedException(nameof(XmlReader));
        }

        if (_attributeIndex >= 0)
        {
            _attributeIndex = -1;
        }

        _attributes.Clear();
        if (_emptyElement && _namespaces.Count > _elements.Count)
        {
            _namespaces.RemoveAt(_namespaces.Count - 1);
        }

        _emptyElement = false;
        if (_position >= _xml.Length)
        {
            if (_elements.Count != 0)
            {
                return Fail("Unexpected end of file before the closing element.");
            }

            _nodeType = XmlNodeType.None;
            return false;
        }

        _tokenLineNumber = _lineNumber;
        _tokenLinePosition = _linePosition;
        _tokenDepth = _elements.Count;
        if (_xml[_position] == '<')
        {
            return ReadMarkup();
        }

        return ReadText();
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        if (_closed)
        {
            throw new ObjectDisposedException(nameof(XmlReader));
        }

        if (_asyncOperation)
        {
            throw new InvalidOperationException("Only one asynchronous XML read may be active at a time.");
        }

        _asyncOperation = true;
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return Read();
        }
        finally
        {
            _asyncOperation = false;
        }
    }

    public override Task<bool> ReadAsync() => ReadAsync(CancellationToken.None);

    public override Task<string> ReadContentAsStringAsync() =>
        Task.FromResult(ReadContentAsString());

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        Load(_source!.ReadAll());
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        Load(await _asyncSource!.ReadAllAsync(cancellationToken).ConfigureAwait(false));
    }

    private void Load(string xml)
    {
        _xml = xml;
        _loaded = true;
        if (_settings.MaxCharactersInDocument > 0 && _xml.Length > _settings.MaxCharactersInDocument)
        {
            throw new XmlException("The XML document exceeds MaxCharactersInDocument.");
        }

        ParseDtdDeclarations();
    }

    private void ParseDtdDeclarations()
    {
        if (_settings.DtdProcessing != DtdProcessing.Parse)
        {
            return;
        }

        var doctypeStart = _xml.IndexOf("<!DOCTYPE", StringComparison.Ordinal);
        while (doctypeStart >= 0)
        {
            var close = FindDoctypeEnd(doctypeStart + 9);
            if (close < 0)
            {
                throw new XmlException("The document type declaration is incomplete.");
            }

            var declaration = _xml.Substring(doctypeStart, close + 1 - doctypeStart);
            ParseInternalEntities(declaration);
            doctypeStart = _xml.IndexOf("<!DOCTYPE", close + 1, StringComparison.Ordinal);
        }
    }

    private void ParseInternalEntities(string declaration)
    {
        var subsetStart = declaration.IndexOf('[', StringComparison.Ordinal);
        var subsetEnd = declaration.LastIndexOf(']');
        if (subsetStart < 0 || subsetEnd <= subsetStart)
        {
            return;
        }

        var cursor = subsetStart + 1;
        while (cursor < subsetEnd)
        {
            var entityStart = declaration.IndexOf("<!ENTITY", cursor, StringComparison.Ordinal);
            if (entityStart < 0 || entityStart >= subsetEnd)
            {
                return;
            }

            cursor = entityStart + 8;
            SkipWhitespace(declaration, ref cursor);
            var nameStart = cursor;
            while (cursor < subsetEnd && !char.IsWhiteSpace(declaration[cursor]) && declaration[cursor] is not ('>' or '\'' or '"'))
            {
                cursor++;
            }

            var name = declaration.Substring(nameStart, cursor - nameStart);
            SkipWhitespace(declaration, ref cursor);
            if (cursor >= subsetEnd)
            {
                throw new XmlException("The entity declaration is incomplete.");
            }

            if (declaration[cursor] is '\'' or '"')
            {
                var quote = declaration[cursor++];
                var valueStart = cursor;
                var valueEnd = declaration.IndexOf(quote, cursor);
                if (valueEnd < 0)
                {
                    throw new XmlException("The entity declaration is incomplete.");
                }

                _entities[name] = declaration.Substring(valueStart, valueEnd - valueStart);
                cursor = valueEnd + 1;
            }
            else
            {
                var systemStart = declaration.IndexOf("SYSTEM", cursor, StringComparison.Ordinal);
                var quoteStart = systemStart < 0 ? -1 : declaration.IndexOfAny(['\'', '"'], systemStart + 6);
                if (quoteStart < 0)
                {
                    throw new XmlException("The external entity declaration is incomplete.");
                }

                var quote = declaration[quoteStart];
                var uriEnd = declaration.IndexOf(quote, quoteStart + 1);
                if (uriEnd < 0)
                {
                    throw new XmlException("The external entity declaration is incomplete.");
                }

                _entities[name] = LoadExternalEntity(declaration.Substring(quoteStart + 1, uriEnd - quoteStart - 1));
                cursor = uriEnd + 1;
            }

            var terminator = declaration.IndexOf('>', cursor);
            cursor = terminator < 0 ? subsetEnd : terminator + 1;
        }
    }

    private string LoadExternalEntity(string relativeUri)
    {
        var resolver = _settings.XmlResolver ?? XmlResolver.ThrowingResolver;
        var absoluteUri = resolver.ResolveUri(null, relativeUri);
        var entity = resolver.GetEntity(absoluteUri, null, null);
        return entity switch
        {
            TextReader reader => reader.ReadToEnd(),
            Stream stream => ReadEntityStream(stream),
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => throw new XmlException("The XML resolver returned an unsupported entity type."),
        };
    }

    private static string ReadEntityStream(Stream stream)
    {
        using (stream)
        {
            return new StreamXmlTextSource(stream, Encoding.UTF8).ReadAll();
        }
    }

    private int FindDoctypeEnd(int start)
    {
        var quote = '\0';
        var subsetDepth = 0;
        for (var index = start; index < _xml.Length; index++)
        {
            var character = _xml[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (character == '[')
            {
                subsetDepth++;
            }
            else if (character == ']')
            {
                subsetDepth--;
            }
            else if (character == '>' && subsetDepth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    public override string? GetAttribute(string name, string? namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (var attribute in _attributes)
        {
            if (StringComparer.Ordinal.Equals(attribute.Name, name) &&
                StringComparer.Ordinal.Equals(attribute.NamespaceUri, namespaceURI ?? string.Empty))
            {
                return attribute.Value;
            }
        }

        return null;
    }

    public override string? GetAttribute(int i) =>
        (uint)i < (uint)_attributes.Count ? _attributes[i].Value : throw new ArgumentOutOfRangeException(nameof(i));

    public override bool MoveToAttribute(string name, string? namespaceURI)
    {
        ArgumentNullException.ThrowIfNull(name);
        for (var index = 0; index < _attributes.Count; index++)
        {
            var attribute = _attributes[index];
            if (StringComparer.Ordinal.Equals(attribute.Name, name) &&
                StringComparer.Ordinal.Equals(attribute.NamespaceUri, namespaceURI ?? string.Empty))
            {
                _attributeIndex = index;
                return true;
            }
        }

        return false;
    }

    public override bool MoveToElement()
    {
        if (_attributeIndex < 0)
        {
            return false;
        }

        _attributeIndex = -1;
        return true;
    }

    public override bool MoveToFirstAttribute()
    {
        if (_attributes.Count == 0)
        {
            return false;
        }

        _attributeIndex = 0;
        return true;
    }

    public override bool MoveToNextAttribute()
    {
        if (_attributeIndex + 1 >= _attributes.Count)
        {
            return false;
        }

        _attributeIndex++;
        return true;
    }

    public override string? LookupNamespace(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if (_namespaces.Count == 0)
        {
            return null;
        }

        return _namespaces[^1].TryGetValue(prefix, out var value) ? value : null;
    }

    protected override void Dispose(bool disposing)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        if (_settings.CloseInput)
        {
            _textReader?.Dispose();
            _stream?.Dispose();
        }
    }

    private bool ReadMarkup()
    {
        if (StartsWith("<!--"))
        {
            return ReadDelimited(XmlNodeType.Comment, "-->", 4);
        }

        if (StartsWith("<![CDATA["))
        {
            return ReadDelimited(XmlNodeType.CDATA, "]]>", 9);
        }

        if (StartsWith("<?xml"))
        {
            return ReadDelimited(XmlNodeType.XmlDeclaration, "?>", 5);
        }

        if (StartsWith("<?"))
        {
            return ReadDelimited(XmlNodeType.ProcessingInstruction, "?>", 2);
        }

        if (StartsWith("<!DOCTYPE"))
        {
            if (_settings.DtdProcessing == DtdProcessing.Prohibit)
            {
                return Fail("DTD processing is prohibited by the reader settings.");
            }

            var start = _position;
            var close = FindDoctypeEnd(_position + 9);
            if (close < 0)
            {
                return Fail("The document type declaration is incomplete.");
            }

            var declaration = _xml.Substring(start, close + 1 - start);
            Advance(close + 1 - _position);
            if (_settings.DtdProcessing == DtdProcessing.Ignore)
            {
                return Read();
            }

            _nodeType = XmlNodeType.DocumentType;
            _value = declaration;
            return true;
        }

        if (StartsWith("</"))
        {
            return ReadEndElement();
        }

        return ReadStartElement();
    }

    private bool ReadDelimited(XmlNodeType type, string terminator, int prefixLength)
    {
        var start = _position + prefixLength;
        var end = _xml.IndexOf(terminator, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return Fail("The XML markup is incomplete.");
        }

        _value = _xml.Substring(start, end - start);
        Advance(end + terminator.Length - _position);
        _nodeType = type;
        return true;
    }

    private bool ReadStartElement()
    {
        var start = _position;
        var end = FindMarkupEnd(_position + 1);
        if (end < 0)
        {
            return Fail("The start element is incomplete.");
        }

        var content = _xml.Substring(_position + 1, end - _position - 1);
        var empty = content.EndsWith("/", StringComparison.Ordinal);
        if (empty)
        {
            content = content.Substring(0, content.Length - 1);
        }

        var cursor = 0;
        SkipWhitespace(content, ref cursor);
        var rawName = ReadName(content, ref cursor);
        if (rawName.Length == 0)
        {
            return Fail("An element name is required.");
        }

        var parent = _namespaces.Count == 0 ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(_namespaces[^1], StringComparer.Ordinal);
        var parsedAttributes = new List<(string Name, string Value)>();
        while (cursor < content.Length)
        {
            SkipWhitespace(content, ref cursor);
            if (cursor >= content.Length)
            {
                break;
            }

            var attributeName = ReadName(content, ref cursor);
            if (attributeName.Length == 0)
            {
                return Fail("An attribute name is required.");
            }

            SkipWhitespace(content, ref cursor);
            if (cursor >= content.Length || content[cursor] != '=')
            {
                return Fail("An attribute must have a quoted value.");
            }

            cursor++;
            SkipWhitespace(content, ref cursor);
            if (cursor >= content.Length || (content[cursor] != '\'' && content[cursor] != '"'))
            {
                return Fail("An attribute value must be quoted.");
            }

            var quote = content[cursor++];
            var valueStart = cursor;
            while (cursor < content.Length && content[cursor] != quote)
            {
                cursor++;
            }

            if (cursor >= content.Length)
            {
                return Fail("An attribute value is incomplete.");
            }

            parsedAttributes.Add((attributeName, DecodeEntities(content.Substring(valueStart, cursor - valueStart))));
            cursor++;
        }

        var (prefix, localName) = SplitName(rawName);
        foreach (var attribute in parsedAttributes)
        {
            if (attribute.Name == "xmlns")
            {
                parent[string.Empty] = attribute.Value;
            }
            else if (attribute.Name.StartsWith("xmlns:", StringComparison.Ordinal))
            {
                parent[attribute.Name.Substring(6)] = attribute.Value;
            }
        }

        var namespaceUri = prefix.Length == 0 ? Lookup(parent, string.Empty) : Lookup(parent, prefix);
        _attributes.Clear();
        foreach (var attribute in parsedAttributes)
        {
            var (attributePrefix, attributeLocalName) = SplitName(attribute.Name);
            var attributeNamespace = attributePrefix == "xmlns" || attribute.Name == "xmlns"
                ? "http://www.w3.org/2000/xmlns/"
                : attributePrefix.Length == 0 ? string.Empty : Lookup(parent, attributePrefix);
            _attributes.Add(new XmlAttributeValue(
                _nameTable.Add(attribute.Name),
                _nameTable.Add(attributePrefix),
                _nameTable.Add(attributeLocalName),
                _nameTable.Add(attributeNamespace),
                attribute.Value));
        }

        _name = _nameTable.Add(rawName);
        _prefix = _nameTable.Add(prefix);
        _localName = _nameTable.Add(localName);
        _namespaceUri = _nameTable.Add(namespaceUri);
        _value = string.Empty;
        _nodeType = XmlNodeType.Element;
        _emptyElement = empty;
        Advance(end + 1 - start);
        if (!empty)
        {
            _elements.Add(new XmlElementFrame(_name, _prefix, _localName, _namespaceUri, _elements.Count));
            _namespaces.Add(parent);
        }
        else
        {
            _namespaces.Add(parent);
        }

        return true;
    }

    private bool ReadEndElement()
    {
        if (_elements.Count == 0)
        {
            return Fail("An end element has no matching start element.");
        }

        var end = _xml.IndexOf('>', _position + 2);
        if (end < 0)
        {
            return Fail("The end element is incomplete.");
        }

        var rawName = _xml.Substring(_position + 2, end - _position - 2).Trim();
        var expected = _elements[^1];
        if (!StringComparer.Ordinal.Equals(rawName, expected.Name))
        {
            return Fail("The end element does not match its start element.");
        }

        _name = expected.Name;
        _prefix = expected.Prefix;
        _localName = expected.LocalName;
        _namespaceUri = expected.NamespaceUri;
        _value = string.Empty;
        _nodeType = XmlNodeType.EndElement;
        _tokenDepth = _elements.Count - 1;
        _emptyElement = false;
        Advance(end + 1 - _position);
        _elements.RemoveAt(_elements.Count - 1);
        _namespaces.RemoveAt(_namespaces.Count - 1);
        return true;
    }

    private bool ReadText()
    {
        var end = _xml.IndexOf('<', _position);
        if (end < 0)
        {
            end = _xml.Length;
        }

        var text = DecodeEntities(_xml.Substring(_position, end - _position));
        Advance(end - _position);
        if (text.Length == 0)
        {
            return Read();
        }

        var whitespace = IsWhitespace(text);
        if (whitespace && _settings.WhitespaceHandling == WhitespaceHandling.None)
        {
            return Read();
        }

        _nodeType = whitespace ? XmlNodeType.Whitespace : XmlNodeType.Text;
        _value = text;
        return true;
    }

    private string DecodeEntities(string text)
    {
        var ampersand = text.IndexOf('&');
        if (ampersand < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var cursor = 0;
        while (cursor < text.Length)
        {
            var next = text.IndexOf('&', cursor);
            if (next < 0)
            {
                builder.Append(text, cursor, text.Length - cursor);
                break;
            }

            builder.Append(text, cursor, next - cursor);
            var end = text.IndexOf(';', next + 1);
            if (end < 0)
            {
                throw new XmlException("An entity reference is incomplete.");
            }

            var entity = text.Substring(next + 1, end - next - 1);
            var decoded = entity switch
            {
                "amp" => "&",
                "lt" => "<",
                "gt" => ">",
                "quot" => "\"",
                "apos" => "'",
                _ when entity.StartsWith("#x", StringComparison.Ordinal) => ((char)Convert.ToInt32(entity.Substring(2), 16)).ToString(),
                _ when entity.StartsWith("#", StringComparison.Ordinal) => ((char)Convert.ToInt32(entity.Substring(1), 10)).ToString(),
                _ when _entities.TryGetValue(entity, out var value) => ExpandEntity(entity, value),
                _ => throw new XmlException("The XML entity is not declared in the selected resolver profile."),
            };
            builder.Append(decoded);
            cursor = end + 1;
        }

        return builder.ToString();
    }

    private string ExpandEntity(string name, string value)
    {
        if (!_entityExpansionStack.Add(name))
        {
            throw new XmlException("The XML entity expansion is recursive.");
        }

        try
        {
            _expandedEntityCharacters = checked(_expandedEntityCharacters + value.Length);
            if (_settings.MaxCharactersFromEntities > 0 &&
                _expandedEntityCharacters > _settings.MaxCharactersFromEntities)
            {
                throw new XmlException("The XML entity expansion exceeds MaxCharactersFromEntities.");
            }

            return DecodeEntities(value);
        }
        finally
        {
            _entityExpansionStack.Remove(name);
        }
    }

    private int FindMarkupEnd(int start)
    {
        var quote = '\0';
        for (var index = start; index < _xml.Length; index++)
        {
            var character = _xml[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (character == '>')
            {
                return index;
            }
        }

        return -1;
    }

    private void Advance(int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (_xml[_position++] == '\n')
            {
                _lineNumber++;
                _linePosition = 1;
            }
            else
            {
                _linePosition++;
            }
        }
    }

    private bool Fail(string message)
    {
        _nodeType = XmlNodeType.None;
        throw new XmlException(message + " (line " + _tokenLineNumber + ", position " + _tokenLinePosition + ").");
    }

    private bool StartsWith(string value) => _xml.AsSpan(_position).StartsWith(value.AsSpan(), StringComparison.Ordinal);

    private static void SkipWhitespace(string text, ref int cursor)
    {
        while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
        {
            cursor++;
        }
    }

    private static string ReadName(string text, ref int cursor)
    {
        var start = cursor;
        while (cursor < text.Length && !char.IsWhiteSpace(text[cursor]) && text[cursor] != '=' && text[cursor] != '/')
        {
            cursor++;
        }

        return text.Substring(start, cursor - start);
    }

    private static (string Prefix, string LocalName) SplitName(string name)
    {
        var separator = name.IndexOf(':');
        return separator < 0 ? (string.Empty, name) : (name.Substring(0, separator), name.Substring(separator + 1));
    }

    private static string Lookup(Dictionary<string, string> namespaces, string prefix) =>
        namespaces.TryGetValue(prefix, out var value) ? value : string.Empty;

    private static bool IsWhitespace(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (!char.IsWhiteSpace(text[index]))
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class XmlSubtreeReader(XmlReader parent) : XmlReader
{
    private readonly XmlReader _parent = parent;
    private readonly int _rootDepth = parent.Depth;
    private bool _started;
    private bool _finished;

    public override int AttributeCount => _parent.AttributeCount;
    public override string BaseURI => _parent.BaseURI;
    public override bool CanResolveEntity => _parent.CanResolveEntity;
    public override int Depth => _parent.Depth;
    public override bool EOF => _finished || _parent.EOF;
    public override bool HasValue => _parent.HasValue;
    public override bool IsEmptyElement => _parent.IsEmptyElement;
    public override string LocalName => _parent.LocalName;
    public override string Name => _parent.Name;
    public override string NamespaceURI => _parent.NamespaceURI;
    public override XmlNameTable NameTable => _parent.NameTable;
    public override XmlNodeType NodeType => _finished ? XmlNodeType.None : _parent.NodeType;
    public override string Prefix => _parent.Prefix;
    public override ReadState ReadState => _finished ? ReadState.EndOfFile : _parent.ReadState;
    public override XmlReaderSettings? Settings => _parent.Settings;
    public override string Value => _parent.Value;

    public override bool Read()
    {
        if (_finished)
        {
            return false;
        }

        if (!_started)
        {
            _started = true;
            return _parent.Read();
        }

        if (!_parent.Read())
        {
            _finished = true;
            return false;
        }

        if (_parent.NodeType == XmlNodeType.EndElement && _parent.Depth == _rootDepth)
        {
            _finished = true;
        }

        return true;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        if (_finished)
        {
            return false;
        }

        if (!_started)
        {
            _started = true;
            return await _parent.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await _parent.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            _finished = true;
            return false;
        }

        if (_parent.NodeType == XmlNodeType.EndElement && _parent.Depth == _rootDepth)
        {
            _finished = true;
        }

        return true;
    }

    public override Task<bool> ReadAsync() => ReadAsync(CancellationToken.None);

    public override string? GetAttribute(string name, string? namespaceURI) => _parent.GetAttribute(name, namespaceURI);
    public override string? GetAttribute(int i) => _parent.GetAttribute(i);
    public override bool MoveToAttribute(string name, string? namespaceURI) => _parent.MoveToAttribute(name, namespaceURI);
    public override bool MoveToElement() => _parent.MoveToElement();
    public override bool MoveToFirstAttribute() => _parent.MoveToFirstAttribute();
    public override bool MoveToNextAttribute() => _parent.MoveToNextAttribute();
    public override string? LookupNamespace(string prefix) => _parent.LookupNamespace(prefix);
    protected override void Dispose(bool disposing) => _parent.Dispose();
}
