// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Primitives;

public readonly struct StringTokenizer : IEnumerable<StringSegment>
{
    private readonly StringSegment _value;
    private readonly char[] _separators;

    public StringTokenizer(string value, char[] separators)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(separators);
        _value = value;
        _separators = separators;
    }

    public StringTokenizer(StringSegment value, char[] separators)
    {
        if (!value.HasValue)
        {
            throw new ArgumentException();
        }

        ArgumentNullException.ThrowIfNull(separators);
        _value = value;
        _separators = separators;
    }

    public Enumerator GetEnumerator() => new(_value, _separators);

    IEnumerator<StringSegment> IEnumerable<StringSegment>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<StringSegment>
    {
        private readonly StringSegment _value;
        private readonly char[] _separators;
        private int _index;

        internal Enumerator(StringSegment value, char[] separators)
        {
            _value = value;
            _separators = separators;
            _index = 0;
            Current = default;
        }

        public Enumerator(ref StringTokenizer tokenizer)
        {
            _value = tokenizer._value;
            _separators = tokenizer._separators;
            _index = 0;
            Current = default;
        }

        public StringSegment Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_value.HasValue || _index > _value.Length)
            {
                Current = default;
                return false;
            }

            var next = _value.IndexOfAny(_separators, _index);
            if (next < 0)
            {
                next = _value.Length;
            }

            Current = _value.Subsegment(_index, next - _index);
            _index = next + 1;
            return true;
        }

        public void Reset()
        {
            _index = 0;
            Current = default;
        }

        public void Dispose()
        {
        }
    }
}
