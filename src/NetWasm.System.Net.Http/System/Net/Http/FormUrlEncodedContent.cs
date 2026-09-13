// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.Net.Http;

public sealed class FormUrlEncodedContent : StringContent
{
    public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        : base(Encode(nameValueCollection), Encoding.UTF8, "application/x-www-form-urlencoded")
    {
    }

    private static string Encode(IEnumerable<KeyValuePair<string, string>> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException();
        }

        var builder = new StringBuilder();
        foreach (var pair in values)
        {
            if (builder.Length > 0)
            {
                builder.Append('&');
            }

            builder.Append(Escape(pair.Key));
            builder.Append('=');
            builder.Append(Escape(pair.Value));
        }

        return builder.ToString();
    }

    private static string Escape(string value) =>
        Uri.EscapeDataString(value ?? throw new ArgumentNullException()).Replace("%20", "+", StringComparison.Ordinal);
}
