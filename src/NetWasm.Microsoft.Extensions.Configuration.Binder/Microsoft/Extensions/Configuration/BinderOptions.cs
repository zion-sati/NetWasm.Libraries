namespace Microsoft.Extensions.Configuration;

public sealed class BinderOptions
{
    public bool BindNonPublicProperties { get; set; }

    public bool ErrorOnUnknownConfiguration { get; set; }
}
