// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public abstract class HttpContent : IDisposable
{
    private bool _disposed;

    protected HttpContent()
    {
        Headers = new HttpContentHeaders();
    }

    public HttpContentHeaders Headers { get; }

    public async Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var stream = new MemoryStream();
        await SerializeToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        return stream.ToArray();
    }

    public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var buffer = new MemoryStream();
        await SerializeToStreamAsync(buffer, cancellationToken).ConfigureAwait(false);
        return new MemoryStream(buffer.ToArray(), writable: false);
    }

    public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException();
        }

        ThrowIfDisposed();
        return SerializeToStreamAsync(stream, cancellationToken);
    }

    public async Task LoadIntoBufferAsync(CancellationToken cancellationToken = default)
    {
        _ = await ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    protected abstract void SerializeToStream(Stream stream, CancellationToken cancellationToken);

    protected virtual Task SerializeToStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        SerializeToStream(stream, cancellationToken);
        return Task.CompletedTask;
    }

    protected abstract bool TryComputeLength(out long length);

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(null);
        }
    }
}
