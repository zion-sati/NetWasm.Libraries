// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.Wasi02;

internal sealed class Wasi02FailureMapper : IHttpFailureMapper
{
    public HttpRequestException Translate(Wasi02FailureFacts failure)
    {
        var exception = new HttpRequestException(
            failure.Message ?? "WASI HTTP request failed.");
        exception.HttpRequestError = MapError(failure.Kind);
        return exception;
    }

    private static HttpRequestError MapError(Wasi02FailureKind kind) => kind switch
    {
        Wasi02FailureKind.DnsTimeout or Wasi02FailureKind.DnsError =>
            HttpRequestError.NameResolutionError,
        Wasi02FailureKind.TlsProtocolError or Wasi02FailureKind.TlsCertificateError or
            Wasi02FailureKind.TlsAlertReceived => HttpRequestError.SecureConnectionError,
        Wasi02FailureKind.HttpRequestMethodInvalid or Wasi02FailureKind.HttpRequestUriInvalid or
            Wasi02FailureKind.HttpRequestUriTooLong or Wasi02FailureKind.HttpRequestHeaderSectionSize or
            Wasi02FailureKind.HttpRequestHeaderSize or Wasi02FailureKind.HttpRequestTrailerSectionSize or
            Wasi02FailureKind.HttpRequestTrailerSize or Wasi02FailureKind.HttpResponseHeaderSectionSize or
            Wasi02FailureKind.HttpResponseHeaderSize or Wasi02FailureKind.HttpResponseTrailerSectionSize or
            Wasi02FailureKind.HttpResponseTrailerSize or Wasi02FailureKind.HttpResponseTransferCoding or
            Wasi02FailureKind.HttpResponseContentCoding or Wasi02FailureKind.HttpProtocolError or
            Wasi02FailureKind.HttpResponseIncomplete or Wasi02FailureKind.HttpResponseBodySize or
            Wasi02FailureKind.HttpRequestBodySize or Wasi02FailureKind.HttpRequestLengthRequired =>
            HttpRequestError.InvalidResponse,
        Wasi02FailureKind.ConfigurationError => HttpRequestError.ConfigurationError,
        _ => HttpRequestError.ConnectionError,
    };
}
