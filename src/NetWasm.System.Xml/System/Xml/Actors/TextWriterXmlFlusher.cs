using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

internal sealed class TextWriterXmlFlusher(TextWriter writer) : IXmlOutputFlusher
{
    public void Flush()
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Flush();
    }
}

internal sealed class TextWriterXmlAsyncFlusher(TextWriter writer) : IXmlOutputAsyncFlusher
{
    public Task FlushAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : FlushImmediately();

        Task FlushImmediately()
        {
            writer.Flush();
            return Task.CompletedTask;
        }
    }
}
