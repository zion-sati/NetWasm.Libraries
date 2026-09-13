// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http;

public sealed class HttpMethod : IEquatable<HttpMethod>
{
    public static readonly HttpMethod Get = new("GET");
    public static readonly HttpMethod Put = new("PUT");
    public static readonly HttpMethod Post = new("POST");
    public static readonly HttpMethod Delete = new("DELETE");
    public static readonly HttpMethod Head = new("HEAD");
    public static readonly HttpMethod Options = new("OPTIONS");
    public static readonly HttpMethod Trace = new("TRACE");
    public static readonly HttpMethod Patch = new("PATCH");

    public HttpMethod(string method)
    {
        if (string.IsNullOrEmpty(method))
        {
            throw new ArgumentException();
        }

        Method = method;
    }

    public string Method { get; }

    public bool Equals(HttpMethod? other) =>
        other is not null && string.Equals(Method, other.Method, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is HttpMethod other && Equals(other);

    public override int GetHashCode() => Method.ToUpperInvariant().GetHashCode();

    public override string ToString() => Method;
}
