using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Memory;

public class MemoryConfigurationSource : IConfigurationSource
{
    public IEnumerable<KeyValuePair<string, string?>>? InitialData { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new MemoryConfigurationProvider(this);
}
