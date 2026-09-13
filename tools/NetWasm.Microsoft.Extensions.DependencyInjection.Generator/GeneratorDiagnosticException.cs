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
