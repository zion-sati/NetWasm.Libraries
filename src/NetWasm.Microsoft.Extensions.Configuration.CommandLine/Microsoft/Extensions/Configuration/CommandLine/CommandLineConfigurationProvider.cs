using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.CommandLine;

public class CommandLineConfigurationProvider : ConfigurationProvider
{
    private readonly Dictionary<string, string>? _switchMappings;

    public CommandLineConfigurationProvider(IEnumerable<string> args, IDictionary<string, string>? switchMappings = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        Args = args;
        if (switchMappings is null) return;
        _switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> mapping in switchMappings)
        {
            if (!mapping.Key.StartsWith('-')) throw new ArgumentException("Switch mappings must start with '-'.", nameof(switchMappings));
            if (!_switchMappings.TryAdd(mapping.Key, mapping.Value)) throw new ArgumentException("Duplicate switch mapping.", nameof(switchMappings));
        }
    }

    protected IEnumerable<string> Args { get; }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        using IEnumerator<string> enumerator = Args.GetEnumerator();
        while (enumerator.MoveNext())
        {
            string current = enumerator.Current;
            int keyStart = current.StartsWith("--", StringComparison.Ordinal)
                ? 2
                : current.StartsWith('-', StringComparison.Ordinal) ? 1
                : current.StartsWith('/', StringComparison.Ordinal) ? 2 : 0;
            if (current.StartsWith('/', StringComparison.Ordinal)) current = "--" + current.Substring(1);
            int separator = current.IndexOf('=');
            string key;
            string value;
            if (separator < 0)
            {
                if (keyStart == 0) continue;
                if (_switchMappings is not null && _switchMappings.TryGetValue(current, out string? mapped)) key = mapped;
                else if (keyStart == 1) continue;
                else key = current.Substring(keyStart);
                if (!enumerator.MoveNext()) continue;
                value = enumerator.Current;
            }
            else
            {
                string keySegment = current.Substring(0, separator);
                if (_switchMappings is not null && _switchMappings.TryGetValue(keySegment, out string? mapped)) key = mapped;
                else if (keyStart == 1) throw new FormatException("The short switch is not defined.");
                else key = current.Substring(keyStart, separator - keyStart);
                value = current.Substring(separator + 1);
            }

            data[key] = value;
        }

        Data = data;
    }
}
