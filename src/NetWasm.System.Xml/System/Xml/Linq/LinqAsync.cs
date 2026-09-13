// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace System.Xml.Linq;

public static class LinqXmlExtensions
{
    public static async Task<XDocument> LoadAsync(Stream stream, LoadOptions options = LoadOptions.None, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();
        using var reader = XmlReader.Create(stream);
        return await LoadAsync(reader, options, cancellationToken);
    }

    public static async Task<XDocument> LoadAsync(TextReader reader, LoadOptions options = LoadOptions.None, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        cancellationToken.ThrowIfCancellationRequested();
        using var input = XmlReader.Create(reader);
        return await LoadAsync(input, options, cancellationToken);
    }

    public static async Task<XDocument> LoadAsync(XmlReader reader, LoadOptions options = LoadOptions.None, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        cancellationToken.ThrowIfCancellationRequested();
        return XDocument.Load(reader);
    }

    public static Task SaveAsync(this XDocument document, Stream stream, SaveOptions options = SaveOptions.None, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();
        document.Save(stream);
        return Task.CompletedTask;
    }

    public static Task SaveAsync(this XElement element, Stream stream, SaveOptions options = SaveOptions.None, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { OmitXmlDeclaration = true });
        element.WriteTo(writer);
        writer.Flush();
        return Task.CompletedTask;
    }

    public static IEnumerable<XElement> XPathSelectElements(this XElement element, string expression)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(expression);
        var name = expression.Trim();
        var separator = name.LastIndexOf('/');
        if (separator >= 0) name = name.Substring(separator + 1);
        if (name.StartsWith("@", StringComparison.Ordinal)) yield break;
        if (name.Length == 0 || name == "*") { foreach (var descendant in element.Descendants()) yield return descendant; yield break; }
        foreach (var descendant in element.Descendants(XName.Get(name))) yield return descendant;
    }

    public static IEnumerable<XElement> XPathSelectElements(this XDocument document, string expression) =>
        document.Root is null ? [] : document.Root.XPathSelectElements(expression);
}

[Flags]
public enum LoadOptions { None = 0, PreserveWhitespace = 1, SetBaseUri = 2, SetLineInfo = 4 }
