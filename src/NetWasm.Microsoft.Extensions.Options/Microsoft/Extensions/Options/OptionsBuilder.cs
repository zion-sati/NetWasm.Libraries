using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options;

/// <summary>Fluent, source-compatible registration surface for one named options type.</summary>
public class OptionsBuilder<TOptions> where TOptions : class
{
    private const string DefaultValidationFailureMessage = "A validation error has occurred.";

    public OptionsBuilder(IServiceCollection services, string? name)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Name = name ?? Options.DefaultName;
    }

    public string Name { get; }

    public IServiceCollection Services { get; }

    public virtual OptionsBuilder<TOptions> Configure(Action<TOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        Services.AddSingleton<IConfigureOptions<TOptions>>(new ConfigureNamedOptions<TOptions>(Name, configureOptions));
        return this;
    }

    public virtual OptionsBuilder<TOptions> PostConfigure(Action<TOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        Services.AddSingleton<IPostConfigureOptions<TOptions>>(new PostConfigureOptions<TOptions>(Name, configureOptions));
        return this;
    }

    public virtual OptionsBuilder<TOptions> Validate(Func<TOptions, bool> validation) =>
        Validate(validation, DefaultValidationFailureMessage);

    public virtual OptionsBuilder<TOptions> Validate(Func<TOptions, bool> validation, string failureMessage)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(failureMessage);
        Services.AddSingleton<IValidateOptions<TOptions>>(new ValidateOptions<TOptions>(Name, validation, failureMessage));
        return this;
    }

    public virtual OptionsBuilder<TOptions> Validate(IValidateOptions<TOptions> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        Services.AddSingleton<IValidateOptions<TOptions>>(new NamedValidateOptions<TOptions>(Name, validator));
        return this;
    }
}
