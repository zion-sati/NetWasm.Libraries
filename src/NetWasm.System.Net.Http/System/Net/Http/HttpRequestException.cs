// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http;

public class HttpRequestException : Exception
{
    public HttpRequestException()
    {
    }

    public HttpRequestException(string message)
        : base(message)
    {
    }

    public HttpRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpStatusCode? StatusCode { get; internal set; }

    public HttpRequestError HttpRequestError { get; internal set; }
}
