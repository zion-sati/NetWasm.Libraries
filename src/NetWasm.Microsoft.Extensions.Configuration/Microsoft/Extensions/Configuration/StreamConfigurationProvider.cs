using System;
using System.IO;

namespace Microsoft.Extensions.Configuration;

public abstract class StreamConfigurationProvider : ConfigurationProvider
{
    private bool _loaded;

    protected StreamConfigurationProvider(StreamConfigurationSource source) => Source = source ?? throw new ArgumentNullException(nameof(source));

    public StreamConfigurationSource Source { get; }

    public abstract void Load(Stream stream);

    public override void Load()
    {
        if (_loaded) throw new InvalidOperationException("Stream configuration providers can only be loaded once.");
        if (Source.Stream is null) throw new InvalidOperationException("The stream configuration source cannot be null.");
        Load(Source.Stream);
        _loaded = true;
    }
}
