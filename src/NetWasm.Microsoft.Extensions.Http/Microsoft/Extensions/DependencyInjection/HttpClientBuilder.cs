using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class HttpClientBuilder : IHttpClientBuilder
{
    internal HttpClientBuilder(IServiceCollection services, string name)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }

    public IServiceCollection Services { get; }

    internal HttpClientFactoryOptions Options =>
        HttpClientFactoryOptionsStore.GetOrCreate(Services).GetOrCreate(Name);
}

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder ConfigureHttpClient(
        this IHttpClientBuilder builder,
        Action<HttpClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureClient);
        GetOptions(builder).HttpClientActions.Add(configureClient);
        return builder;
    }

    public static IHttpClientBuilder ConfigureHttpClient(
        this IHttpClientBuilder builder,
        Action<IServiceProvider, HttpClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureClient);
        GetOptions(builder).ServiceProviderHttpClientActions.Add(configureClient);
        return builder;
    }

    public static IHttpClientBuilder ConfigureHttpMessageHandlerBuilder(
        this IHttpClientBuilder builder,
        Action<HttpMessageHandlerBuilder> configureBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureBuilder);
        GetOptions(builder).HttpMessageHandlerBuilderActions.Add(configureBuilder);
        return builder;
    }

    public static IHttpClientBuilder AddHttpMessageHandler(
        this IHttpClientBuilder builder,
        Func<DelegatingHandler> configureHandler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureHandler);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
        {
            DelegatingHandler handler = configureHandler()
                ?? throw new InvalidOperationException("The handler factory returned null.");
            config.AdditionalHandlers.Add(handler);
        });
    }

    public static IHttpClientBuilder AddHttpMessageHandler(
        this IHttpClientBuilder builder,
        Func<IServiceProvider, DelegatingHandler> configureHandler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureHandler);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
        {
            DelegatingHandler handler = configureHandler(config.Services)
                ?? throw new InvalidOperationException("The handler factory returned null.");
            config.AdditionalHandlers.Add(handler);
        });
    }

    public static IHttpClientBuilder AddHttpMessageHandler<THandler>(this IHttpClientBuilder builder)
        where THandler : DelegatingHandler
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
        {
            object value = config.Services.GetService(typeof(THandler))
                ?? ActivatorUtilities.CreateInstance(config.Services, typeof(THandler));
            config.AdditionalHandlers.Add((THandler)value);
        });
    }

    public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(
        this IHttpClientBuilder builder,
        Func<HttpMessageHandler> configureHandler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureHandler);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
            config.PrimaryHandler = configureHandler()
                ?? throw new InvalidOperationException("The primary handler factory returned null."));
    }

    public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(
        this IHttpClientBuilder builder,
        Func<IServiceProvider, HttpMessageHandler> configureHandler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureHandler);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
            config.PrimaryHandler = configureHandler(config.Services)
                ?? throw new InvalidOperationException("The primary handler factory returned null."));
    }

    public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(
        this IHttpClientBuilder builder)
        where THandler : HttpMessageHandler
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
        {
            object value = config.Services.GetService(typeof(THandler))
                ?? ActivatorUtilities.CreateInstance(config.Services, typeof(THandler));
            config.PrimaryHandler = (THandler)value;
        });
    }

    public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(
        this IHttpClientBuilder builder,
        Action<HttpMessageHandler, IServiceProvider> configureHandler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureHandler);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
            configureHandler(config.PrimaryHandler, config.Services));
    }

    public static IHttpClientBuilder ConfigureAdditionalHttpMessageHandlers(
        this IHttpClientBuilder builder,
        Action<IList<DelegatingHandler>, IServiceProvider> configureHandlers)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureHandlers);
        return builder.ConfigureHttpMessageHandlerBuilder(config =>
            configureHandlers(config.AdditionalHandlers, config.Services));
    }

    public static IHttpClientBuilder SetHandlerLifetime(
        this IHttpClientBuilder builder,
        TimeSpan handlerLifetime)
    {
        ArgumentNullException.ThrowIfNull(builder);
        GetOptions(builder).HandlerLifetime = handlerLifetime;
        return builder;
    }

    public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder)
        where TClient : class =>
        AddTypedClientCore(builder, static (client, provider) =>
            Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<TClient>(provider, client));

    public static IHttpClientBuilder AddTypedClient<TClient, TImplementation>(this IHttpClientBuilder builder)
        where TClient : class
        where TImplementation : class, TClient =>
        AddTypedClientCore<TClient>(builder, static (client, provider) =>
            Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<TImplementation>(provider, client));

    public static IHttpClientBuilder AddTypedClient<TClient>(
        this IHttpClientBuilder builder,
        Func<HttpClient, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return AddTypedClientCore(builder, (client, _) => factory(client));
    }

    public static IHttpClientBuilder AddTypedClient<TClient>(
        this IHttpClientBuilder builder,
        Func<HttpClient, IServiceProvider, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return AddTypedClientCore(builder, factory);
    }

    private static IHttpClientBuilder AddTypedClientCore<TClient>(
        IHttpClientBuilder builder,
        Func<HttpClient, IServiceProvider, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddTransient<TClient>(provider =>
        {
            var factoryService = (IHttpClientFactory?)provider.GetService(typeof(IHttpClientFactory))
                ?? throw new InvalidOperationException("IHttpClientFactory is not registered.");
            return factory(factoryService.CreateClient(builder.Name), provider);
        });
        return builder;
    }

    private static HttpClientFactoryOptions GetOptions(IHttpClientBuilder builder) =>
        builder is HttpClientBuilder netWasmBuilder
            ? netWasmBuilder.Options
            : throw new NotSupportedException("The builder was not created by NetWasm HttpClientFactory.");

}
