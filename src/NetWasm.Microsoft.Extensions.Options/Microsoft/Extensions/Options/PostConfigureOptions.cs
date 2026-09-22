using System;

namespace Microsoft.Extensions.Options;

/// <summary>Applies an action to matching named options after regular configuration.</summary>
public class PostConfigureOptions<TOptions> : IPostConfigureOptions<TOptions> where TOptions : class
{
    public PostConfigureOptions(string? name, Action<TOptions>? action)
    {
        Name = name;
        Action = action;
    }

    public string? Name { get; }

    public Action<TOptions>? Action { get; }

    public virtual void PostConfigure(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (Name is null || Name == name)
        {
            Action?.Invoke(options);
        }
    }

    public void PostConfigure(TOptions options) => PostConfigure(Options.DefaultName, options);
}

/// <summary>Applies a post-configuration action with one generated DI dependency.</summary>
public class PostConfigureOptions<TOptions, TDependency> : IPostConfigureOptions<TOptions>
    where TOptions : class
    where TDependency : class
{
    public PostConfigureOptions(string? name, TDependency dependency, Action<TOptions, TDependency>? action)
    {
        Name = name;
        Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
        Action = action;
    }

    public string? Name { get; }

    public TDependency Dependency { get; }

    public Action<TOptions, TDependency>? Action { get; }

    public virtual void PostConfigure(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (Name is null || Name == name)
        {
            Action?.Invoke(options, Dependency);
        }
    }

    public void PostConfigure(TOptions options) => PostConfigure(Options.DefaultName, options);
}
