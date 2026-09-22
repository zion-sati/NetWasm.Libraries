using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Wasi02;

namespace System.Net.Http;

/// <summary>
/// The NetWasm primary handler. Connection pooling and wire transport remain
/// owned by the WASI HTTP host.
/// </summary>
public class HttpClientHandler : HttpMessageHandler
{
    private readonly IHttpTransport _transport;

    public HttpClientHandler()
        : this(Wasi02HttpTransportComposition.Create())
    {
    }

    public HttpClientHandler(IHttpTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        _transport.SendAsync(request, cancellationToken);
}
