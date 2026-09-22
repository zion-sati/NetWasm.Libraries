namespace Microsoft.Extensions.Options;

/// <summary>Applies an options post-configuration after all regular configuration.</summary>
public interface IPostConfigureOptions<in TOptions> where TOptions : class
{
    void PostConfigure(string? name, TOptions options);
}
