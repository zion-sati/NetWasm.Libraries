// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The facade follows dotnet/runtime's System.Xml.ReaderWriter shape at commit
// 811225a482702af7ecc35d817966bc70b88a3a23. This retained writer is
// synchronous and emits only to an application-owned TextWriter or Stream.

using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

public abstract class XmlWriter : IDisposable, IAsyncDisposable
{
    protected XmlWriter() { }

    public static XmlWriter Create(Stream output) => Create(output, new XmlWriterSettings());

    public static XmlWriter Create(Stream output, XmlWriterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(settings);
        return new SimpleXmlWriter(
            new StreamXmlEmitter(output, settings.Encoding),
            new StreamXmlFlusher(output),
            new StreamXmlAsyncFlusher(output),
            new StreamXmlCloser(output, settings.CloseOutput),
            settings);
    }

    public static XmlWriter Create(TextWriter output) => Create(output, new XmlWriterSettings());

    public static XmlWriter Create(TextWriter output, XmlWriterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(settings);
        return new SimpleXmlWriter(
            new TextWriterXmlEmitter(output),
            new TextWriterXmlFlusher(output),
            new TextWriterXmlAsyncFlusher(output),
            new TextWriterXmlCloser(output, settings.CloseOutput),
            settings);
    }

    public abstract XmlWriterSettings Settings { get; }
    public abstract WriteState WriteState { get; }

    public virtual void WriteStartDocument()
    {
        WriteStartDocument(false);
    }

    public abstract void WriteStartDocument(bool standalone);
    public abstract void WriteEndDocument();
    public abstract void WriteStartElement(string? prefix, string localName, string? ns);
    public virtual void WriteStartElement(string localName) => WriteStartElement(null, localName, null);
    public virtual void WriteStartElement(string? prefix, string localName) => WriteStartElement(prefix, localName, null);
    public abstract void WriteEndElement();
    public abstract void WriteFullEndElement();
    public abstract void WriteStartAttribute(string? prefix, string localName, string? ns);
    public virtual void WriteStartAttribute(string localName) => WriteStartAttribute(null, localName, null);
    public virtual void WriteStartAttribute(string? prefix, string localName) => WriteStartAttribute(prefix, localName, null);
    public abstract void WriteEndAttribute();
    public abstract void WriteString(string? text);
    public abstract void WriteWhitespace(string? ws);
    public abstract void WriteCData(string? text);
    public abstract void WriteComment(string? text);
    public abstract void WriteProcessingInstruction(string name, string? text);
    public abstract void WriteRaw(string? data);
    public virtual void WriteAttributeString(string localName, string? value)
    {
        WriteStartAttribute(localName);
        WriteString(value);
        WriteEndAttribute();
    }

    public virtual void WriteAttributeString(string? prefix, string localName, string? ns, string? value)
    {
        WriteStartAttribute(prefix, localName, ns);
        WriteString(value);
        WriteEndAttribute();
    }

    public abstract void Flush();

    public virtual Task FlushAsync() => Task.CompletedTask;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    protected virtual void Dispose(bool disposing) { }
}

public enum WriteState
{
    Start,
    Prolog,
    Element,
    Attribute,
    Content,
    Closed,
    Error,
    End,
}

