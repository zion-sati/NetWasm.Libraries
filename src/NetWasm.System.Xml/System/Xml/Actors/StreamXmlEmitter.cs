using System.IO;
using System.Text;

namespace System.Xml;

internal sealed class StreamXmlEmitter(Stream stream, Encoding encoding) : IXmlOutputSink
{
    public void Emit(string value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);
        var bytes = encoding.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
