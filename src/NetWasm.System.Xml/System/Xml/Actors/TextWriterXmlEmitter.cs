using System.IO;

namespace System.Xml;

internal sealed class TextWriterXmlEmitter(TextWriter writer) : IXmlOutputSink
{
    public void Emit(string value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(value);
    }
}
