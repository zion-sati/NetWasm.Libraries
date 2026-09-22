using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpClientFactoryServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        EnsureFactory(services);
        return services;
    }

    public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        EnsureFactory(services);
        return new HttpClientBuilder(services, name);
    }

    public static IHttpClientBuilder AddHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(configureClient);
        return services.AddHttpClient(name).ConfigureHttpClient(configureClient);
    }

    public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services)
        where TClient : class =>
        services.AddHttpClient(TypedClientName<TClient>.Value).AddTypedClient<TClient>();

    public static IHttpClientBuilder AddHttpClient<TClient>(
        this IServiceCollection services,
        string name)
        where TClient : class =>
        services.AddHttpClient(name).AddTypedClient<TClient>();

    public static IHttpClientBuilder AddHttpClient<TClient>(
        this IServiceCollection services,
        Action<HttpClient> configureClient)
        where TClient : class =>
        services.AddHttpClient<TClient>().ConfigureHttpClient(configureClient);

    public static IHttpClientBuilder AddHttpClient<TClient>(
        this IServiceCollection services,
        string name,
        Action<HttpClient> configureClient)
        where TClient : class =>
        services.AddHttpClient<TClient>(name).ConfigureHttpClient(configureClient);

    public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient =>
        services.AddHttpClient(TypedClientName<TClient>.Value).AddTypedClient<TClient, TImplementation>();

    public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string name)
        where TClient : class
        where TImplementation : class, TClient =>
        services.AddHttpClient(name).AddTypedClient<TClient, TImplementation>();

    public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        Action<HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient =>
        services.AddHttpClient<TClient, TImplementation>().ConfigureHttpClient(configureClient);

    public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string name,
        Action<HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient =>
        services.AddHttpClient<TClient, TImplementation>(name).ConfigureHttpClient(configureClient);

    private static void EnsureFactory(IServiceCollection services)
    {
        HttpClientFactoryOptionsStore options = HttpClientFactoryOptionsStore.GetOrCreate(services);
        services.TryAddSingleton<IHttpClientFactory>(provider =>
            new DefaultHttpClientFactory(provider, options));
        services.TryAddSingleton<IHttpMessageHandlerFactory>(provider =>
            (IHttpMessageHandlerFactory)provider.GetService(typeof(IHttpClientFactory))!);
    }

    private static class TypedClientName<TClient>
    {
        internal static readonly string Value = TypedClientNameSequence.Create();
    }

    private static class TypedClientNameSequence
    {
        private static readonly object Gate = new();
        private static int _next;

        internal static string Create()
        {
            lock (Gate) return "__NetWasmTypedClient" + (++_next).ToString();
        }
    }
}
