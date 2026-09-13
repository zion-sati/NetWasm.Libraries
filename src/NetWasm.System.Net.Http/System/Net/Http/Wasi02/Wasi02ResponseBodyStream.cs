// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Wasi02;

internal sealed class Wasi02ResponseBodyStream : Stream
{
    private readonly Wasi02ResourceLease<IWasi02InputStream> _lease;
    private readonly IWasi02ReadinessAwaiter _readiness;
    private readonly IHttpFailureMapper _failureMapper;
    private long _position;
    private bool _disposed;

    internal Wasi02ResponseBodyStream(
        Wasi02ResourceLease<IWasi02InputStream> lease,
        IWasi02ReadinessAwaiter readiness,
        IHttpFailureMapper failureMapper)
    {
        _lease = lease ?? throw new ArgumentNullException();
        _readiness = readiness ?? throw new ArgumentNullException();
        _failureMapper = failureMapper ?? throw new ArgumentNullException();
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush() => ThrowIfDisposed();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException();
        }

        if (offset < 0 || count < 0 || offset > buffer.Length - count)
        {
            throw new ArgumentOutOfRangeException();
        }

        return ReadCoreAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        ReadCoreAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _lease.Dispose();
        }

        base.Dispose(disposing);
    }

    private async ValueTask<int> ReadCoreAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (buffer.Length == 0)
        {
            return 0;
        }

        while (true)
        {
            var result = await _lease.Resource
                .ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
            switch (result.Status)
            {
                case Wasi02ReadStatus.Data:
                    if (result.BytesRead < 0 || result.BytesRead > buffer.Length)
                    {
                        throw new IOException();
                    }

                    _position += result.BytesRead;
                    return result.BytesRead;
                case Wasi02ReadStatus.WouldBlock:
                    await _readiness.WaitAsync(
                        _lease.Resource.Subscribe(),
                        cancellationToken).ConfigureAwait(false);
                    continue;
                case Wasi02ReadStatus.EndOfStream:
                    return 0;
                case Wasi02ReadStatus.Failed:
                    throw _failureMapper.Translate(result.Failure!.Value);
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(null);
        }
    }
}

internal sealed class Wasi02ResponseBodyStreamFactory : IWasi02ResponseBodyStreamFactory
{
    private readonly IWasi02ReadinessAwaiter _readiness;
    private readonly IHttpFailureMapper _failureMapper;

    internal Wasi02ResponseBodyStreamFactory(
        IWasi02ReadinessAwaiter readiness,
        IHttpFailureMapper failureMapper)
    {
        _readiness = readiness ?? throw new ArgumentNullException();
        _failureMapper = failureMapper ?? throw new ArgumentNullException();
    }

    public Stream Create(IWasi02InputStream inputStream)
    {
        if (inputStream is null)
        {
            throw new ArgumentNullException();
        }

        return new Wasi02ResponseBodyStream(
            new Wasi02ResourceLease<IWasi02InputStream>(inputStream),
            _readiness,
            _failureMapper);
    }
}
