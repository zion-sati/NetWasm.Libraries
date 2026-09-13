// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Settings retain the synchronous ReaderWriter contract from the pinned
// upstream facade. Unsupported ambient-resource paths intentionally have no
// default resolver.

using System.Text;

namespace System.Xml;

public sealed class XmlReaderSettings
{
    public XmlReaderSettings()
    {
        NameTable = new NameTable();
        DtdProcessing = DtdProcessing.Prohibit;
        ConformanceLevel = ConformanceLevel.Document;
        WhitespaceHandling = WhitespaceHandling.All;
    }

    public bool Async { get; set; }
    public bool CloseInput { get; set; }
    public ConformanceLevel ConformanceLevel { get; set; }
    public DtdProcessing DtdProcessing { get; set; }
    public long MaxCharactersFromEntities { get; set; }
    public long MaxCharactersInDocument { get; set; }
    public XmlNameTable? NameTable { get; set; }
    public XmlResolver? XmlResolver { get; set; }
    public WhitespaceHandling WhitespaceHandling { get; set; }

    internal XmlReaderSettings CloneForReader() => new()
    {
        Async = Async,
        CloseInput = CloseInput,
        ConformanceLevel = ConformanceLevel,
        DtdProcessing = DtdProcessing,
        MaxCharactersFromEntities = MaxCharactersFromEntities,
        MaxCharactersInDocument = MaxCharactersInDocument,
        NameTable = NameTable ?? new NameTable(),
        XmlResolver = XmlResolver,
        WhitespaceHandling = WhitespaceHandling,
    };
}

public sealed class XmlWriterSettings
{
    public XmlWriterSettings()
    {
        Encoding = Encoding.UTF8;
        ConformanceLevel = ConformanceLevel.Document;
        NewLineChars = "\n";
    }

    public bool Async { get; set; }
    public bool CloseOutput { get; set; }
    public ConformanceLevel ConformanceLevel { get; set; }
    public Encoding Encoding { get; set; }
    public bool Indent { get; set; }
    public NamespaceHandling NamespaceHandling { get; set; }
    public string NewLineChars { get; set; }
    public bool OmitXmlDeclaration { get; set; }

    internal XmlWriterSettings CloneForWriter() => new()
    {
        Async = Async,
        CloseOutput = CloseOutput,
        ConformanceLevel = ConformanceLevel,
        Encoding = Encoding,
        Indent = Indent,
        NamespaceHandling = NamespaceHandling,
        NewLineChars = NewLineChars,
        OmitXmlDeclaration = OmitXmlDeclaration,
    };
}
