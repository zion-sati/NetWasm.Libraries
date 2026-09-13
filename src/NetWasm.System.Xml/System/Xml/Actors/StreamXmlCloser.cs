using System.IO;

namespace System.Xml;

internal sealed class StreamXmlCloser(Stream stream, bool closeOutput) : IXmlOutputCloser
{
    public void Close()
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (closeOutput)
        {
            stream.Dispose();
        }
    }
}
