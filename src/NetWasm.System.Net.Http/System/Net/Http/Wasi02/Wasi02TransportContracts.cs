// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Wasi02;

internal enum Wasi02FailureKind
{
    DnsTimeout,
    DnsError,
    DestinationNotFound,
    DestinationUnavailable,
    DestinationIpProhibited,
    DestinationIpUnroutable,
    ConnectionRefused,
    ConnectionTerminated,
    ConnectionTimeout,
    ConnectionReadTimeout,
    ConnectionWriteTimeout,
    ConnectionLimitReached,
    TlsProtocolError,
    TlsCertificateError,
    TlsAlertReceived,
    HttpRequestDenied,
    HttpRequestLengthRequired,
    HttpRequestBodySize,
    HttpRequestMethodInvalid,
    HttpRequestUriInvalid,
    HttpRequestUriTooLong,
    HttpRequestHeaderSectionSize,
    HttpRequestHeaderSize,
    HttpRequestTrailerSectionSize,
    HttpRequestTrailerSize,
    HttpResponseIncomplete,
    HttpResponseHeaderSectionSize,
    HttpResponseHeaderSize,
    HttpResponseBodySize,
    HttpResponseTrailerSectionSize,
    HttpResponseTrailerSize,
    HttpResponseTransferCoding,
    HttpResponseContentCoding,
    HttpResponseTimeout,
    HttpUpgradeFailed,
    HttpProtocolError,
    LoopDetected,
    ConfigurationError,
    InternalError,
}

internal readonly struct Wasi02FailureFacts
{
    internal Wasi02FailureFacts(Wasi02FailureKind kind, string? message = null)
    {
        Kind = kind;
        Message = message;
    }

    internal Wasi02FailureKind Kind { get; }
    internal string? Message { get; }
}

internal sealed class Wasi02TransportFailureException : Exception
{
    internal Wasi02TransportFailureException(Wasi02FailureFacts facts)
        : base(facts.Message)
    {
        Facts = facts;
    }

    internal Wasi02FailureFacts Facts { get; }
}

internal enum Wasi02ReadStatus
{
    Data,
    WouldBlock,
    EndOfStream,
    Failed,
}

internal readonly struct Wasi02ReadResult
{
    private Wasi02ReadResult(
        Wasi02ReadStatus status,
        int bytesRead,
        Wasi02FailureFacts? failure)
    {
        Status = status;
        BytesRead = bytesRead;
        Failure = failure;
    }

    internal Wasi02ReadStatus Status { get; }
    internal int BytesRead { get; }
    internal Wasi02FailureFacts? Failure { get; }

    internal static Wasi02ReadResult Data(int bytesRead) =>
        new(Wasi02ReadStatus.Data, bytesRead, null);

    internal static Wasi02ReadResult WouldBlock() =>
        new(Wasi02ReadStatus.WouldBlock, 0, null);

    internal static Wasi02ReadResult EndOfStream() =>
        new(Wasi02ReadStatus.EndOfStream, 0, null);

    internal static Wasi02ReadResult Failed(Wasi02FailureFacts failure) =>
        new(Wasi02ReadStatus.Failed, 0, failure);

    internal static Wasi02ReadResult Unknown() =>
        new((Wasi02ReadStatus)255, 0, null);
}

internal interface IWasi02Pollable : IDisposable
{
    int TakeHandle();
}

internal interface IWasi02ReactorRegistration : IDisposable
{
    Task Completion { get; }
}

// The concrete implementation is supplied by the existing reactor composition
// root. This contract does not create a second reactor or expose its WIT types.
internal interface IWasi02Reactor
{
    IWasi02ReactorRegistration Register(
        IWasi02Pollable pollable);
}

internal interface IWasi02ReadinessAwaiter
{
    Task WaitAsync(IWasi02Pollable pollable, CancellationToken cancellationToken);
}

internal interface IWasi02OutputStream : IDisposable
{
    ulong CheckWrite();

    void Write(byte[] contents);

    void Flush();

    IWasi02Pollable Subscribe();
}

internal interface IHttpRequestBodyWriter
{
    Task WriteAsync(
        IWasi02OutputStream output,
        byte[] contents,
        CancellationToken cancellationToken);
}

internal interface IWasi02InputStream : IDisposable
{
    IWasi02Pollable Subscribe();

    ValueTask<Wasi02ReadResult> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken);
}

internal interface IWasi02ResponseBodyStreamFactory
{
    Stream Create(IWasi02InputStream inputStream);
}

internal interface IWasi02OutgoingHandler
{
    Task<Wasi02IncomingResponse> SendAsync(
        HttpRequestMessage request,
        byte[]? body,
        CancellationToken cancellationToken);
}

internal sealed class Wasi02IncomingResponse
{
    internal Wasi02IncomingResponse(
        int statusCode,
        IEnumerable<KeyValuePair<string, string>> headers,
        IWasi02InputStream? body)
    {
        if (headers is null)
        {
            throw new ArgumentNullException();
        }

        var values = new List<KeyValuePair<string, string>>();
        foreach (var header in headers)
        {
            values.Add(header);
        }

        StatusCode = statusCode;
        Headers = values.ToArray();
        Body = body;
    }

    internal int StatusCode { get; }
    internal IReadOnlyList<KeyValuePair<string, string>> Headers { get; }
    internal IWasi02InputStream? Body { get; }
}

internal interface IHttpFailureMapper
{
    HttpRequestException Translate(Wasi02FailureFacts failure);
}

internal sealed class Wasi02ResourceLease<T> : IDisposable
    where T : class, IDisposable
{
    private T? _resource;

    internal Wasi02ResourceLease(T resource)
    {
        _resource = resource ?? throw new ArgumentNullException();
    }

    internal T Resource => _resource ?? throw new ObjectDisposedException(null);

    public void Dispose()
    {
        var resource = _resource;
        if (resource is null)
        {
            return;
        }

        _resource = null;
        resource.Dispose();
    }
}
