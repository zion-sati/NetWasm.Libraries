using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class OptionsConfigurationServiceCollectionExtensions
{
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config)
        where TOptions : class => services.Configure<TOptions>(global::Microsoft.Extensions.Options.Options.DefaultName, config);

    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config, Action<BinderOptions>? configureBinder)
        where TOptions : class => services.Configure<TOptions>(global::Microsoft.Extensions.Options.Options.DefaultName, config, configureBinder);

    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string? name, IConfiguration config)
        where TOptions : class => services.Configure<TOptions>(name, config, null);

    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string? name, IConfiguration config, Action<BinderOptions>? configureBinder)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        services.AddOptions<TOptions>(name);
        services.AddSingleton<IConfigureOptions<TOptions>>(new NamedConfigureFromConfigurationOptions<TOptions>(name, config, configureBinder));
        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(name, config));
        return services;
    }
}
