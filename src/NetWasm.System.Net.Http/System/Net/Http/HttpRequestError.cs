// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http;

public enum HttpRequestError
{
    Unknown = 0,
    NameResolutionError = 1,
    ConnectionError = 2,
    SecureConnectionError = 3,
    HttpProtocolError = 4,
    InvalidResponse = 5,
    UserCanceled = 6,
    ConfigurationError = 7,
}
