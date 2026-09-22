using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Options;

public class ConfigureFromConfigurationOptions<TOptions> : ConfigureOptions<TOptions>
    where TOptions : class
{
    public ConfigureFromConfigurationOptions(IConfiguration config)
        : base(options => config.Bind(options))
    {
    }
}

public class NamedConfigureFromConfigurationOptions<TOptions> : ConfigureNamedOptions<TOptions>
    where TOptions : class
{
    public NamedConfigureFromConfigurationOptions(string? name, IConfiguration config)
        : base(name, options => config.Bind(options))
    {
    }

    public NamedConfigureFromConfigurationOptions(string? name, IConfiguration config, Action<BinderOptions>? configureBinder)
        : base(name, options => config.Bind(options, configureBinder))
    {
    }
}
