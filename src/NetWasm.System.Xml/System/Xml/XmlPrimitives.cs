using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The public names and enum values follow dotnet/runtime commit
// 811225a482702af7ecc35d817966bc70b88a3a23. The synchronous implementation
// below is the deliberately narrow NetWasm ReaderWriter profile.

namespace System.Xml;

public enum XmlNodeType
{
    None,
    Element,
    Attribute,
    Text,
    CDATA,
    EntityReference,
    Entity,
    ProcessingInstruction,
    Comment,
    Document,
    DocumentType,
    XmlDeclaration,
    EndElement,
    EndEntity,
    Whitespace,
    SignificantWhitespace,
}

public enum ReadState
{
    Initial,
    Interactive,
    Error,
    EndOfFile,
    Closed,
}

public enum ConformanceLevel
{
    Auto,
    Fragment,
    Document,
}

public enum DtdProcessing
{
    Prohibit,
    Ignore,
    Parse,
}

public enum NamespaceHandling
{
    Default,
    OmitDuplicates,
}

public enum WhitespaceHandling
{
    All,
    Significant,
    None,
}

public enum NewLineHandling
{
    Replace,
    Entitize,
    None,
}

public interface IXmlLineInfo
{
    int LineNumber { get; }
    int LinePosition { get; }
    bool HasLineInfo();
}

public abstract class XmlNameTable
{
    protected XmlNameTable() { }
    public abstract string Add(string array);
    public abstract string? Get(string value);
}

public sealed class NameTable : XmlNameTable
{
    private readonly System.Collections.Generic.Dictionary<string, string> _names = new(StringComparer.Ordinal);

    public override string Add(string array)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (_names.TryGetValue(array, out var existing))
        {
            return existing;
        }

        _names.Add(array, array);
        return array;
    }

    public override string? Get(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _names.TryGetValue(value, out var existing) ? existing : null;
    }
}

public sealed class XmlQualifiedName : IEquatable<XmlQualifiedName>
{
    public static readonly XmlQualifiedName Empty = new(string.Empty, string.Empty);

    public XmlQualifiedName() : this(string.Empty, string.Empty) { }

    public XmlQualifiedName(string name) : this(name, string.Empty) { }

    public XmlQualifiedName(string name, string? ns)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Namespace = ns ?? string.Empty;
    }

    public string Name { get; }
    public string Namespace { get; }

    public override string ToString() => Namespace.Length == 0 ? Name : Name;

    public bool Equals(XmlQualifiedName? other) =>
        other is not null &&
        StringComparer.Ordinal.Equals(Name, other.Name) &&
        StringComparer.Ordinal.Equals(Namespace, other.Namespace);

    public override bool Equals(object? obj) => obj is XmlQualifiedName other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Name, Namespace);
}

public sealed class XmlException : SystemException
{
    public XmlException(string message) : base(message) { }
    public XmlException(string message, Exception? innerException) : base(message, innerException) { }
}

public abstract class XmlResolver
{
    protected XmlResolver() { }

    private static readonly XmlResolver s_throwingResolver = new ThrowingResolver();

    private System.Net.ICredentials? _credentials;

    public virtual System.Net.ICredentials? Credentials
    {
        get => _credentials;
        set => _credentials = value;
    }

    public static XmlResolver ThrowingResolver => s_throwingResolver;

    public abstract object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn);

    public virtual Task<object?> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
        Task.FromResult(GetEntity(absoluteUri, role, ofObjectToReturn));

    public virtual bool SupportsType(Uri absoluteUri, Type? type) =>
        type is null || type == typeof(Stream) || type == typeof(TextReader);

    public virtual Uri ResolveUri(Uri? baseUri, string? relativeUri)
    {
        if (relativeUri is null)
        {
            return baseUri ?? new Uri(string.Empty, UriKind.Relative);
        }

        return baseUri is null ? new Uri(relativeUri, UriKind.RelativeOrAbsolute) : new Uri(baseUri, relativeUri);
    }
}

internal sealed class ThrowingResolver : XmlResolver
{
    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
        throw new PlatformNotSupportedException("External XML resources require an explicit application resolver.");
}

public sealed class XmlPreloadedResolver : XmlResolver
{
    private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);
    private readonly XmlResolver _fallback;

    public XmlPreloadedResolver() : this(ThrowingResolver) { }

    public XmlPreloadedResolver(XmlResolver fallback)
    {
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
    }

    public void Add(Uri uri, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(value);
        _resources[uri.AbsoluteUri] = [.. value];
    }

    public void Add(Uri uri, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Add(uri, Encoding.UTF8.GetBytes(value));
    }

    public void Add(Uri uri, Stream value)
    {
        ArgumentNullException.ThrowIfNull(value);
        using var buffer = new MemoryStream();
        value.CopyTo(buffer);
        Add(uri, buffer.ToArray());
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        ArgumentNullException.ThrowIfNull(absoluteUri);
        if (!_resources.TryGetValue(absoluteUri.AbsoluteUri, out var bytes))
        {
            return _fallback.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        if (ofObjectToReturn is null || ofObjectToReturn == typeof(Stream))
        {
            return new MemoryStream(bytes, writable: false);
        }

        if (ofObjectToReturn == typeof(TextReader))
        {
            return new StreamReader(new MemoryStream(bytes, writable: false), Encoding.UTF8);
        }

        throw new XmlException("The preloaded XML resource type is not supported.");
    }

    public override Task<object?> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
        Task.FromResult(GetEntity(absoluteUri, role, ofObjectToReturn));
}

[Obsolete("Use XmlResolver.ThrowingResolver when external XML resources must be denied.")]
public sealed class XmlSecureResolver : XmlResolver
{
    public XmlSecureResolver(XmlResolver resolver, string? securityUrl)
    {
        ArgumentNullException.ThrowIfNull(resolver);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
        throw new PlatformNotSupportedException("Secure XML resource resolution is not available in the reflection-free profile.");
}
