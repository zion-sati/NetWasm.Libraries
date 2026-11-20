using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration;

public static partial class ConfigurationBinder
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
        IConfiguration source = key.Length == 0 ? configuration : configuration.GetSection(key);
        string? value = source is IConfigurationSection section ? section.Value : configuration[key];
        if (value is null) return defaultValue;
        return ConvertValue<T>(value, (source as IConfigurationSection)?.Path ?? key)!;
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
        // As upstream's binder: a ConfigurationSection tells a key explicitly set to null from an absent one.
        return section is ConfigurationSection configurationSection
            ? configurationSection.TryGetValue(key: null, out _)
            : section.Value is not null;
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
