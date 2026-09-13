// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema;

public static class XmlSchemaValidationExtensions
{
    public static bool TryValidate(this XmlDocument document, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(schemas);
        try
        {
            document.Validate(schemas, validationEventHandler);
            return true;
        }
        catch (XmlSchemaException)
        {
            return false;
        }
    }
}
