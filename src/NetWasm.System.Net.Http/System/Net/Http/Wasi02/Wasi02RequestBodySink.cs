// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Wasi02;

internal sealed class Wasi02RequestBodyWriter : IHttpRequestBodyWriter
{
    private readonly IWasi02ReadinessAwaiter _readiness;

    internal Wasi02RequestBodyWriter(IWasi02ReadinessAwaiter readiness)
    {
        _readiness = readiness ?? throw new ArgumentNullException();
    }

    public Task WriteAsync(
        IWasi02OutputStream output,
        byte[] contents,
        CancellationToken cancellationToken)
    {
        if (output is null)
        {
            throw new ArgumentNullException();
        }

        if (contents is null)
        {
            throw new ArgumentNullException();
        }

        if (contents.Length == 0)
        {
            return Task.CompletedTask;
        }

        return WriteCoreAsync(output, contents, cancellationToken);
    }

    private async Task WriteCoreAsync(
        IWasi02OutputStream output,
        byte[] contents,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < contents.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var permitted = output.CheckWrite();
            if (permitted == 0)
            {
                await _readiness.WaitAsync(output.Subscribe(), cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            var count = (int)Math.Min(
                permitted,
                unchecked((ulong)(contents.Length - offset)));
            var chunk = new byte[count];
            Array.Copy(contents, offset, chunk, 0, count);
            output.Write(chunk);
            offset += count;
        }

        output.Flush();
        await _readiness.WaitAsync(output.Subscribe(), cancellationToken)
            .ConfigureAwait(false);
    }
}
