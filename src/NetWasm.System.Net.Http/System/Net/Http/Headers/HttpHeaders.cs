// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.Http.Headers;

public abstract class HttpHeaders
{
    private readonly Dictionary<string, List<string>> _values =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string name, string value)
    {
        ValidateName(name);
        if (value is null)
        {
            throw new ArgumentNullException();
        }

        if (!_values.TryGetValue(name, out var values))
        {
            values = new List<string>();
            _values.Add(name, values);
        }

        values.Add(value);
    }

    public bool TryAddWithoutValidation(string name, string value)
    {
        if (string.IsNullOrEmpty(name) || value is null)
        {
            return false;
        }

        Add(name, value);
        return true;
    }

    public bool TryAddWithoutValidation(string name, IEnumerable<string> values)
    {
        if (values is null)
        {
            return false;
        }

        foreach (var value in values)
        {
            if (!TryAddWithoutValidation(name, value))
            {
                return false;
            }
        }

        return true;
    }

    public bool Contains(string name) => name is not null && _values.ContainsKey(name);

    public IEnumerable<string> GetValues(string name)
    {
        if (!_values.TryGetValue(name ?? throw new ArgumentNullException(), out var values))
        {
            throw new KeyNotFoundException();
        }

        return values.ToArray();
    }

    public bool TryGetValues(string name, out IEnumerable<string>? values)
    {
        if (name is not null && _values.TryGetValue(name, out var stored))
        {
            values = stored.ToArray();
            return true;
        }

        values = null;
        return false;
    }

    public void Clear() => _values.Clear();

    internal void SetSingle(string name, string value)
    {
        ValidateName(name);
        _values[name] = new List<string> { value ?? throw new ArgumentNullException() };
    }

    internal IEnumerable<KeyValuePair<string, string>> Entries()
    {
        foreach (var pair in _values)
        {
            foreach (var value in pair.Value)
            {
                yield return new KeyValuePair<string, string>(pair.Key, value);
            }
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException();
        }
    }
}
