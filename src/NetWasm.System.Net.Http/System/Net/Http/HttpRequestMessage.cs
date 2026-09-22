// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http.Headers;

namespace System.Net.Http;

public sealed class HttpRequestMessage : IDisposable
{
    private bool _disposed;
    private HttpMethod _method;
    private Uri? _requestUri;
    private HttpContent? _content;

    public HttpRequestMessage(HttpMethod method, Uri? requestUri)
    {
        _method = method ?? throw new ArgumentNullException();
        _requestUri = requestUri;

        Headers = new HttpRequestHeaders();
        Options = new HttpRequestOptions();
    }

    public HttpRequestMessage(HttpMethod method, string? requestUri)
        : this(method, requestUri is null ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute))
    {
    }

    public HttpMethod Method
    {
        get => _method;
        set => _method = value ?? throw new ArgumentNullException();
    }

    public Uri? RequestUri
    {
        get => _requestUri;
        set => _requestUri = value;
    }

    public HttpRequestHeaders Headers { get; }

    public HttpContent? Content
    {
        get => _content;
        set
        {
            ThrowIfDisposed();
            _content = value;
        }
    }

    public HttpRequestOptions Options { get; }

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

public sealed class HttpRequestOptions
{
    private readonly Dictionary<object, object?> _values = new();

    public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value) =>
        _values[key] = value;

    public bool TryGetValue<TValue>(HttpRequestOptionsKey<TValue> key, out TValue? value)
    {
        if (_values.TryGetValue(key, out var boxed) && boxed is TValue typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}

public readonly struct HttpRequestOptionsKey<TValue>
{
    public HttpRequestOptionsKey(string name)
    {
        Name = name ?? throw new ArgumentNullException();
    }

    public string Name { get; }
}
