using System.IO;

namespace Microsoft.Extensions.Configuration.Json;

public class JsonStreamConfigurationProvider : StreamConfigurationProvider
{
    public JsonStreamConfigurationProvider(JsonStreamConfigurationSource source) : base(source)
    {
    }

    public override void Load(Stream stream) => Data = JsonConfigurationFileParser.Parse(stream);
}
