using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http;

public abstract class HttpMessageHandlerBuilder
{
    public abstract string Name { get; set; }

    public abstract HttpMessageHandler PrimaryHandler { get; set; }

    public abstract IList<DelegatingHandler> AdditionalHandlers { get; }

    public abstract IServiceProvider Services { get; }

    public abstract HttpMessageHandler Build();

    public static HttpMessageHandler CreateHandlerPipeline(
        HttpMessageHandler primaryHandler,
        IEnumerable<DelegatingHandler> additionalHandlers)
    {
        ArgumentNullException.ThrowIfNull(primaryHandler);
        ArgumentNullException.ThrowIfNull(additionalHandlers);

        HttpMessageHandler next = primaryHandler;
        var handlers = new List<DelegatingHandler>();
        foreach (DelegatingHandler? handler in additionalHandlers)
        {
            if (handler is null)
            {
                throw new InvalidOperationException("Additional handlers cannot contain null entries.");
            }

            handlers.Add(handler);
        }

        for (var index = handlers.Count - 1; index >= 0; index--)
        {
            DelegatingHandler handler = handlers[index];
            if (handler.InnerHandler is not null)
            {
                throw new InvalidOperationException("Delegating handlers must not be reused.");
            }

            handler.InnerHandler = next;
            next = handler;
        }

        return next;
    }
}

public interface IHttpMessageHandlerBuilderFilter
{
    Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next);
}

public interface ITypedHttpClientFactory<TClient>
    where TClient : class
{
    TClient CreateClient(HttpClient httpClient);
}
