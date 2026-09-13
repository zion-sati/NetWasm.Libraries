namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator
{
    internal static class Guard
    {
        internal static void NotNull(object? value, string parameterName)
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        internal static void NotNullOrEmpty(string? value, string parameterName)
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("The value cannot be an empty string.", parameterName);
            }
        }

        internal static string NormalizeLineEndings(string value) =>
            value.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
