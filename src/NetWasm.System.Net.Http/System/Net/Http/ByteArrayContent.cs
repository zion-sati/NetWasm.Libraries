// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;

namespace System.Net.Http;

public class ByteArrayContent : HttpContent
{
    private readonly byte[] _content;

    public ByteArrayContent(byte[] content)
    {
        _content = content ?? throw new ArgumentNullException();
    }

    protected override void SerializeToStream(Stream stream, CancellationToken cancellationToken) =>
        stream.Write(_content, 0, _content.Length);

    protected override bool TryComputeLength(out long length)
    {
        length = _content.Length;
        return true;
    }
}
