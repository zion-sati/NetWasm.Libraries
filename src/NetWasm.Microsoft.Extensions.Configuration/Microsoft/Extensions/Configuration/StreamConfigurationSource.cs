using System.IO;

namespace Microsoft.Extensions.Configuration;

public abstract class StreamConfigurationSource : IConfigurationSource
{
    public Stream? Stream { get; set; }

    public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
}
