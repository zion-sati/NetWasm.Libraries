// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http.Wasi02;

namespace System.Net.Http;

public class HttpClient : IDisposable
{
    internal static readonly TimeSpan InfiniteTimeout = TimeSpan.FromMilliseconds(-1);
    private readonly HttpMessageInvoker _invoker;
    private long _maxResponseContentBufferSize = int.MaxValue;
    private TimeSpan _timeout;
    private Uri? _baseAddress;
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
        _timeout = InfiniteTimeout;
    }

    public HttpClient(IHttpTransport transport)
        : this(new WasiHttpMessageHandler(transport))
    {
    }

    public HttpRequestHeaders DefaultRequestHeaders { get; }

    public Uri? BaseAddress
    {
        get => _baseAddress;
        set
        {
            if (value is not null && !value.IsAbsoluteUri)
            {
                throw new ArgumentException("The base address must be absolute.", nameof(value));
            }

            _baseAddress = value;
        }
    }

    public TimeSpan Timeout
    {
        get => _timeout;
        set
        {
            if (value != InfiniteTimeout &&
                value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException();
            }

            _timeout = value;
        }
    }

    public long MaxResponseContentBufferSize
    {
        get => _maxResponseContentBufferSize;
        set
        {
            if (value <= 0 || value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            _maxResponseContentBufferSize = value;
        }
    }

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

        if (request.RequestUri is null)
        {
            if (_baseAddress is null)
            {
                throw new InvalidOperationException("A request URI or BaseAddress is required.");
            }

            request.RequestUri = _baseAddress;
        }
        else if (!request.RequestUri.IsAbsoluteUri)
        {
            if (_baseAddress is null)
            {
                throw new InvalidOperationException("A relative request URI requires BaseAddress.");
            }

            request.RequestUri = new Uri(_baseAddress, request.RequestUri);
        }

        if (completionOption is not HttpCompletionOption.ResponseContentRead and
            not HttpCompletionOption.ResponseHeadersRead)
        {
            throw new ArgumentOutOfRangeException();
        }

        foreach (var header in DefaultRequestHeaders.Entries())
        {
            if (!request.Headers.Contains(header.Key))
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        return SendAsyncCore(request, completionOption, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(
        Uri requestUri,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            new HttpRequestMessage(HttpMethod.Get, requestUri),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

    public Task<HttpResponseMessage> GetAsync(
        Uri requestUri,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);

    public Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default) =>
        GetAsync(new Uri(requestUri ?? throw new ArgumentNullException()), cancellationToken);

    public Task<HttpResponseMessage> GetAsync(
        string requestUri,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default) =>
        GetAsync(new Uri(requestUri ?? throw new ArgumentNullException()), completionOption, cancellationToken);

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

    public Task<HttpResponseMessage> PostAsync(
        Uri requestUri,
        HttpContent? content,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content,
            },
            completionOption,
            cancellationToken);

    public Task<HttpResponseMessage> PostAsync(
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken = default) =>
        PostAsync(new Uri(requestUri ?? throw new ArgumentNullException()), content, cancellationToken);

    public Task<HttpResponseMessage> PostAsync(
        string requestUri,
        HttpContent? content,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default) =>
        PostAsync(new Uri(requestUri ?? throw new ArgumentNullException()), content, completionOption, cancellationToken);

    public Task<HttpResponseMessage> PutAsync(
        Uri requestUri,
        HttpContent? content,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = content,
            },
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

    public Task<HttpResponseMessage> PutAsync(
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken = default) =>
        PutAsync(new Uri(requestUri ?? throw new ArgumentNullException()), content, cancellationToken);

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

    private async Task<HttpResponseMessage> SendAsyncCore(
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = _timeout == InfiniteTimeout
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutCts is not null)
        {
            timeoutCts.CancelAfter(_timeout);
        }

        var operationToken = timeoutCts?.Token ?? cancellationToken;
        try
        {
            var response = await _invoker.SendAsync(request, operationToken).ConfigureAwait(false);
            try
            {
                if (completionOption == HttpCompletionOption.ResponseContentRead && response.Content is not null)
                {
                    await response.Content
                        .LoadIntoBufferAsync(MaxResponseContentBufferSize, operationToken)
                        .ConfigureAwait(false);
                }

                return response;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
        catch (OperationCanceledException exception)
            when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException(
                $"The request timed out after { _timeout.TotalSeconds } seconds.",
                new TimeoutException(exception.Message, exception),
                exception.CancellationToken);
        }
    }
}
