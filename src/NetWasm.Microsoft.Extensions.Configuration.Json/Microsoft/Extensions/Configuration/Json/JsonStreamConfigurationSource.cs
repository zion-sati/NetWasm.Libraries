namespace Microsoft.Extensions.Configuration.Json;

public class JsonStreamConfigurationSource : StreamConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder) => new JsonStreamConfigurationProvider(this);
}
