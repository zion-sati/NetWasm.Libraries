// Original NetWasm capability actors. Each adapter exposes one action only.
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

internal interface IXmlTextSource
{
    string ReadAll();
}

internal interface IXmlAsyncTextSource
{
    Task<string> ReadAllAsync(CancellationToken cancellationToken);
}

internal sealed class TextReaderXmlTextSource(TextReader reader) : IXmlTextSource
{
    public string ReadAll()
    {
        ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadToEnd();
    }
}

internal sealed class StreamXmlTextSource(Stream stream, Encoding encoding) : IXmlTextSource
{
    public string ReadAll()
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);
        var bytes = new List<byte>();
        var buffer = new byte[4096];
        int count;
        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var index = 0; index < count; index++)
            {
                bytes.Add(buffer[index]);
            }
        }

        var data = bytes.ToArray();
        var selected = XmlEncodingSelector.Select(data, encoding, out var offset);
        return selected.GetString(data, offset, data.Length - offset);
    }
}

internal sealed class TextReaderXmlAsyncTextSource(TextReader reader) : IXmlAsyncTextSource
{
    public Task<string> ReadAllAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<string>(cancellationToken)
            : Task.FromResult(reader.ReadToEnd());
    }
}

internal sealed class StreamXmlAsyncTextSource(Stream stream, Encoding encoding) : IXmlAsyncTextSource
{
    public async Task<string> ReadAllAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);
        var bytes = new List<byte>();
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            for (var index = 0; index < count; index++)
            {
                bytes.Add(buffer[index]);
            }
        }

        var data = bytes.ToArray();
        var selected = XmlEncodingSelector.Select(data, encoding, out var offset);
        return selected.GetString(data, offset, data.Length - offset);
    }
}

internal static class XmlEncodingSelector
{
    internal static Encoding Select(byte[] data, Encoding fallback, out int offset)
    {
        if (data.Length >= 4 && data[0] == 0 && data[1] == 0 && data[2] == 0xFE && data[3] == 0xFF)
        {
            offset = 4;
            return Encoding.GetEncoding(12001);
        }

        if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0 && data[3] == 0)
        {
            offset = 4;
            return Encoding.UTF32;
        }

        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            offset = 3;
            return Encoding.UTF8;
        }

        if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
        {
            offset = 2;
            return Encoding.BigEndianUnicode;
        }

        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
        {
            offset = 2;
            return Encoding.Unicode;
        }

        offset = 0;
        return fallback;
    }
}
