// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.Headers;

public sealed class HttpContentHeaders : HttpHeaders
{
    private MediaTypeHeaderValue? _contentType;

    public MediaTypeHeaderValue? ContentType
    {
        get => _contentType;
        set
        {
            _contentType = value;
            if (value is null)
            {
                return;
            }

            SetSingle("Content-Type", value.ToString());
        }
    }
}
