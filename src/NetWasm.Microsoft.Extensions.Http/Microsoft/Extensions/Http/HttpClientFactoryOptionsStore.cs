using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http;

internal sealed class HttpClientFactoryOptionsStore
{
    private readonly Dictionary<string, HttpClientFactoryOptions> _options =
        new(StringComparer.Ordinal);

    internal HttpClientFactoryOptions GetOrCreate(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!_options.TryGetValue(name, out HttpClientFactoryOptions? options))
        {
            options = new HttpClientFactoryOptions();
            _options.Add(name, options);
        }

        return options;
    }

    internal static HttpClientFactoryOptionsStore GetOrCreate(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ImplementationInstance is HttpClientFactoryOptionsStore store)
            {
                return store;
            }
        }

        var created = new HttpClientFactoryOptionsStore();
        services.Add(ServiceDescriptor.Singleton(created));
        return created;
    }
}
