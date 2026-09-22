using System;

namespace Microsoft.Extensions.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationIgnoreAttribute : Attribute
{
}
