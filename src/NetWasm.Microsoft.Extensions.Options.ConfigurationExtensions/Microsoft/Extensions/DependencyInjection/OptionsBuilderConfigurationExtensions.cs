using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class OptionsBuilderConfigurationExtensions
{
    public static OptionsBuilder<TOptions> Bind<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config)
        where TOptions : class => optionsBuilder.Bind(config, null);

    public static OptionsBuilder<TOptions> Bind<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config, Action<BinderOptions>? configureBinder)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(config);
        optionsBuilder.Services.AddSingleton<IConfigureOptions<TOptions>>(
            new NamedConfigureFromConfigurationOptions<TOptions>(optionsBuilder.Name, config, configureBinder));
        optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(
            new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, config));
        return optionsBuilder;
    }

    public static OptionsBuilder<TOptions> BindConfiguration<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, string configSectionPath, Action<BinderOptions>? configureBinder = null)
        where TOptions : class
    {
        throw new NotSupportedException("BindConfiguration requires an IConfiguration service; use Bind(config) in the NetWasm profile.");
    }
}
