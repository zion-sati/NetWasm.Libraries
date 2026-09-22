// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Primitives;

public sealed class StringSegmentComparer :
    IComparer<StringSegment>,
    IEqualityComparer<StringSegment>
{
    private readonly StringComparison _comparison;
    private readonly StringComparer _stringComparer;

    private StringSegmentComparer(StringComparison comparison, StringComparer stringComparer)
    {
        _comparison = comparison;
        _stringComparer = stringComparer;
    }

    public static StringSegmentComparer Ordinal { get; } =
        new(StringComparison.Ordinal, StringComparer.Ordinal);

    public static StringSegmentComparer OrdinalIgnoreCase { get; } =
        new(StringComparison.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);

    public int Compare(StringSegment x, StringSegment y) =>
        StringSegment.Compare(x, y, _comparison);

    public bool Equals(StringSegment x, StringSegment y) =>
        x.Equals(y, _comparison);

    public int GetHashCode(StringSegment obj) =>
        obj.HasValue ? _stringComparer.GetHashCode(obj.Value!) : 0;
}
