// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;

namespace System.Net.Http;

public sealed class EmptyContent : HttpContent
{
    protected override void SerializeToStream(Stream stream, CancellationToken cancellationToken)
    {
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return true;
    }
}
