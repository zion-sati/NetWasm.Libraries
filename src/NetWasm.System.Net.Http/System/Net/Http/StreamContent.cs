// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public sealed class StreamContent : HttpContent
{
    private readonly Stream _content;

    public StreamContent(Stream content)
    {
        _content = content ?? throw new ArgumentNullException();
    }

    protected override void SerializeToStream(Stream stream, CancellationToken cancellationToken) =>
        _content.CopyTo(stream);

    protected override Task SerializeToStreamAsync(
        Stream stream,
        CancellationToken cancellationToken) =>
        _content.CopyToAsync(stream, cancellationToken);

    protected override bool TryComputeLength(out long length)
    {
        if (_content.CanSeek)
        {
            length = _content.Length - _content.Position;
            return true;
        }

        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _content.Dispose();
        }

        base.Dispose(disposing);
    }
}
