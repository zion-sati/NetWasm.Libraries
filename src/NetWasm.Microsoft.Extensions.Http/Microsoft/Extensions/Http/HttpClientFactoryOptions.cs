using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Extensions.Http;

public sealed class HttpClientFactoryOptions
{
    private TimeSpan _handlerLifetime = TimeSpan.FromMinutes(2);

    public IList<Action<HttpMessageHandlerBuilder>> HttpMessageHandlerBuilderActions { get; } =
        new List<Action<HttpMessageHandlerBuilder>>();

    public IList<Action<HttpClient>> HttpClientActions { get; } =
        new List<Action<HttpClient>>();

    internal IList<Action<IServiceProvider, HttpClient>> ServiceProviderHttpClientActions { get; } =
        new List<Action<IServiceProvider, HttpClient>>();

    public List<Action<HttpMessageHandlerBuilder>> LoggingBuilderActions { get; } =
        new List<Action<HttpMessageHandlerBuilder>>();

    public TimeSpan HandlerLifetime
    {
        get => _handlerLifetime;
        set
        {
            if (value != InfiniteHandlerLifetime && value < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Handler lifetime must be at least one second or infinite.");
            }

            _handlerLifetime = value;
        }
    }

    internal static TimeSpan InfiniteHandlerLifetime => TimeSpan.FromMilliseconds(-1);

    public Func<string, bool> ShouldRedactHeaderValue { get; set; } = static _ => false;

    public bool SuppressDefaultLogging { get; set; }

    public bool SuppressHandlerScope { get; set; }
}
