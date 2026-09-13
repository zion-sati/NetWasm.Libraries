// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;

namespace System.Net.Http;

public class StringContent : ByteArrayContent
{
    public StringContent(string content)
        : this(content, Encoding.UTF8, "text/plain")
    {
    }

    public StringContent(string content, Encoding encoding, string mediaType)
        : base((content ?? throw new ArgumentNullException()).ToCharArrayBytes(encoding ?? throw new ArgumentNullException()))
    {
        Headers.ContentType = new Headers.MediaTypeHeaderValue(mediaType ?? throw new ArgumentNullException());
    }
}

internal static class StringContentExtensions
{
    internal static byte[] ToCharArrayBytes(this string content, Encoding encoding) => encoding.GetBytes(content);
}
