// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Wasi02;

internal sealed class Wasi02HttpTransport : IHttpTransport
{
    private readonly IWasi02OutgoingHandler _handler;
    private readonly IWasi02ResponseBodyStreamFactory _responseBodyFactory;
    private readonly IHttpFailureMapper _failureMapper;

    internal Wasi02HttpTransport(
        IWasi02OutgoingHandler handler,
        IWasi02ResponseBodyStreamFactory responseBodyFactory,
        IHttpFailureMapper failureMapper)
    {
        _handler = handler ?? throw new ArgumentNullException();
        _responseBodyFactory = responseBodyFactory ?? throw new ArgumentNullException();
        _failureMapper = failureMapper ?? throw new ArgumentNullException();
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException();
        }

        Wasi02IncomingResponse? incoming = null;
        var bodyTransferred = false;
        try
        {
            var body = request.Content is null
                ? null
                : await request.Content
                    .ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

            incoming = await _handler
                .SendAsync(request, body, cancellationToken)
                .ConfigureAwait(false);
            if (incoming is null)
            {
                throw new InvalidOperationException();
            }

            var response = new HttpResponseMessage((HttpStatusCode)incoming.StatusCode);
            foreach (var header in incoming.Headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }

            if (incoming.Body is null)
            {
                response.Content = new EmptyContent();
            }
            else
            {
                var stream = _responseBodyFactory.Create(incoming.Body);
                response.Content = new StreamContent(stream);
                bodyTransferred = true;
            }

            return response;
        }
        catch (Wasi02TransportFailureException failure)
        {
            throw _failureMapper.Translate(failure.Facts);
        }
        finally
        {
            if (!bodyTransferred && incoming?.Body is not null)
            {
                incoming.Body.Dispose();
            }
        }
    }
}
