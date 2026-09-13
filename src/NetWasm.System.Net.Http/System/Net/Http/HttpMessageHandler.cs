// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public abstract class HttpMessageHandler : IDisposable
{
    private bool _disposed;

    protected abstract Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);

    internal Task<HttpResponseMessage> SendAsyncCore(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return SendAsync(request, cancellationToken);
    }

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
