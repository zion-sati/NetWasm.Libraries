// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Primitives;

public readonly struct StringValues :
    IList<string?>,
    IReadOnlyList<string?>,
    IEquatable<StringValues>,
    IEquatable<string?>,
    IEquatable<string?[]>
{
    private readonly object? _values;

    public static readonly StringValues Empty = new(Array.Empty<string?>());

    public StringValues(string? value)
    {
        _values = value;
    }

    public StringValues(string?[]? values)
    {
        _values = values;
    }

    public int Count => _values switch
    {
        null => 0,
        string => 1,
        string[] values => values.Length,
        _ => 0,
    };

    bool ICollection<string?>.IsReadOnly => true;

    public string? this[int index]
    {
        get
        {
            if (_values is string value)
            {
                return index == 0 ? value : ThrowIndexOutOfRange();
            }

            if (_values is string[] values && (uint)index < (uint)values.Length)
            {
                return values[index];
            }

            return ThrowIndexOutOfRange();
        }
    }

    string? IList<string?>.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    public static StringValues Concat(StringValues values1, StringValues values2)
    {
        if (values1.Count == 0)
        {
            return values2;
        }

        if (values2.Count == 0)
        {
            return values1;
        }

        var values = new string?[values1.Count + values2.Count];
        CopyTo(values1, values, 0);
        CopyTo(values2, values, values1.Count);
        return new StringValues(values);
    }

    public static StringValues Concat(in StringValues values, string? value) =>
        Concat(values, new StringValues(value));

    public static StringValues Concat(string? value, in StringValues values) =>
        Concat(new StringValues(value), values);

    public bool Equals(StringValues other) => Equals(this, other);

    public bool Equals(string? other) => Equals(this, new StringValues(other));

    public bool Equals(string?[]? other)
    {
        if (other is null)
        {
            return Count == 0;
        }

        if (Count != other.Length)
        {
            return false;
        }

        for (var index = 0; index < other.Length; index++)
        {
            if (!string.Equals(this[index], other[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static bool Equals(StringValues left, StringValues right) =>
        left.Equals(right, StringComparison.Ordinal);

    public static bool Equals(StringValues left, string? right) => left.Equals(right);

    public static bool Equals(StringValues left, string?[]? right) => left.Equals(right);

    public static bool Equals(string? left, StringValues right) => right.Equals(left);

    public static bool Equals(string?[]? left, StringValues right) => right.Equals(left);

    public static bool IsNullOrEmpty(StringValues value) => value.Count == 0 ||
        (value.Count == 1 && string.IsNullOrEmpty(value[0]));

    public string?[] ToArray()
    {
        if (_values is null)
        {
            return Array.Empty<string?>();
        }

        if (_values is string[] values)
        {
            return values;
        }

        return new[] { (string?)_values };
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<string?> IEnumerable<string?>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override bool Equals(object? obj) => obj switch
    {
        null => Equals(this, Empty),
        StringValues values => Equals(values),
        string value => Equals(value),
        string[] values => Equals(values),
        _ => false,
    };

    public override int GetHashCode()
    {
        if (_values is string value)
        {
            return value.GetHashCode();
        }

        if (_values is not string?[] values)
        {
            return 0;
        }

        if (values.Length == 1)
        {
            return values[0]?.GetHashCode() ?? 1;
        }

        var hash = 0;
        for (var index = 0; index < values.Length; index++)
        {
            hash = CombineHashCodes(hash, values[index]?.GetHashCode() ?? 0);
        }

        return hash;
    }

    public override string ToString()
    {
        if (Count == 0)
        {
            return string.Empty;
        }

        if (Count == 1)
        {
            return this[0] ?? string.Empty;
        }

        var builder = new StringBuilder();
        var hasValue = false;
        for (var index = 0; index < Count; index++)
        {
            var value = this[index];
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (hasValue)
            {
                builder.Append(',');
            }

            builder.Append(value);
            hasValue = true;
        }

        return builder.ToString();
    }

    public static implicit operator StringValues(string? value) => new(value);

    public static implicit operator StringValues(string?[]? value) => new(value);

    public static implicit operator string?(StringValues values) =>
        values.Count == 0 ? null : values.Count == 1 ? values[0] : values.ToString();

    public static implicit operator string?[]?(StringValues values) => values.GetArrayValue();

    public static bool operator ==(StringValues left, StringValues right) => left.Equals(right);
    public static bool operator !=(StringValues left, StringValues right) => !left.Equals(right);
    public static bool operator ==(StringValues left, string? right) => left.Equals(right);
    public static bool operator !=(StringValues left, string? right) => !left.Equals(right);
    public static bool operator ==(string? left, StringValues right) => right.Equals(left);
    public static bool operator !=(string? left, StringValues right) => !right.Equals(left);
    public static bool operator ==(StringValues left, string?[]? right) => left.Equals(right);
    public static bool operator !=(StringValues left, string?[]? right) => !left.Equals(right);
    public static bool operator ==(string?[]? left, StringValues right) => right.Equals(left);
    public static bool operator !=(string?[]? left, StringValues right) => !right.Equals(left);
    public static bool operator ==(StringValues left, object? right) => left.Equals(right);
    public static bool operator !=(StringValues left, object? right) => !left.Equals(right);
    public static bool operator ==(object? left, StringValues right) => right.Equals(left);
    public static bool operator !=(object? left, StringValues right) => !right.Equals(left);

    private bool Equals(StringValues other, StringComparison comparisonType)
    {
        if (Count != other.Count)
        {
            return false;
        }

        for (var index = 0; index < Count; index++)
        {
            if (!string.Equals(this[index], other[index], comparisonType))
            {
                return false;
            }
        }

        return true;
    }

    private static void CopyTo(StringValues values, string?[] destination, int offset)
    {
        for (var index = 0; index < values.Count; index++)
        {
            destination[offset + index] = values[index];
        }
    }

    private static string? ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

    private string?[]? GetArrayValue() => _values switch
    {
        null => null,
        string value => new[] { value },
        string?[] values => values,
        _ => null,
    };

    private static int CombineHashCodes(int left, int right)
    {
        uint rotated = ((uint)left << 5) | ((uint)left >> 27);
        return unchecked(((int)rotated + left) ^ right);
    }

    bool ICollection<string?>.Contains(string? item) => IndexOf(item) >= 0;

    int IList<string?>.IndexOf(string? item) => IndexOf(item);

    void ICollection<string?>.CopyTo(string?[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (arrayIndex < 0 || arrayIndex > array.Length - Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        CopyTo(this, array, arrayIndex);
    }

    void ICollection<string?>.Add(string? item) => throw new NotSupportedException();
    void ICollection<string?>.Clear() => throw new NotSupportedException();
    bool ICollection<string?>.Remove(string? item) => throw new NotSupportedException();
    void IList<string?>.Insert(int index, string? item) => throw new NotSupportedException();
    void IList<string?>.RemoveAt(int index) => throw new NotSupportedException();

    private int IndexOf(string? item)
    {
        for (var index = 0; index < Count; index++)
        {
            if (string.Equals(this[index], item, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    public struct Enumerator : IEnumerator<string?>
    {
        private readonly StringValues _values;
        private int _index;

        internal Enumerator(StringValues values)
        {
            _values = values;
            _index = -1;
        }

        public string? Current => _index < 0 ? null : _values[_index];

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_index + 1 >= _values.Count)
            {
                return false;
            }

            _index++;
            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
