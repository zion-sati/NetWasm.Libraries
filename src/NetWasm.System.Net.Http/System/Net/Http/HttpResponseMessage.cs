// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

namespace System.Net.Http;

public sealed class HttpResponseMessage : IDisposable
{
    private bool _disposed;
    private HttpStatusCode _statusCode;
    private HttpContent? _content;

    public HttpResponseMessage()
        : this(HttpStatusCode.OK)
    {
    }

    public HttpResponseMessage(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
        Headers = new HttpResponseHeaders();
    }

    public HttpStatusCode StatusCode
    {
        get => _statusCode;
        set
        {
            var numericValue = (int)value;
            if (numericValue < 100 || numericValue > 999)
            {
                throw new ArgumentOutOfRangeException();
            }

            _statusCode = value;
        }
    }

    public string? ReasonPhrase { get; set; }

    public HttpResponseHeaders Headers { get; }

    public HttpContent? Content
    {
        get => _content;
        set
        {
            ThrowIfDisposed();
            _content = value;
        }
    }

    public bool IsSuccessStatusCode =>
        (int)StatusCode >= 200 && (int)StatusCode <= 299;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _content?.Dispose();
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
