using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder Add<TSource>(this IConfigurationBuilder builder, Action<TSource>? configureSource)
        where TSource : IConfigurationSource, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        var source = new TSource();
        configureSource?.Invoke(source);
        return builder.Add(source);
    }

    public static string? GetConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetSection("ConnectionStrings")[name];

    public static IEnumerable<KeyValuePair<string, string?>> AsEnumerable(this IConfiguration configuration) =>
        configuration.AsEnumerable(false);

    public static IEnumerable<KeyValuePair<string, string?>> AsEnumerable(this IConfiguration configuration, bool makePathsRelative)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var stack = new Stack<IConfiguration>();
        stack.Push(configuration);
        int prefixLength = makePathsRelative && configuration is IConfigurationSection section ? section.Path.Length + 1 : 0;
        while (stack.Count != 0)
        {
            IConfiguration current = stack.Pop();
            if (current is IConfigurationSection child && (!makePathsRelative || !ReferenceEquals(configuration, current)))
            {
                yield return new KeyValuePair<string, string?>(child.Path.Substring(prefixLength), child.Value);
            }

            foreach (IConfigurationSection descendant in current.GetChildren()) stack.Push(descendant);
        }
    }

    public static bool Exists(this IConfigurationSection? section)
    {
        if (section is null || section.Value is not null) return section is not null;
        using IEnumerator<IConfigurationSection> children = section.GetChildren().GetEnumerator();
        return children.MoveNext();
    }

    public static IConfigurationSection GetRequiredSection(this IConfiguration configuration, string key)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        IConfigurationSection section = configuration.GetSection(key);
        if (section.Exists()) return section;
        throw new InvalidOperationException($"The configuration section '{key}' was not found.");
    }
}