internal sealed class SimpleXmlWriter(
    IXmlOutputSink sink,
    IXmlOutputFlusher flusher,
    IXmlOutputAsyncFlusher asyncFlusher,
    IXmlOutputCloser closer,
    XmlWriterSettings settings) : XmlWriter
{
    private readonly IXmlOutputSink _sink = sink;
    private readonly IXmlOutputFlusher _flusher = flusher;
    private readonly IXmlOutputAsyncFlusher _asyncFlusher = asyncFlusher;
    private readonly IXmlOutputCloser _closer = closer;
    private readonly XmlWriterSettings _settings = settings.CloneForWriter();
    private readonly List<string> _elements = [];
    private bool _startTagOpen;
    private bool _attributeOpen;
    private bool _documentStarted;
    private bool _hasText;
    private bool _closed;
    private WriteState _writeState = WriteState.Start;

    public override XmlWriterSettings Settings => _settings;
    public override WriteState WriteState => _closed ? WriteState.Closed : _writeState;

    public override void WriteStartDocument(bool standalone)
    {
        EnsureOpen();
        if (_documentStarted)
        {
            throw new InvalidOperationException("The XML declaration has already been written.");
        }

        _documentStarted = true;
        _writeState = WriteState.Prolog;
        if (!_settings.OmitXmlDeclaration)
        {
            var declaration = "<?xml version=\"1.0\" encoding=\"" + _settings.Encoding.WebName + "\"" + (standalone ? " standalone=\"yes\"" : string.Empty) + "?>";
            _sink.Emit(declaration);
            EmitNewLineIfIndented();
        }
    }

    public override void WriteEndDocument()
    {
        EnsureOpen();
        while (_elements.Count != 0)
        {
            WriteEndElement();
        }

        _writeState = WriteState.End;
    }

    public override void WriteStartElement(string? prefix, string localName, string? ns)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(localName);
        if (_attributeOpen)
        {
            WriteEndAttribute();
        }

        CloseStartTag();
        if (_settings.Indent && _elements.Count > 0 && !_hasText)
        {
            EmitIndent(_elements.Count);
        }

        var qualifiedName = string.IsNullOrEmpty(prefix) ? localName : prefix + ":" + localName;
        _sink.Emit("<" + qualifiedName);
        _elements.Add(qualifiedName);
        _startTagOpen = true;
        _hasText = false;
        _writeState = WriteState.Element;
    }

    public override void WriteEndElement() => EndElement(false);

    public override void WriteFullEndElement() => EndElement(true);

    public override void WriteStartAttribute(string? prefix, string localName, string? ns)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(localName);
        if (!_startTagOpen)
        {
            throw new InvalidOperationException("Attributes may only be written inside a start element.");
        }

        if (_attributeOpen)
        {
            WriteEndAttribute();
        }

        var qualifiedName = string.IsNullOrEmpty(prefix) ? localName : prefix + ":" + localName;
        _sink.Emit(" " + qualifiedName + "=\"");
        _attributeOpen = true;
        _writeState = WriteState.Attribute;
    }

    public override void WriteEndAttribute()
    {
        EnsureOpen();
        if (!_attributeOpen)
        {
            return;
        }

        _sink.Emit("\"");
        _attributeOpen = false;
        _writeState = WriteState.Element;
    }

    public override void WriteString(string? text)
    {
        EnsureOpen();
        if (text is null)
        {
            return;
        }

        if (_attributeOpen)
        {
            _sink.Emit(EscapeAttribute(text));
        }
        else
        {
            CloseStartTag();
            _sink.Emit(EscapeText(text));
            _hasText = true;
            _writeState = WriteState.Content;
        }
    }

    public override void WriteWhitespace(string? ws)
    {
        if (ws is null)
        {
            throw new ArgumentNullException(nameof(ws));
        }

        if (!IsWhitespace(ws))
        {
            throw new ArgumentException("The value must contain only XML whitespace.", nameof(ws));
        }

        WriteString(ws);
    }

    public override void WriteCData(string? text)
    {
        EnsureOpen();
        CloseStartTag();
        var value = text ?? string.Empty;
        if (value.Contains("]]>", StringComparison.Ordinal))
        {
            throw new ArgumentException("CDATA content cannot contain its closing delimiter.", nameof(text));
        }

        _sink.Emit("<![CDATA[" + value + "]]>");
        _hasText = true;
        _writeState = WriteState.Content;
    }

    public override void WriteComment(string? text)
    {
        EnsureOpen();
        CloseStartTag();
        var value = text ?? string.Empty;
        if (value.Contains("--", StringComparison.Ordinal) || value.EndsWith("-", StringComparison.Ordinal))
        {
            throw new ArgumentException("Comment content is not well formed.", nameof(text));
        }

        _sink.Emit("<!--" + value + "-->");
        _writeState = WriteState.Content;
    }

    public override void WriteProcessingInstruction(string name, string? text)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(name);
        CloseStartTag();
        var value = text ?? string.Empty;
        if (value.Contains("?>", StringComparison.Ordinal))
        {
            throw new ArgumentException("Processing instruction content is not well formed.", nameof(text));
        }

        _sink.Emit("<?" + name + (value.Length == 0 ? string.Empty : " " + value) + "?>");
        _writeState = WriteState.Content;
    }

    public override void WriteRaw(string? data)
    {
        EnsureOpen();
        if (data is null)
        {
            return;
        }

        CloseStartTag();
        _sink.Emit(data);
        _writeState = WriteState.Content;
    }

    public override void Flush()
    {
        EnsureOpen();
        if (_attributeOpen)
        {
            WriteEndAttribute();
        }

        CloseStartTag();
        _flusher.Flush();
    }

    public override async Task FlushAsync()
    {
        EnsureOpen();
        if (_attributeOpen)
        {
            WriteEndAttribute();
        }

        CloseStartTag();
        await _asyncFlusher.FlushAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_closed)
        {
            return;
        }

        await FlushAsync().ConfigureAwait(false);
        Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (_closed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                if (_elements.Count != 0)
                {
                    WriteEndDocument();
                }

                Flush();
            }
            finally
            {
                _closer.Close();
                _closed = true;
            }
        }
    }

    private void EndElement(bool full)
    {
        EnsureOpen();
        if (_elements.Count == 0)
        {
            throw new InvalidOperationException("There is no open element.");
        }

        if (_attributeOpen)
        {
            WriteEndAttribute();
        }

        var name = _elements[^1];
        if (_startTagOpen)
        {
            _sink.Emit(full ? "></" + name + ">" : "/>");
            _startTagOpen = false;
        }
        else
        {
            if (_settings.Indent && !_hasText)
            {
                EmitIndent(_elements.Count - 1);
            }

            _sink.Emit("</" + name + ">");
        }

        _elements.RemoveAt(_elements.Count - 1);
        _hasText = false;
        _writeState = _elements.Count == 0 ? WriteState.Content : WriteState.Element;
    }

    private void CloseStartTag()
    {
        if (!_startTagOpen)
        {
            return;
        }

        if (_attributeOpen)
        {
            WriteEndAttribute();
        }

        _sink.Emit(">");
        _startTagOpen = false;
    }

    private void EmitIndent(int depth)
    {
        _sink.Emit(_settings.NewLineChars);
        for (var index = 0; index < depth; index++)
        {
            _sink.Emit("  ");
        }
    }

    private void EmitNewLineIfIndented()
    {
        if (_settings.Indent)
        {
            _sink.Emit(_settings.NewLineChars);
        }
    }

    private static string EscapeText(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string EscapeAttribute(string value) => EscapeText(value).Replace("\"", "&quot;").Replace("\r", "&#xD;").Replace("\n", "&#xA;");

    private static bool IsWhitespace(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] is not (' ' or '\t' or '\r' or '\n'))
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureOpen()
    {
        if (_closed)
        {
            throw new ObjectDisposedException(nameof(XmlWriter));
        }
    }
}
