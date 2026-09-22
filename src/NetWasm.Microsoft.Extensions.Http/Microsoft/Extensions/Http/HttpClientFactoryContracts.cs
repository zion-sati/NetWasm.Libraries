using System;

namespace System.Net.Http;

public interface IHttpClientFactory
{
    HttpClient CreateClient(string name);
}

public interface IHttpMessageHandlerFactory
{
    HttpMessageHandler CreateHandler(string name);
}

public static class HttpClientFactoryExtensions
{
    public static HttpClient CreateClient(this IHttpClientFactory factory) =>
        (factory ?? throw new ArgumentNullException(nameof(factory))).CreateClient(string.Empty);
}

public static class HttpMessageHandlerFactoryExtensions
{
    public static HttpMessageHandler CreateHandler(this IHttpMessageHandlerFactory factory) =>
        (factory ?? throw new ArgumentNullException(nameof(factory))).CreateHandler(string.Empty);
}
