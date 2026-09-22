using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationBinder
{
    public static T? Get<T>(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (BinderRegistry<T>.Factory is null)
            throw new NotSupportedException("ConfigurationBinder.Get<T> requires the NetWasm configuration binding generator for this closed T.");
        return BinderRegistry<T>.Factory(configuration, new BinderOptions());
    }

    public static T? Get<T>(this IConfiguration configuration, Action<BinderOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new BinderOptions();
        configureOptions?.Invoke(options);
        ValidateOptions(options);
        if (BinderRegistry<T>.Factory is null)
            throw new NotSupportedException("ConfigurationBinder.Get<T> requires the NetWasm configuration binding generator for this closed T.");
        return BinderRegistry<T>.Factory(configuration, options);
    }

    public static void Bind<T>(this IConfiguration configuration, T instance)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(instance);
        if (BinderRegistry<T>.Binder is null)
            throw new NotSupportedException("ConfigurationBinder.Bind<T> requires the NetWasm configuration binding generator for this closed T.");
        BinderRegistry<T>.Binder(configuration, new BinderOptions(), instance);
    }

    public static void Bind<T>(this IConfiguration configuration, T instance, Action<BinderOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(instance);
        var options = new BinderOptions();
        configureOptions?.Invoke(options);
        ValidateOptions(options);
        if (BinderRegistry<T>.Binder is null)
            throw new NotSupportedException("ConfigurationBinder.Bind<T> requires the NetWasm configuration binding generator for this closed T.");
        BinderRegistry<T>.Binder(configuration, options, instance);
    }

    public static T? GetValue<T>(this IConfiguration configuration, string key)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return GetValue(configuration, key, default(T));
    }

    public static T GetValue<T>(this IConfiguration configuration, string key, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? value = key.Length == 0 && configuration is IConfigurationSection section
            ? section.Value
            : configuration[key];
        if (value is null) return defaultValue;
        object? converted = ConvertValue(typeof(T), value);
        return (T)converted!;
    }

    public static void Register<T>(Func<IConfiguration, BinderOptions, T?> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        BinderRegistry<T>.Factory = factory;
    }

    public static void Register<T>(Func<IConfiguration, BinderOptions, T?> factory, Action<IConfiguration, BinderOptions, T> binder)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(binder);
        BinderRegistry<T>.Factory = factory;
        BinderRegistry<T>.Binder = binder;
    }

    public static IReadOnlyList<IConfigurationSection> GetChildren(IConfiguration configuration, string key)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var result = new List<IConfigurationSection>();
        foreach (IConfigurationSection child in configuration.GetSection(key).GetChildren()) result.Add(child);
        return result;
    }

    public static bool ValueIsPresent(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return section is IConfigurationSectionValueAccessor accessor
            ? accessor.TryGetValue(out _)
            : section.Value is not null;
    }

    private static object? ConvertValue(Type type, string value)
    {
        Type? nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
        {
            if (value.Length == 0) return null;
            object? nested = ConvertValue(nullable, value);
            return nested;
        }

        if (type == typeof(string)) return value;
        if (type == typeof(bool)) return bool.Parse(value);
        if (type == typeof(byte)) return byte.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(short)) return short.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(long)) return long.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(decimal)) return decimal.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(Guid)) return Guid.Parse(value);
        throw new FormatException($"The configuration value '{value}' cannot be converted to {type}.");
    }

    private static void ValidateOptions(BinderOptions options)
    {
        if (options.BindNonPublicProperties)
            throw new NotSupportedException("NetWasm configuration binding does not support non-public properties.");
        if (options.ErrorOnUnknownConfiguration)
            throw new NotSupportedException("NetWasm configuration binding does not support ErrorOnUnknownConfiguration yet.");
    }

    private static class BinderRegistry<T>
    {
        internal static Func<IConfiguration, BinderOptions, T?>? Factory;

        internal static Action<IConfiguration, BinderOptions, T>? Binder;
    }
}
