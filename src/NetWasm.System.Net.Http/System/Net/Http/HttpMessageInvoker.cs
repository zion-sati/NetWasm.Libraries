// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class HttpMessageInvoker : IDisposable
{
    private readonly HttpMessageHandler _handler;
    private readonly bool _disposeHandler;
    private bool _disposed;

    public HttpMessageInvoker(HttpMessageHandler handler)
        : this(handler, disposeHandler: true)
    {
    }

    public HttpMessageInvoker(HttpMessageHandler handler, bool disposeHandler)
    {
        _handler = handler ?? throw new ArgumentNullException();
        _disposeHandler = disposeHandler;
    }

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(null);
        }

        if (request is null)
        {
            throw new ArgumentNullException();
        }

        return _handler.SendAsyncCore(request, cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_disposeHandler)
            {
                _handler.Dispose();
            }
        }
    }
}
