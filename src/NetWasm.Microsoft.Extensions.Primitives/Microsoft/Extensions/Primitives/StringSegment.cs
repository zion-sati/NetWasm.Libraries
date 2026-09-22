// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Primitives;

public readonly struct StringSegment : IEquatable<StringSegment>, IEquatable<string?>
{
    public static readonly StringSegment Empty = string.Empty;

    public StringSegment(string? buffer)
    {
        Buffer = buffer;
        Offset = 0;
        Length = buffer?.Length ?? 0;
    }

    public StringSegment(string buffer, int offset, int length)
    {
        if (buffer is null || offset < 0 || length < 0 || offset > buffer.Length - length)
        {
            throw new ArgumentOutOfRangeException();
        }

        Buffer = buffer;
        Offset = offset;
        Length = length;
    }

    public string? Buffer { get; }

    public int Offset { get; }

    public int Length { get; }

    public string? Value => HasValue ? Buffer!.Substring(Offset, Length) : null;

    public bool HasValue => Buffer is not null;

    public char this[int index]
    {
        get
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            return Buffer![Offset + index];
        }
    }

    public ReadOnlySpan<char> AsSpan() =>
        HasValue ? Buffer!.AsSpan(Offset, Length) : ReadOnlySpan<char>.Empty;

    public ReadOnlySpan<char> AsSpan(int start)
    {
        ValidateSubsegment(start, Length - start);
        return Buffer!.AsSpan(Offset + start, Length - start);
    }

    public ReadOnlySpan<char> AsSpan(int start, int length)
    {
        ValidateSubsegment(start, length);
        return Buffer!.AsSpan(Offset + start, length);
    }

    public ReadOnlyMemory<char> AsMemory() =>
        HasValue ? Buffer!.AsMemory(Offset, Length) : ReadOnlyMemory<char>.Empty;

    public int IndexOf(char c) => IndexOf(c, 0, Length);

    public int IndexOf(char c, int start) => IndexOf(c, start, Length - start);

    public int IndexOf(char c, int start, int count)
    {
        ValidateSubsegment(start, count);
        if (!HasValue)
        {
            return -1;
        }

        var index = Buffer!.IndexOf(c, Offset + start, count);
        return index < 0 ? -1 : index - Offset;
    }

    public int IndexOfAny(char[] anyOf) => IndexOfAny(anyOf, 0, Length);

    public int IndexOfAny(char[] anyOf, int startIndex) =>
        IndexOfAny(anyOf, startIndex, Length - startIndex);

    public int IndexOfAny(char[] anyOf, int startIndex, int count)
    {
        ArgumentNullException.ThrowIfNull(anyOf);
        ValidateSubsegment(startIndex, count);
        if (!HasValue)
        {
            return -1;
        }

        for (var index = startIndex; index < startIndex + count; index++)
        {
            for (var separator = 0; separator < anyOf.Length; separator++)
            {
                if (this[index] == anyOf[separator])
                {
                    return index;
                }
            }
        }

        return -1;
    }

    public int LastIndexOf(char value)
    {
        for (var index = Length - 1; index >= 0; index--)
        {
            if (this[index] == value)
            {
                return index;
            }
        }

        return -1;
    }

    public bool StartsWith(string text, StringComparison comparisonType)
    {
        ArgumentNullException.ThrowIfNull(text);
        return HasValue && AsSpan().StartsWith(text.AsSpan(), comparisonType);
    }

    public bool EndsWith(string text, StringComparison comparisonType)
    {
        ArgumentNullException.ThrowIfNull(text);
        return HasValue && AsSpan().EndsWith(text.AsSpan(), comparisonType);
    }

    public StringSegment Trim() => TrimStart().TrimEnd();

    public StringSegment TrimStart()
    {
        var start = 0;
        while (start < Length && char.IsWhiteSpace(this[start]))
        {
            start++;
        }

        return Subsegment(start, Length - start);
    }

    public StringSegment TrimEnd()
    {
        var end = Length;
        while (end > 0 && char.IsWhiteSpace(this[end - 1]))
        {
            end--;
        }

        return Subsegment(0, end);
    }

    public StringSegment Subsegment(int offset) => Subsegment(offset, Length - offset);

    public StringSegment Subsegment(int offset, int length)
    {
        ValidateSubsegment(offset, length);
        return HasValue ? new StringSegment(Buffer!, Offset + offset, length) : Empty;
    }

    public string Substring(int offset) => Substring(offset, Length - offset);

    public string Substring(int offset, int length)
    {
        ValidateSubsegment(offset, length);
        return Buffer!.Substring(Offset + offset, length);
    }

    public StringTokenizer Split(char[] chars)
    {
        ArgumentNullException.ThrowIfNull(chars);
        return new StringTokenizer(this, chars);
    }

    public bool Equals(StringSegment other) => Equals(other, StringComparison.Ordinal);

    public bool Equals(StringSegment other, StringComparison comparisonType)
    {
        if (!HasValue || !other.HasValue)
        {
            ValidateComparison(comparisonType);
            return Buffer == other.Buffer;
        }

        return AsSpan().Equals(other.AsSpan(), comparisonType);
    }

    public bool Equals(string? text) => Equals(text, StringComparison.Ordinal);

    public bool Equals(string? text, StringComparison comparisonType)
    {
        if (!HasValue || text is null)
        {
            ValidateComparison(comparisonType);
            return Buffer == text;
        }

        return AsSpan().Equals(text.AsSpan(), comparisonType);
    }

    public static int Compare(StringSegment a, StringSegment b, StringComparison comparisonType)
    {
        if (!a.HasValue || !b.HasValue)
        {
            ValidateComparison(comparisonType);
            return !a.HasValue ? (b.HasValue ? -1 : 0) : 1;
        }

        return a.AsSpan().CompareTo(b.AsSpan(), comparisonType);
    }

    public static bool Equals(StringSegment a, StringSegment b, StringComparison comparisonType) =>
        a.Equals(b, comparisonType);

    public static bool IsNullOrEmpty(StringSegment value) => !value.HasValue || value.Length == 0;

    public static implicit operator StringSegment(string? value) => new(value);

    public static implicit operator ReadOnlySpan<char>(StringSegment segment) => segment.AsSpan();

    public static implicit operator ReadOnlyMemory<char>(StringSegment segment) => segment.AsMemory();

    public static bool operator ==(StringSegment left, StringSegment right) => left.Equals(right);

    public static bool operator !=(StringSegment left, StringSegment right) => !left.Equals(right);

    public override bool Equals(object? obj) => obj switch
    {
        StringSegment segment => Equals(segment),
        string text => Equals(text),
        _ => false,
    };

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    public override string ToString() => Value ?? string.Empty;

    private void ValidateSubsegment(int start, int count)
    {
        if (!HasValue || start < 0 || count < 0 || start > Length - count)
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    private static void ValidateComparison(StringComparison comparisonType)
    {
        if (comparisonType is not StringComparison.Ordinal and not StringComparison.OrdinalIgnoreCase)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
