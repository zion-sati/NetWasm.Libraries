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
    private byte[]? _bufferedContent;
    private Stream? _contentReadStream;
    private bool _disposed;

    protected HttpContent()
    {
        Headers = new HttpContentHeaders();
    }

    public HttpContentHeaders Headers { get; }

    public async Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_bufferedContent is not null)
        {
            return (byte[])_bufferedContent.Clone();
        }

        using var stream = new MemoryStream();
        await SerializeToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        _bufferedContent = stream.ToArray();
        return (byte[])_bufferedContent.Clone();
    }

    public async Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_bufferedContent is not null)
        {
            return new MemoryStream(_bufferedContent, writable: false);
        }

        if (_contentReadStream is null)
        {
            _contentReadStream = await CreateContentReadStreamAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return _contentReadStream;
    }

    public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException();
        }

        ThrowIfDisposed();
        if (_bufferedContent is not null)
        {
            return stream.WriteAsync(_bufferedContent, 0, _bufferedContent.Length, cancellationToken);
        }

        return SerializeToStreamAsync(stream, cancellationToken);
    }

    public async Task LoadIntoBufferAsync(CancellationToken cancellationToken = default)
    {
        _ = await ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task LoadIntoBufferAsync(long maxLength, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (_bufferedContent is not null)
        {
            if (_bufferedContent.LongLength > maxLength)
            {
                throw new HttpRequestException("The response content exceeded the configured buffer limit.");
            }

            return;
        }

        if (Headers.ContentLength is long contentLength && contentLength > maxLength)
        {
            throw new HttpRequestException("The response content exceeded the configured buffer limit.");
        }

        using var buffer = new MemoryStream();
        using var limited = new LengthLimitWriteStream(buffer, maxLength);
        await SerializeToStreamAsync(limited, cancellationToken).ConfigureAwait(false);
        _bufferedContent = buffer.ToArray();
    }

    protected abstract void SerializeToStream(Stream stream, CancellationToken cancellationToken);

    protected virtual Task SerializeToStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        SerializeToStream(stream, cancellationToken);
        return Task.CompletedTask;
    }

    protected virtual Task<Stream> CreateContentReadStreamAsync(
        CancellationToken cancellationToken)
    {
        return CreateContentReadStreamCoreAsync(cancellationToken);
    }

    protected Stream? ContentReadStream => _contentReadStream;

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
        if (disposing)
        {
            _contentReadStream?.Dispose();
            _contentReadStream = null;
        }
    }

    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(null);
        }
    }

    private async Task<Stream> CreateContentReadStreamCoreAsync(CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await SerializeToStreamAsync(buffer, cancellationToken).ConfigureAwait(false);
        _bufferedContent = buffer.ToArray();
        return new MemoryStream(_bufferedContent, writable: false);
    }

    private sealed class LengthLimitWriteStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _limit;
        private long _written;

        public LengthLimitWriteStream(Stream inner, long limit)
        {
            _inner = inner;
            _limit = limit;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _written;
        public override long Position
        {
            get => _written;
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 0 || _written > _limit - count)
            {
                throw new HttpRequestException("The response content exceeded the configured buffer limit.");
            }

            _inner.Write(buffer, offset, count);
            _written += count;
        }

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_written > _limit - buffer.Length)
            {
                throw new HttpRequestException("The response content exceeded the configured buffer limit.");
            }

            _written += buffer.Length;
            return WriteAsyncCore(buffer, cancellationToken);
        }

        private async ValueTask WriteAsyncCore(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken)
        {
            try
            {
                await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _written -= buffer.Length;
                throw;
            }
        }
    }
}
