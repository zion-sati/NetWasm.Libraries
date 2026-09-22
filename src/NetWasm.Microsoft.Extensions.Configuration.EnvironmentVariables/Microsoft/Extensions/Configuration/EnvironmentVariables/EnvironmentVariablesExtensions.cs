using System;

namespace Microsoft.Extensions.Configuration;

public static class EnvironmentVariablesExtensions
{
    public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(new EnvironmentVariables.EnvironmentVariablesConfigurationSource());
    }

    public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder builder, string? prefix)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(new EnvironmentVariables.EnvironmentVariablesConfigurationSource { Prefix = prefix });
    }

    public static IConfigurationBuilder AddEnvironmentVariables(
        this IConfigurationBuilder builder,
        string? prefix,
        Func<string, string>? variableNameTransformation)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(new EnvironmentVariables.EnvironmentVariablesConfigurationSource
        {
            Prefix = prefix,
            VariableNameTransformation = variableNameTransformation,
        });
    }

    public static IConfigurationBuilder AddEnvironmentVariables(
        this IConfigurationBuilder builder,
        Action<EnvironmentVariables.EnvironmentVariablesConfigurationSource>? configureSource) =>
        builder.Add(configureSource);
}
