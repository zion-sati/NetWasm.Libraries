using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration;

public static class CommandLineConfigurationExtensions
{
    public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder builder, string[] args) =>
        builder.AddCommandLine(args, null);

    public static IConfigurationBuilder AddCommandLine(
        this IConfigurationBuilder builder,
        string[] args,
        IDictionary<string, string>? switchMappings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);
        return builder.Add(new CommandLine.CommandLineConfigurationSource { Args = args, SwitchMappings = switchMappings });
    }

    public static IConfigurationBuilder AddCommandLine(
        this IConfigurationBuilder builder,
        Action<CommandLine.CommandLineConfigurationSource>? configureSource) =>
        builder.Add(configureSource);
}
