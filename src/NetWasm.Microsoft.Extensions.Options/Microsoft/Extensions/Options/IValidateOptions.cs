namespace Microsoft.Extensions.Options;

/// <summary>Validates one named or unnamed options instance.</summary>
public interface IValidateOptions<in TOptions> where TOptions : class
{
    ValidateOptionsResult Validate(string? name, TOptions options);
}
