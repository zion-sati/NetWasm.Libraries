using System;
using System.IO;

namespace Microsoft.Extensions.Configuration;

public static class JsonConfigurationExtensions
{
    public static IConfigurationBuilder AddJsonStream(this IConfigurationBuilder builder, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stream);
        return builder.Add<Json.JsonStreamConfigurationSource>(source => source.Stream = stream);
    }
}
