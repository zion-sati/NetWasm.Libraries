using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

internal sealed class StreamXmlFlusher(Stream stream) : IXmlOutputFlusher
{
    public void Flush()
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Flush();
    }
}

internal sealed class StreamXmlAsyncFlusher(Stream stream) : IXmlOutputAsyncFlusher
{
    public Task FlushAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return stream.FlushAsync(cancellationToken);
    }
}
