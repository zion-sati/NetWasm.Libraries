using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Configuration;

public static class MemoryConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddInMemoryCollection(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(new MemoryConfigurationSource());
    }

    public static IConfigurationBuilder AddInMemoryCollection(
        this IConfigurationBuilder builder,
        IEnumerable<KeyValuePair<string, string?>>? initialData)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(new MemoryConfigurationSource { InitialData = initialData });
    }
}
