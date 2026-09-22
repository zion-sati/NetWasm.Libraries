using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationPath
{
    public static readonly string KeyDelimiter = ":";

    public static string Combine(params string[] pathSegments)
    {
        ArgumentNullException.ThrowIfNull(pathSegments);
        return string.Join(KeyDelimiter, pathSegments);
    }

    public static string Combine(IEnumerable<string> pathSegments)
    {
        ArgumentNullException.ThrowIfNull(pathSegments);
        return string.Join(KeyDelimiter, pathSegments);
    }

    public static string? GetSectionKey(string? path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        string nonNullPath = path!;
        int index = nonNullPath.LastIndexOf(':');
        return index < 0 ? nonNullPath : nonNullPath.Substring(index + 1);
    }

    public static string? GetParentPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string nonNullPath = path!;
        int index = nonNullPath.LastIndexOf(':');
        return index < 0 ? null : nonNullPath.Substring(0, index);
    }
}
