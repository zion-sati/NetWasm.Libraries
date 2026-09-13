using System.IO;

namespace System.Xml;

internal sealed class TextWriterXmlCloser(TextWriter writer, bool closeOutput) : IXmlOutputCloser
{
    public void Close()
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (closeOutput)
        {
            writer.Dispose();
        }
    }
}
