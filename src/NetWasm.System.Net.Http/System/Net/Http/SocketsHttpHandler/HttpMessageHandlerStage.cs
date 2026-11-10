// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpMessageHandlerStage : HttpMessageHandler
    {

        protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsyncCore(request, cancellationToken).AsTask();

        internal abstract ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}
