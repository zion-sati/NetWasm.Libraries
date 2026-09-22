// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.Headers;

public sealed class HttpContentHeaders : HttpHeaders
{
    private MediaTypeHeaderValue? _contentType;

    public MediaTypeHeaderValue? ContentType
    {
        get
        {
            if (_contentType is null && TryGetValues("Content-Type", out var values))
            {
                using var enumerator = values!.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    _contentType = new MediaTypeHeaderValue(enumerator.Current);
                }
            }

            return _contentType;
        }
        set
        {
            _contentType = value;
            if (value is null)
            {
                Remove("Content-Type");
                return;
            }

            SetSingle("Content-Type", value.ToString());
        }
    }

    public long? ContentLength
    {
        get
        {
            if (!TryGetValues("Content-Length", out var values))
            {
                return null;
            }

            using var enumerator = values!.GetEnumerator();
            if (!enumerator.MoveNext() || !long.TryParse(enumerator.Current, out var length) || length < 0)
            {
                throw new FormatException();
            }

            return length;
        }
        set
        {
            if (value is null)
            {
                Remove("Content-Length");
                return;
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            SetSingle("Content-Length", value.Value.ToString());
        }
    }
}
