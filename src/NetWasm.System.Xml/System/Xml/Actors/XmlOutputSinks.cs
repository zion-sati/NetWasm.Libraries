using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

internal interface IXmlOutputSink
{
    void Emit(string value);
}

internal interface IXmlOutputFlusher
{
    void Flush();
}

internal interface IXmlOutputAsyncFlusher
{
    Task FlushAsync(CancellationToken cancellationToken);
}

internal interface IXmlOutputCloser
{
    void Close();
}
