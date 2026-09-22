// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WebAssembly;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bindings = NetWasm.Wit.Netwasm.Http.Client._1._0._0;

namespace System.Net.Http.Wasi02;

internal static class Wasi02HttpTransportComposition
{
    internal static IHttpTransport Create()
    {
        var readiness = new Wasi02ReadinessAwaiter(new Wasi02CoreReactor(
            PlatformServices.PollableScheduler));
        var failureMapper = new Wasi02FailureMapper();
        return new Wasi02HttpTransport(
            new Wasi02BindingOutgoingHandler(
                new Wasi02RequestBodyWriter(readiness),
                readiness),
            new Wasi02ResponseBodyStreamFactory(readiness, failureMapper),
            failureMapper);
    }
}

internal sealed class Wasi02CoreReactor(IPlatformPollableScheduler scheduler) :
    IWasi02Reactor
{
    private readonly IPlatformPollableScheduler _scheduler = scheduler ??
        throw new ArgumentNullException();

    public IWasi02ReactorRegistration Register(IWasi02Pollable pollable)
    {
        if (pollable is null)
        {
            throw new ArgumentNullException();
        }

        var completion = new TaskCompletionSource();
        var schedule = _scheduler.SchedulePollable(
            () => completion.TrySetResult(),
            pollable.TakeHandle());
        return new Wasi02CoreReactorRegistration(schedule, completion.Task);
    }
}

internal sealed class Wasi02CoreReactorRegistration(
    IPlatformSchedule schedule,
    Task completion) : IWasi02ReactorRegistration
{
    private IPlatformSchedule? _schedule = schedule ??
        throw new ArgumentNullException();

    public Task Completion { get; } = completion ??
        throw new ArgumentNullException();

    public void Dispose()
    {
        var schedule = _schedule;
        if (schedule is null)
        {
            return;
        }

        _schedule = null;
        schedule.Dispose();
    }
}

internal sealed class Wasi02BindingPollable(Bindings.Pollable pollable) :
    IWasi02Pollable
{
    private Bindings.Pollable? _pollable = pollable ??
        throw new ArgumentNullException();

    public int TakeHandle()
    {
        var pollable = _pollable ?? throw new ObjectDisposedException(null);
        _pollable = null;
        return unchecked((int)pollable.LowerImport(transferOwnership: true));
    }

    public void Dispose()
    {
        var pollable = _pollable;
        _pollable = null;
        pollable?.Dispose();
    }
}

internal sealed class Wasi02BindingOutputStream(Bindings.OutputStream output) :
    IWasi02OutputStream
{
    private Bindings.OutputStream? _output = output ??
        throw new ArgumentNullException();

    public ulong CheckWrite()
    {
        var result = Bindings.StreamsImports.CheckWrite(Resource);
        if (!result.IsOk)
        {
            throw Wasi02BindingErrorMapper.Translate(result.Error);
        }

        return result.Ok;
    }

    public void Write(byte[] contents)
    {
        var result = Bindings.StreamsImports.Write(
            Resource,
            contents ?? throw new ArgumentNullException());
        if (!result.IsOk)
        {
            throw Wasi02BindingErrorMapper.Translate(result.Error);
        }
    }

    public void Flush()
    {
        var result = Bindings.StreamsImports.Flush(Resource);
        if (!result.IsOk)
        {
            throw Wasi02BindingErrorMapper.Translate(result.Error);
        }
    }

    public IWasi02Pollable Subscribe() =>
        new Wasi02BindingPollable(Bindings.StreamsImports.Subscribe(Resource));

    public void Dispose()
    {
        var output = _output;
        _output = null;
        output?.Dispose();
    }

    private Bindings.OutputStream Resource =>
        _output ?? throw new ObjectDisposedException(null);
}

