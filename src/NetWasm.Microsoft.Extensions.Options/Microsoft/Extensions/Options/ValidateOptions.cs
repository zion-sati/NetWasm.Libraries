using System;

namespace Microsoft.Extensions.Options;

/// <summary>Validates an options instance with a source-level delegate.</summary>
public class ValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
    public ValidateOptions(string? name, Func<TOptions, bool> validation, string failureMessage)
    {
        Name = name;
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        FailureMessage = failureMessage ?? throw new ArgumentNullException(nameof(failureMessage));
    }

    public string? Name { get; }

    public Func<TOptions, bool> Validation { get; }

    public string FailureMessage { get; }

    public virtual ValidateOptionsResult Validate(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (Name is not null && Name != name)
        {
            return ValidateOptionsResult.Skip;
        }

        return Validation(options) ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(FailureMessage);
    }
}

/// <summary>Validates an options instance with one generated DI dependency.</summary>
public class ValidateOptions<TOptions, TDependency> : IValidateOptions<TOptions>
    where TOptions : class
    where TDependency : class
{
    public ValidateOptions(
        string? name,
        TDependency dependency,
        Func<TOptions, TDependency, bool> validation,
        string failureMessage)
    {
        Name = name;
        Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        FailureMessage = failureMessage ?? throw new ArgumentNullException(nameof(failureMessage));
    }

    public string? Name { get; }

    public TDependency Dependency { get; }

    public Func<TOptions, TDependency, bool> Validation { get; }

    public string FailureMessage { get; }

    public virtual ValidateOptionsResult Validate(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (Name is not null && Name != name)
        {
            return ValidateOptionsResult.Skip;
        }

        return Validation(options, Dependency)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(FailureMessage);
    }
}

/// <summary>Applies a generated validator only to the options name selected by an options builder.</summary>
public sealed class NamedValidateOptions<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly string _name;
    private readonly IValidateOptions<TOptions> _validator;

    public NamedValidateOptions(string name, IValidateOptions<TOptions> validator)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public ValidateOptionsResult Validate(string? name, TOptions options) =>
        name == _name ? _validator.Validate(name, options) : ValidateOptionsResult.Skip;
}
