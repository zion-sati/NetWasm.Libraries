// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NetWasm: dotnet/runtime 474ed963's (.NET 11 RC3) WasiHttpHandler surface (capability constants, Properties, the controls that
// throw PlatformNotSupportedException and the unsupported synchronous Send), sending through NetWasm's IHttpTransport
// rather than upstream's own WASI bindings. The default transport sends through wasi:http 0.3; HttpClientHandler's
// transport constructor supplies another.

using System.Collections.Generic;
using System.Net.Http.WasiHttp;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class WasiHttpHandler : HttpMessageHandler
    {
        private readonly IHttpTransport _transport;

        public WasiHttpHandler()
            : this(WasiHttpTransportComposition.Create())
        {
        }

        public WasiHttpHandler(IHttpTransport transport)
        {
            ArgumentNullException.ThrowIfNull(transport);
            _transport = transport;
        }

        public const bool SupportsAutomaticDecompression = false;
        public const bool SupportsProxy = false;
        public const bool SupportsRedirectConfiguration = false;

        public IDictionary<string, object?> Properties => field ??= new Dictionary<string, object?>();

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // As upstream's WASI request wrapper does, the response names the request it answers.
            if (response is not null)
            {
                response.RequestMessage = request;
            }

            return response!;
        }

        #region PlatformNotSupported
#pragma warning disable CA1822

        internal ClientCertificateOption ClientCertificateOptions;


        public bool UseCookies
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public CookieContainer CookieContainer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ICredentials? Credentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int MaxConnectionsPerServer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public SslClientAuthenticationOptions SslOptions
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool AllowAutoRedirect
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
#pragma warning restore CA1822

        #endregion
    }
}
