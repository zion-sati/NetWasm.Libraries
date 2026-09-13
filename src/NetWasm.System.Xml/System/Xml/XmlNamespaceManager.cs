// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Xml;

/// <summary>Resolves namespace prefixes used by an XPath expression.</summary>
public class XmlNamespaceManager
{
    private readonly Dictionary<string, string> _prefixes = new(StringComparer.Ordinal);
    private readonly XmlNameTable _nameTable;

    public XmlNamespaceManager(XmlNameTable nameTable)
    {
        _nameTable = nameTable ?? throw new ArgumentNullException(nameof(nameTable));
        _prefixes[string.Empty] = string.Empty;
        _prefixes["xml"] = "http://www.w3.org/XML/1998/namespace";
    }

    public XmlNameTable NameTable => _nameTable;
    public string DefaultNamespace => LookupNamespace(string.Empty) ?? string.Empty;

    public virtual void AddNamespace(string prefix, string uri)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(uri);
        if (prefix is "xml" or "xmlns")
        {
            throw new ArgumentException("The reserved XML prefix cannot be rebound.", nameof(prefix));
        }

        _prefixes[prefix] = uri;
    }

    public virtual bool HasNamespace(string prefix) => LookupNamespace(prefix) is not null;
    public virtual string? LookupNamespace(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return _prefixes.TryGetValue(prefix, out var uri) ? uri : null;
    }

    public virtual string? LookupPrefix(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        foreach (var pair in _prefixes)
        {
            if (StringComparer.Ordinal.Equals(pair.Value, uri)) return pair.Key;
        }

        return null;
    }

    public virtual IEnumerator GetEnumerator() => _prefixes.Keys.GetEnumerator();
}
