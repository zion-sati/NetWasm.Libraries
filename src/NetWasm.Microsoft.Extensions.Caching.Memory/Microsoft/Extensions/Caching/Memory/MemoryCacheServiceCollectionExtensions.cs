using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class MemoryCacheServiceCollectionExtensions
{
    public static IServiceCollection AddMemoryCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions();
        services.TryAddSingleton<Microsoft.Extensions.Caching.Memory.IMemoryCache, Microsoft.Extensions.Caching.Memory.MemoryCache>();
        return services;
    }

    public static IServiceCollection AddMemoryCache(this IServiceCollection services, Action<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);
        services.AddMemoryCache();
        services.Configure(setupAction);
        return services;
    }

    public static IServiceCollection AddDistributedMemoryCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions();
        services.TryAddSingleton<IDistributedCache, Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache>();
        return services;
    }

    public static IServiceCollection AddDistributedMemoryCache(this IServiceCollection services, Action<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);
        services.AddDistributedMemoryCache();
        services.Configure(setupAction);
        return services;
    }
}
