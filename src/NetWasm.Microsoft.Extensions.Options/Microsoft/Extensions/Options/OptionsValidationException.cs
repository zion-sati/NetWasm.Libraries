using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options;

/// <summary>Thrown when one or more registered options validators fail.</summary>
public sealed class OptionsValidationException : Exception
{
    public OptionsValidationException(string optionsName, Type optionsType, IEnumerable<string>? failures)
    {
        ArgumentNullException.ThrowIfNull(optionsName);
        ArgumentNullException.ThrowIfNull(optionsType);

        OptionsName = optionsName;
        OptionsType = optionsType;
        Failures = failures is null ? Array.Empty<string>() : new List<string>(failures);
    }

    public string OptionsName { get; }

    public Type OptionsType { get; }

    public IEnumerable<string> Failures { get; }

    public override string Message => string.Join("; ", Failures);
}
