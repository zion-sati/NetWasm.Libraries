// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Registers the option services used by NetWasm extensions.</summary>
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddOptions(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddSingleton(typeof(Microsoft.Extensions.Options.IOptions<>), typeof(Microsoft.Extensions.Options.OptionsManager<>));
            services.TryAddScoped(typeof(Microsoft.Extensions.Options.IOptionsSnapshot<>), typeof(Microsoft.Extensions.Options.OptionsManager<>));
            services.TryAddSingleton(typeof(Microsoft.Extensions.Options.IOptionsMonitor<>), typeof(Microsoft.Extensions.Options.OptionsMonitor<>));
            services.TryAddTransient(typeof(Microsoft.Extensions.Options.IOptionsFactory<>), typeof(Microsoft.Extensions.Options.OptionsFactory<>));
            return services;
        }

        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions)
            where TOptions : class
            => services.Configure(Microsoft.Extensions.Options.Options.DefaultName, configureOptions);

        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string? name, Action<TOptions> configureOptions)
            where TOptions : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);
            services.AddOptions();
            services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<TOptions>>(
                new Microsoft.Extensions.Options.ConfigureNamedOptions<TOptions>(name, configureOptions));
            return services;
        }

        public static IServiceCollection ConfigureAll<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions)
            where TOptions : class
            => services.Configure(name: null, configureOptions);
    }
}
