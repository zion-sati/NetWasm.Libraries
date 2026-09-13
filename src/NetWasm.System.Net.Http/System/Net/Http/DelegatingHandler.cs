// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public abstract class DelegatingHandler : HttpMessageHandler
{
    private HttpMessageHandler? _innerHandler;

    protected DelegatingHandler()
    {
    }

    protected DelegatingHandler(HttpMessageHandler innerHandler)
    {
        InnerHandler = innerHandler;
    }

    public HttpMessageHandler? InnerHandler
    {
        get => _innerHandler;
        set
        {
            ThrowIfDisposed();
            if (_innerHandler is not null)
            {
                throw new InvalidOperationException();
            }

            _innerHandler = value ?? throw new ArgumentNullException();
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_innerHandler is null)
        {
            throw new InvalidOperationException();
        }

        return _innerHandler.SendAsyncCore(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerHandler?.Dispose();
        }

        base.Dispose(disposing);
    }
}
