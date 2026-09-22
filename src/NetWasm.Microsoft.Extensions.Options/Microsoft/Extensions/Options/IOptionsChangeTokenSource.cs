using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options;

/// <summary>Provides a change token for a named options instance.</summary>
public interface IOptionsChangeTokenSource<out TOptions>
{
    IChangeToken GetChangeToken();

    string? Name { get; }
}