internal sealed class Wasi02BindingInputStream(
    Bindings.InputStream input,
    Bindings.IncomingBody body) : IWasi02InputStream
{
    private Bindings.InputStream? _input = input ??
        throw new ArgumentNullException();
    private Bindings.IncomingBody? _body = body ??
        throw new ArgumentNullException();

    public IWasi02Pollable Subscribe() =>
        new Wasi02BindingPollable(Bindings.StreamsImports.Subscribe(Resource));

    public ValueTask<Wasi02ReadResult> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Bindings.StreamsImports.Read(
            Resource,
            unchecked((ulong)buffer.Length));
        if (!result.IsOk)
        {
            return new ValueTask<Wasi02ReadResult>(
                result.Error.Tag == Bindings.StreamErrorTag.Closed
                    ? Wasi02ReadResult.EndOfStream()
                    : Wasi02ReadResult.Failed(
                        Wasi02BindingErrorMapper.Map(result.Error)));
        }

        var bytes = result.Ok;
        if (bytes.Length == 0)
        {
            return new ValueTask<Wasi02ReadResult>(Wasi02ReadResult.WouldBlock());
        }

        if (bytes.Length > buffer.Length)
        {
            return new ValueTask<Wasi02ReadResult>(Wasi02ReadResult.Failed(
                new Wasi02FailureFacts(
                    Wasi02FailureKind.HttpResponseBodySize,
                    "WASI HTTP returned more bytes than requested.")));
        }

        bytes.AsSpan().CopyTo(buffer.Span);
        return new ValueTask<Wasi02ReadResult>(Wasi02ReadResult.Data(bytes.Length));
    }

    public void Dispose()
    {
        var input = _input;
        _input = null;
        input?.Dispose();

        var body = _body;
        _body = null;
        if (body is null)
        {
            return;
        }

        try
        {
            using var trailers = Bindings.TypesImports.Finish(body);
        }
        finally
        {
            body.Dispose();
        }
    }

    private Bindings.InputStream Resource =>
        _input ?? throw new ObjectDisposedException(null);
}

