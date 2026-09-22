// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options;

/// <summary>Creates an options object and applies its registered configuration and validation actions.</summary>
public class OptionsFactory<TOptions> : IOptionsFactory<TOptions> where TOptions : class
{
    private readonly IConfigureOptions<TOptions>[] _setups;
    private readonly IPostConfigureOptions<TOptions>[] _postConfigures;
    private readonly IValidateOptions<TOptions>[] _validations;

    public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups)
        : this(setups, Array.Empty<IPostConfigureOptions<TOptions>>(), Array.Empty<IValidateOptions<TOptions>>())
    {
    }

    public OptionsFactory(
        IEnumerable<IConfigureOptions<TOptions>> setups,
        IEnumerable<IPostConfigureOptions<TOptions>> postConfigures)
        : this(setups, postConfigures, Array.Empty<IValidateOptions<TOptions>>())
    {
    }

    public OptionsFactory(
        IEnumerable<IConfigureOptions<TOptions>> setups,
        IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
        IEnumerable<IValidateOptions<TOptions>> validations)
    {
        ArgumentNullException.ThrowIfNull(setups);
        ArgumentNullException.ThrowIfNull(postConfigures);
        ArgumentNullException.ThrowIfNull(validations);

        _setups = setups is IConfigureOptions<TOptions>[] setupArray
            ? setupArray
            : new List<IConfigureOptions<TOptions>>(setups).ToArray();
        _postConfigures = postConfigures is IPostConfigureOptions<TOptions>[] postConfigureArray
            ? postConfigureArray
            : new List<IPostConfigureOptions<TOptions>>(postConfigures).ToArray();
        _validations = validations is IValidateOptions<TOptions>[] validationArray
            ? validationArray
            : new List<IValidateOptions<TOptions>>(validations).ToArray();
    }

    public virtual TOptions Create(string name)
    {
        var options = CreateInstance(name);
        foreach (IConfigureOptions<TOptions> setup in _setups)
        {
            if (setup is IConfigureNamedOptions<TOptions> named)
            {
                named.Configure(name, options);
            }
            else if (name == Options.DefaultName)
            {
                setup.Configure(options);
            }
        }

        foreach (IPostConfigureOptions<TOptions> postConfigure in _postConfigures)
        {
            postConfigure.PostConfigure(name, options);
        }

        if (_validations.Length == 0)
        {
            return options;
        }

        var failures = new List<string>();
        foreach (IValidateOptions<TOptions> validation in _validations)
        {
            var result = validation.Validate(name, options);
            if (result is not null && result.Failed && result.Failures is not null)
            {
                failures.AddRange(result.Failures);
            }
        }

        if (failures.Count != 0)
        {
            throw new OptionsValidationException(name, typeof(TOptions), failures);
        }

        return options;
    }

    /// <summary>Creates an options object through the closed public parameterless constructor.</summary>
    protected virtual TOptions CreateInstance(string name) => Activator.CreateInstance<TOptions>();
}
