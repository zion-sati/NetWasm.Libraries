// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal sealed class GeneratorDiagnosticException : InvalidOperationException
{
    internal GeneratorDiagnosticException(string code, string message)
        : base($"{code}: {message}")
    {
        Code = code;
    }

    internal GeneratorDiagnosticException(string code, string message, Exception innerException)
        : base($"{code}: {message}", innerException)
    {
        Code = code;
    }

    internal string Code { get; }
}
