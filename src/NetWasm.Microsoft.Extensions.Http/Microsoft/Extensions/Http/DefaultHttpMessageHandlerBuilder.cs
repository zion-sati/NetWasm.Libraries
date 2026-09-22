using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http;

internal sealed class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
{
    private HttpMessageHandler? _primaryHandler;

    internal DefaultHttpMessageHandlerBuilder(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public override string Name { get; set; } = string.Empty;

    public override HttpMessageHandler PrimaryHandler
    {
        get => _primaryHandler ??= new HttpClientHandler();
        set => _primaryHandler = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override IList<DelegatingHandler> AdditionalHandlers { get; } =
        new List<DelegatingHandler>();

    public override IServiceProvider Services { get; }

    public override HttpMessageHandler Build() =>
        CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
}
