// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http.Wasi02;

namespace System.Net.Http;

public class HttpClient : IDisposable
{
    private readonly HttpMessageInvoker _invoker;
    private bool _disposed;

    public HttpClient()
        : this(Wasi02HttpTransportComposition.Create())
    {
    }

    public HttpClient(HttpMessageHandler handler)
        : this(handler, disposeHandler: true)
    {
    }

    public HttpClient(HttpMessageHandler handler, bool disposeHandler)
    {
        _invoker = new HttpMessageInvoker(handler, disposeHandler);
        DefaultRequestHeaders = new HttpRequestHeaders();
        Timeout = TimeSpan.FromMilliseconds(-1);
    }

    public HttpClient(IHttpTransport transport)
        : this(new WasiHttpMessageHandler(transport))
    {
    }

    public HttpRequestHeaders DefaultRequestHeaders { get; }

    public TimeSpan Timeout { get; set; }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
        SendAsync(request, HttpCompletionOption.ResponseContentRead, default);

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (request is null)
        {
            throw new ArgumentNullException();
        }

        return _invoker.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(
        Uri requestUri,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            new HttpRequestMessage(HttpMethod.Get, requestUri),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

    public Task<HttpResponseMessage> PostAsync(
        Uri requestUri,
        HttpContent? content,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content,
            },
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _invoker.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            // NetWasm's trimmed CoreLib does not retain reflection names for
            // every framework type. The object identity is not part of the
            // public disposal contract, so keep this path metadata-free.
            throw new ObjectDisposedException(null);
        }
    }
}
