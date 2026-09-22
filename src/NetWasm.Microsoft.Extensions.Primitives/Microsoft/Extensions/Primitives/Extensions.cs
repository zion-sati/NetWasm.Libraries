// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.Primitives;

public static class Extensions
{
    public static StringBuilder Append(this StringBuilder builder, StringSegment segment) =>
        builder.Append(segment.Buffer, segment.Offset, segment.Length);
}
