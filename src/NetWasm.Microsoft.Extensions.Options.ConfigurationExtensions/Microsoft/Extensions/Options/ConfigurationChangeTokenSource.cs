using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options;

public sealed class ConfigurationChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions>
    where TOptions : class
{
    private readonly IConfiguration _configuration;

    public ConfigurationChangeTokenSource(IConfiguration configuration)
        : this(Options.DefaultName, configuration)
    {
    }

    public ConfigurationChangeTokenSource(string? name, IConfiguration configuration)
    {
        Name = name ?? Options.DefaultName;
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string Name { get; }

    public IChangeToken GetChangeToken() => _configuration.GetReloadToken();
}
