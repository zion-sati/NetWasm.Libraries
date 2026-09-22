using System;

namespace Microsoft.Extensions.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationKeyNameAttribute : Attribute
{
    public ConfigurationKeyNameAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

    public string Name { get; }
}
