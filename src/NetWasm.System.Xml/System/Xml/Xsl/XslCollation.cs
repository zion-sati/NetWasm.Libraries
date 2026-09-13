// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl;

public readonly struct XmlXsltCollationRequest
{
    public XmlXsltCollationRequest(bool hasLanguage, bool hasCaseOrder, bool hasAlternate, string? dataType)
    {
        HasLanguage = hasLanguage;
        HasCaseOrder = hasCaseOrder;
        HasAlternate = hasAlternate;
        DataType = dataType;
    }

    public bool HasLanguage { get; }
    public bool HasCaseOrder { get; }
    public bool HasAlternate { get; }
    public string? DataType { get; }
}

public interface IXsltCollationValidator
{
    void Validate(XmlXsltCollationRequest request);
}

public sealed class XmlXsltCollationValidator : IXsltCollationValidator
{
    public void Validate(XmlXsltCollationRequest request)
    {
        if (request.HasLanguage || request.HasCaseOrder || request.HasAlternate ||
            string.Equals(request.DataType, "text", StringComparison.OrdinalIgnoreCase))
        {
            throw new PlatformNotSupportedException("Culture-dependent XSLT collation is not available in the reflection-free profile.");
        }

        if (!string.IsNullOrEmpty(request.DataType) &&
            !string.Equals(request.DataType, "number", StringComparison.OrdinalIgnoreCase))
        {
            throw new PlatformNotSupportedException("The requested XSLT collation data type is not available in the reflection-free profile.");
        }
    }
}
