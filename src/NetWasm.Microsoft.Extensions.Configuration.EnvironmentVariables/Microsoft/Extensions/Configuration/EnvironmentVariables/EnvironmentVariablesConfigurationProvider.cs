using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables;

public class EnvironmentVariablesConfigurationProvider : ConfigurationProvider
{
    private readonly string _prefix;
    private readonly string _normalizedPrefix;
    private readonly Func<string, string> _transformation;

    public EnvironmentVariablesConfigurationProvider() : this(null, null)
    {
    }

    public EnvironmentVariablesConfigurationProvider(string? prefix) : this(prefix, null)
    {
    }

    public EnvironmentVariablesConfigurationProvider(string? prefix, Func<string, string>? variableNameTransformation)
    {
        _prefix = prefix ?? string.Empty;
        _transformation = variableNameTransformation ?? EnvironmentVariablesConfigurationSource.DefaultTransformation;
        _normalizedPrefix = Normalize(_prefix);
    }

    public override void Load() => Load(Environment.GetEnvironmentVariables());

    public override string ToString() => string.IsNullOrEmpty(_prefix)
        ? "EnvironmentVariablesConfigurationProvider"
        : "EnvironmentVariablesConfigurationProvider Prefix: '" + _prefix + "'";

    internal void Load(IDictionary environmentVariables)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        IDictionaryEnumerator enumerator = environmentVariables.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {
                string key = (string)enumerator.Entry.Key;
                string? value = enumerator.Entry.Value as string;
                if (TryConnectionString(data, key, value)) continue;
                AddIfMatches(data, Normalize(key), value);
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        Data = data;
    }

    private bool TryConnectionString(Dictionary<string, string?> data, string key, string? value)
    {
        (string Prefix, string? Provider)[] prefixes =
        {
            ("MYSQLCONNSTR_", "MySql.Data.MySqlClient"),
            ("SQLAZURECONNSTR_", "System.Data.SqlClient"),
            ("SQLCONNSTR_", "System.Data.SqlClient"),
            ("POSTGRESQLCONNSTR_", "Npgsql"),
            ("CUSTOMCONNSTR_", null),
            ("APIHUBCONNSTR_", null),
            ("DOCDBCONNSTR_", null),
            ("EVENTHUBCONNSTR_", null),
            ("NOTIFICATIONHUBCONNSTR_", null),
            ("REDISCACHECONNSTR_", null),
            ("SERVICEBUSCONNSTR_", null),
        };
        foreach ((string prefix, string? provider) in prefixes)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            string normalized = Normalize(key.Substring(prefix.Length));
            AddIfMatches(data, "ConnectionStrings:" + normalized, value);
            if (provider is not null) AddIfMatches(data, "ConnectionStrings:" + normalized + "_ProviderName", provider);
            return true;
        }

        return false;
    }

    private string Normalize(string key) => _transformation(key) ?? throw new InvalidOperationException("Environment variable transformation returned null.");

    private void AddIfMatches(Dictionary<string, string?> data, string key, string? value)
    {
        if (key.StartsWith(_normalizedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            data[key.Substring(_normalizedPrefix.Length)] = value;
        }
    }
}
