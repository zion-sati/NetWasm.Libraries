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

        MediaType = mediaType;
    }

    public string MediaType { get; }

    public override string ToString() => MediaType;
}
