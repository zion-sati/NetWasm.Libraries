// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.Headers;

public sealed class MediaTypeHeaderValue
{
    public MediaTypeHeaderValue(string mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
        {
            throw new ArgumentException();
        }

        var separator = mediaType.IndexOf(';');
        MediaType = (separator < 0 ? mediaType : mediaType[..separator]).Trim();
        if (MediaType.Length == 0)
        {
            throw new ArgumentException();
        }

        if (separator >= 0)
        {
            var parameters = mediaType[(separator + 1)..].Split(';');
            foreach (var parameter in parameters)
            {
                var equals = parameter.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                var name = parameter[..equals].Trim();
                if (!name.Equals("charset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var charset = parameter[(equals + 1)..].Trim();
                if (charset.Length >= 2 && charset[0] == '"' && charset[^1] == '"')
                {
                    charset = charset[1..^1];
                }

                CharSet = charset;
                break;
            }
        }
    }

    public string MediaType { get; }

    public string? CharSet { get; set; }

    public override string ToString() =>
        CharSet is null ? MediaType : $"{MediaType}; charset={CharSet}";
}
