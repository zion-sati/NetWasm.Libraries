// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Text;

namespace Microsoft.Extensions.Primitives;

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("This compatibility type is not supported by the NetWasm profile.", error: true)]
public struct InplaceStringBuilder
{
    private readonly StringBuilder? _builder;
    private int _capacity;

    public InplaceStringBuilder(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        _capacity = capacity;
        _builder = new StringBuilder(capacity);
    }

    public int Capacity
    {
        get => _capacity;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (_builder is not null && _builder.Length != 0)
            {
                throw new InvalidOperationException();
            }

            _capacity = value;
        }
    }

    public void Append(string? value) => Append(value, 0, value?.Length ?? 0);

    public void Append(StringSegment segment) =>
        Append(segment.Buffer, segment.Offset, segment.Length);

    public void Append(string? value, int offset, int count)
    {
        var nonNullValue = value ?? throw new ArgumentNullException();
        if (offset < 0 || count < 0 || offset > nonNullValue.Length - count)
        {
            throw new ArgumentOutOfRangeException();
        }

        EnsureBuilder();
        if (_builder!.Length + count > _capacity)
        {
            throw new InvalidOperationException();
        }

        _builder.Append(nonNullValue, offset, count);
    }

    public void Append(char c)
    {
        EnsureBuilder();
        if (_builder!.Length >= _capacity)
        {
            throw new InvalidOperationException();
        }

        _builder.Append(c);
    }

    public override string ToString()
    {
        EnsureBuilder();
        if (_builder!.Length != _capacity)
        {
            throw new InvalidOperationException();
        }

        return _builder.ToString();
    }

    private void EnsureBuilder()
    {
        // A struct default is retained for source compatibility; lazily use
        // the configured capacity when the first append arrives.
        if (_builder is null)
        {
            throw new InvalidOperationException();
        }
    }
}
