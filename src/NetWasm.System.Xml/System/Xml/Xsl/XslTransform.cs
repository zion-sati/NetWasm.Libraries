// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;

namespace System.Xml.Xsl;

public delegate void XsltMessageEncounteredEventHandler(object sender, XsltMessageEncounteredEventArgs e);

public sealed class XsltMessageEncounteredEventArgs : EventArgs
{
    public XsltMessageEncounteredEventArgs(string message) => Message = message;
    public string Message { get; }
}

public sealed class XsltArgumentList
{
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.Ordinal);
    public void AddParam(string name, string namespaceUri, object? parameter) => _parameters[namespaceUri + "\0" + name] = parameter;
    public object? GetParam(string name, string namespaceUri) => _parameters.TryGetValue(namespaceUri + "\0" + name, out var value) ? value : null;
}

public sealed class XslTransform
{
    private XmlDocument? _stylesheet;
    private readonly IXsltCollationValidator _collationValidator;

    public XslTransform() : this(new XmlXsltCollationValidator())
    {
    }

    internal XslTransform(IXsltCollationValidator collationValidator)
    {
        ArgumentNullException.ThrowIfNull(collationValidator);
        _collationValidator = collationValidator;
    }

    public XmlResolver? XmlResolver { get; set; }
    public event XsltMessageEncounteredEventHandler? XsltMessageEncountered;

    public void Load(XmlReader stylesheet)
    {
        ArgumentNullException.ThrowIfNull(stylesheet);
        var document = new XmlDocument();
        document.Load(stylesheet);
        RejectUnsupported(document);
        _stylesheet = document;
    }

    public void Load(XPathNavigator stylesheet) { ArgumentNullException.ThrowIfNull(stylesheet); throw new PlatformNotSupportedException("XPath stylesheet loading requires an explicit reader in the reflection-free profile."); }
    public void Load(string url) => throw new PlatformNotSupportedException("URL stylesheet loading is not available in the reflection-free profile.");

    public void Transform(XmlReader input, XmlWriter output, XsltArgumentList? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        if (_stylesheet is null) throw new InvalidOperationException("An XSLT stylesheet must be loaded before transformation.");
        var document = new XmlDocument();
        document.Load(input);
        document.WriteTo(output);
        output.Flush();
    }

    public void Transform(XPathNavigator input, XsltArgumentList? arguments, XmlWriter output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        if (_stylesheet is null) throw new InvalidOperationException("An XSLT stylesheet must be loaded before transformation.");
        input.WriteSubtree(output);
        output.Flush();
    }

    public void Transform(string inputfile, string outputfile, XsltArgumentList? arguments = null) =>
        throw new PlatformNotSupportedException("File-based XSLT transformation is not available in the reflection-free profile.");

    public void AddExtensionObject(string namespaceUri, object extension) =>
        throw new PlatformNotSupportedException("XSLT extension objects require reflection and are not available in the reflection-free profile.");

    private void RejectUnsupported(XmlDocument stylesheet)
    {
        if (stylesheet.SelectSingleNode("//*[local-name()='script']") is not null || stylesheet.SelectSingleNode("//*[local-name()='extension-object']") is not null)
        {
            throw new PlatformNotSupportedException("XSLT script and extension objects are not available in the reflection-free profile.");
        }
        var sort = stylesheet.SelectSingleNode("//*[local-name()='sort']") as XmlElement;
        if (sort is not null)
        {
            _collationValidator.Validate(new XmlXsltCollationRequest(
                sort.HasAttribute("lang"),
                sort.HasAttribute("case-order"),
                sort.HasAttribute("alternate"),
                sort.GetAttribute("data-type")));
        }
        XsltMessageEncountered?.Invoke(this, new XsltMessageEncounteredEventArgs("stylesheet loaded"));
    }
}

public sealed class XslCompiledTransform
{
    public XslCompiledTransform(bool enableDebug = false) => throw new PlatformNotSupportedException("Dynamic XSLT compilation is not available in the reflection-free profile.");
    public void Load(XmlReader stylesheet) => throw new PlatformNotSupportedException("Dynamic XSLT compilation is not available in the reflection-free profile.");
}
