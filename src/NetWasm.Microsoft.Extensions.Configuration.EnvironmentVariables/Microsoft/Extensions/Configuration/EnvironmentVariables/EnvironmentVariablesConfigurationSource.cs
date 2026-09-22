using System;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables;

public class EnvironmentVariablesConfigurationSource : IConfigurationSource
{
    public static Func<string, string> DefaultTransformation { get; } = static name =>
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
    };

    public static Func<string, string> ColonAndDotTransformation { get; } = static name =>
    {
        ArgumentNullException.ThrowIfNull(name);
        var result = new System.Text.StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] == '_' && i + 1 < name.Length && name[i + 1] == '_')
            {
                if (i + 2 < name.Length && name[i + 2] == '_')
                {
                    result.Append('.');
                    i += 2;
                }
                else
                {
                    result.Append(':');
                    i++;
                }
            }
            else result.Append(name[i]);
        }

        return result.ToString();
    };

    public string? Prefix { get; set; }

    public Func<string, string>? VariableNameTransformation { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new EnvironmentVariablesConfigurationProvider(Prefix, VariableNameTransformation);
}