internal sealed class Wasi02BindingOutgoingHandler(
    IHttpRequestBodyWriter bodyWriter,
    IWasi02ReadinessAwaiter readiness) : IWasi02OutgoingHandler
{
    private readonly IHttpRequestBodyWriter _bodyWriter = bodyWriter ??
        throw new ArgumentNullException();
    private readonly IWasi02ReadinessAwaiter _readiness = readiness ??
        throw new ArgumentNullException();

    public async Task<Wasi02IncomingResponse> SendAsync(
        HttpRequestMessage request,
        byte[]? body,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException();
        }

        Bindings.OutgoingRequest? outgoing = null;
        Bindings.OutgoingBody? outgoingBody = null;
        Wasi02BindingOutputStream? output = null;
        Bindings.FutureIncomingResponse? future = null;
        try
        {
            outgoing = CreateRequest(request);
            var outgoingBodyResult = Bindings.TypesImports.Body(outgoing);
            if (!outgoingBodyResult.IsOk)
            {
                throw InvalidResponse("WASI HTTP request body was already acquired.");
            }

            outgoingBody = outgoingBodyResult.Ok;
            if (body is not null && body.Length != 0)
            {
                var outputResult = Bindings.TypesImports.Write(outgoingBody);
                if (!outputResult.IsOk)
                {
                    throw InvalidResponse("WASI HTTP request body stream was unavailable.");
                }

                output = new Wasi02BindingOutputStream(outputResult.Ok);
            }

            var handle = Bindings.OutgoingHandlerImports.Handle(
                outgoing,
                WitOption<Bindings.RequestOptions>.None);
            if (!handle.IsOk)
            {
                throw new Wasi02TransportFailureException(
                    Wasi02BindingErrorMapper.Map(handle.Error));
            }

            future = handle.Ok;
            if (output is not null)
            {
                await _bodyWriter.WriteAsync(output, body!, cancellationToken)
                    .ConfigureAwait(false);
                output.Dispose();
                output = null;
            }

            var finish = Bindings.TypesImports.Finish(
                outgoingBody,
                WitOption<Bindings.Fields>.None);
            if (!finish.IsOk)
            {
                throw new Wasi02TransportFailureException(
                    Wasi02BindingErrorMapper.Map(finish.Error));
            }

            outgoingBody = null;
            return await AwaitResponseAsync(future, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            output?.Dispose();
            outgoingBody?.Dispose();
            outgoing?.Dispose();
            future?.Dispose();
        }
    }

    private async Task<Wasi02IncomingResponse> AwaitResponseAsync(
        Bindings.FutureIncomingResponse future,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var available = Bindings.TypesImports.Get(future);
            if (!available.HasValue)
            {
                await _readiness.WaitAsync(
                    new Wasi02BindingPollable(
                        Bindings.TypesImports.Subscribe(future)),
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            var singleRead = available.Value;
            if (!singleRead.IsOk)
            {
                throw InvalidResponse(
                    "WASI HTTP response future was consumed more than once.");
            }

            var response = singleRead.Ok;
            if (!response.IsOk)
            {
                throw new Wasi02TransportFailureException(
                    Wasi02BindingErrorMapper.Map(response.Error));
            }

            return MaterializeResponse(response.Ok);
        }
    }

    private static Bindings.OutgoingRequest CreateRequest(HttpRequestMessage request)
    {
        Uri requestUri = request.RequestUri
            ?? throw new InvalidOperationException("A request URI is required.");
        if (!requestUri.IsAbsoluteUri)
        {
            throw new InvalidOperationException("The request URI must be absolute.");
        }

        using var fields = Bindings.TypesImports.CreateFields();
        AppendHeaders(fields, request.Headers.Entries());
        if (request.Content is not null)
        {
            AppendHeaders(fields, request.Content.Headers.Entries());
        }

        var outgoing = Bindings.TypesImports.CreateOutgoingRequest(fields);
        try
        {
            RequireSet(
                Bindings.TypesImports.SetMethod(
                    outgoing,
                    MapMethod(request.Method)),
                Wasi02FailureKind.HttpRequestMethodInvalid);
            RequireSet(
                Bindings.TypesImports.SetScheme(
                    outgoing,
                    new WitOption<Bindings.Scheme>(MapScheme(requestUri.Scheme))),
                Wasi02FailureKind.HttpRequestUriInvalid);
            RequireSet(
                Bindings.TypesImports.SetAuthority(
                    outgoing,
                    new WitOption<string>(requestUri.Authority)),
                Wasi02FailureKind.HttpRequestUriInvalid);
            RequireSet(
                Bindings.TypesImports.SetPathWithQuery(
                    outgoing,
                    new WitOption<string>(requestUri.PathAndQuery)),
                Wasi02FailureKind.HttpRequestUriInvalid);
            return outgoing;
        }
        catch
        {
            outgoing.Dispose();
            throw;
        }
    }

    private static Wasi02IncomingResponse MaterializeResponse(
        Bindings.IncomingResponse response)
    {
        using (response)
        {
            var headers = new List<KeyValuePair<string, string>>();
            using (var fields = Bindings.TypesImports.Headers(response))
            {
                foreach (var entry in Bindings.TypesImports.Entries(fields))
                {
                    headers.Add(new KeyValuePair<string, string>(
                        entry.Item1,
                        Encoding.UTF8.GetString(entry.Item2)));
                }
            }

            var bodyResult = Bindings.TypesImports.Consume(response);
            if (!bodyResult.IsOk)
            {
                throw InvalidResponse("WASI HTTP response body was already consumed.");
            }

            var incomingBody = bodyResult.Ok;
            try
            {
                var streamResult = Bindings.TypesImports.Stream(incomingBody);
                if (!streamResult.IsOk)
                {
                    throw InvalidResponse("WASI HTTP response body stream was unavailable.");
                }

                return new Wasi02IncomingResponse(
                    Bindings.TypesImports.Status(response),
                    headers,
                    new Wasi02BindingInputStream(streamResult.Ok, incomingBody));
            }
            catch
            {
                incomingBody.Dispose();
                throw;
            }
        }
    }

    private static void AppendHeaders(
        Bindings.Fields fields,
        IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
        {
            var result = Bindings.TypesImports.Append(
                fields,
                header.Key,
                Encoding.UTF8.GetBytes(header.Value));
            if (!result.IsOk)
            {
                throw new Wasi02TransportFailureException(new Wasi02FailureFacts(
                    Wasi02FailureKind.HttpRequestHeaderSize,
                    $"WASI HTTP rejected request header '{header.Key}'."));
            }
        }
    }

    private static void RequireSet(
        WitResult<WitUnit, WitUnit> result,
        Wasi02FailureKind failure)
    {
        if (!result.IsOk)
        {
            throw new Wasi02TransportFailureException(new Wasi02FailureFacts(failure));
        }
    }

    private static Bindings.Method MapMethod(HttpMethod method) =>
        method.Method.ToUpperInvariant() switch
        {
            "GET" => Bindings.Method.Get(),
            "HEAD" => Bindings.Method.Head(),
            "POST" => Bindings.Method.Post(),
            "PUT" => Bindings.Method.Put(),
            "DELETE" => Bindings.Method.Delete(),
            "CONNECT" => Bindings.Method.Connect(),
            "OPTIONS" => Bindings.Method.Options(),
            "TRACE" => Bindings.Method.Trace(),
            "PATCH" => Bindings.Method.Patch(),
            _ => Bindings.Method.Other(method.Method),
        };

    private static Bindings.Scheme MapScheme(string scheme) =>
        scheme.ToLowerInvariant() switch
        {
            "http" => Bindings.Scheme.HTTP(),
            "https" => Bindings.Scheme.HTTPS(),
            _ => Bindings.Scheme.Other(scheme),
        };

    private static Wasi02TransportFailureException InvalidResponse(string message) =>
        new(new Wasi02FailureFacts(Wasi02FailureKind.HttpProtocolError, message));
}

internal static class Wasi02BindingErrorMapper
{
    internal static Wasi02TransportFailureException Translate(
        Bindings.StreamError error) =>
        new(Map(error));

    internal static Wasi02FailureFacts Map(Bindings.StreamError error)
    {
        if (error.Tag == Bindings.StreamErrorTag.Closed)
        {
            return new Wasi02FailureFacts(Wasi02FailureKind.ConnectionTerminated);
        }

        using var detail = error.LastOperationFailedValue;
        var code = Bindings.TypesImports.HttpErrorCode(detail);
        return code.HasValue
            ? Map(code.Value)
            : new Wasi02FailureFacts(
                Wasi02FailureKind.InternalError,
                Bindings.ErrorImports.ToDebugString(detail));
    }

    internal static Wasi02FailureFacts Map(Bindings.ErrorCode error) =>
        new(error.Tag switch
        {
            Bindings.ErrorCodeTag.DNSTimeout => Wasi02FailureKind.DnsTimeout,
            Bindings.ErrorCodeTag.DNSError => Wasi02FailureKind.DnsError,
            Bindings.ErrorCodeTag.DestinationNotFound => Wasi02FailureKind.DestinationNotFound,
            Bindings.ErrorCodeTag.DestinationUnavailable => Wasi02FailureKind.DestinationUnavailable,
            Bindings.ErrorCodeTag.DestinationIPProhibited => Wasi02FailureKind.DestinationIpProhibited,
            Bindings.ErrorCodeTag.DestinationIPUnroutable => Wasi02FailureKind.DestinationIpUnroutable,
            Bindings.ErrorCodeTag.ConnectionRefused => Wasi02FailureKind.ConnectionRefused,
            Bindings.ErrorCodeTag.ConnectionTerminated => Wasi02FailureKind.ConnectionTerminated,
            Bindings.ErrorCodeTag.ConnectionTimeout => Wasi02FailureKind.ConnectionTimeout,
            Bindings.ErrorCodeTag.ConnectionReadTimeout => Wasi02FailureKind.ConnectionReadTimeout,
            Bindings.ErrorCodeTag.ConnectionWriteTimeout => Wasi02FailureKind.ConnectionWriteTimeout,
            Bindings.ErrorCodeTag.ConnectionLimitReached => Wasi02FailureKind.ConnectionLimitReached,
            Bindings.ErrorCodeTag.TLSProtocolError => Wasi02FailureKind.TlsProtocolError,
            Bindings.ErrorCodeTag.TLSCertificateError => Wasi02FailureKind.TlsCertificateError,
            Bindings.ErrorCodeTag.TLSAlertReceived => Wasi02FailureKind.TlsAlertReceived,
            Bindings.ErrorCodeTag.HTTPRequestDenied => Wasi02FailureKind.HttpRequestDenied,
            Bindings.ErrorCodeTag.HTTPRequestLengthRequired => Wasi02FailureKind.HttpRequestLengthRequired,
            Bindings.ErrorCodeTag.HTTPRequestBodySize => Wasi02FailureKind.HttpRequestBodySize,
            Bindings.ErrorCodeTag.HTTPRequestMethodInvalid => Wasi02FailureKind.HttpRequestMethodInvalid,
            Bindings.ErrorCodeTag.HTTPRequestURIInvalid => Wasi02FailureKind.HttpRequestUriInvalid,
            Bindings.ErrorCodeTag.HTTPRequestURITooLong => Wasi02FailureKind.HttpRequestUriTooLong,
            Bindings.ErrorCodeTag.HTTPRequestHeaderSectionSize => Wasi02FailureKind.HttpRequestHeaderSectionSize,
            Bindings.ErrorCodeTag.HTTPRequestHeaderSize => Wasi02FailureKind.HttpRequestHeaderSize,
            Bindings.ErrorCodeTag.HTTPRequestTrailerSectionSize => Wasi02FailureKind.HttpRequestTrailerSectionSize,
            Bindings.ErrorCodeTag.HTTPRequestTrailerSize => Wasi02FailureKind.HttpRequestTrailerSize,
            Bindings.ErrorCodeTag.HTTPResponseIncomplete => Wasi02FailureKind.HttpResponseIncomplete,
            Bindings.ErrorCodeTag.HTTPResponseHeaderSectionSize => Wasi02FailureKind.HttpResponseHeaderSectionSize,
            Bindings.ErrorCodeTag.HTTPResponseHeaderSize => Wasi02FailureKind.HttpResponseHeaderSize,
            Bindings.ErrorCodeTag.HTTPResponseBodySize => Wasi02FailureKind.HttpResponseBodySize,
            Bindings.ErrorCodeTag.HTTPResponseTrailerSectionSize => Wasi02FailureKind.HttpResponseTrailerSectionSize,
            Bindings.ErrorCodeTag.HTTPResponseTrailerSize => Wasi02FailureKind.HttpResponseTrailerSize,
            Bindings.ErrorCodeTag.HTTPResponseTransferCoding => Wasi02FailureKind.HttpResponseTransferCoding,
            Bindings.ErrorCodeTag.HTTPResponseContentCoding => Wasi02FailureKind.HttpResponseContentCoding,
            Bindings.ErrorCodeTag.HTTPResponseTimeout => Wasi02FailureKind.HttpResponseTimeout,
            Bindings.ErrorCodeTag.HTTPUpgradeFailed => Wasi02FailureKind.HttpUpgradeFailed,
            Bindings.ErrorCodeTag.HTTPProtocolError => Wasi02FailureKind.HttpProtocolError,
            Bindings.ErrorCodeTag.LoopDetected => Wasi02FailureKind.LoopDetected,
            Bindings.ErrorCodeTag.ConfigurationError => Wasi02FailureKind.ConfigurationError,
            Bindings.ErrorCodeTag.InternalError => Wasi02FailureKind.InternalError,
            _ => Wasi02FailureKind.InternalError,
        },
        error.Tag == Bindings.ErrorCodeTag.InternalError &&
        error.InternalErrorValue.HasValue
            ? error.InternalErrorValue.Value
            : null);
}
