// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    /// <summary>Creates an options object and applies its registered configuration actions.</summary>
    public class OptionsFactory<TOptions> : IOptionsFactory<TOptions> where TOptions : class, new()
    {
        private readonly IConfigureOptions<TOptions>[] _setups;

        public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups)
        {
            _setups = setups is IConfigureOptions<TOptions>[] array
                ? array
                : new List<IConfigureOptions<TOptions>>(setups).ToArray();
        }

        public virtual TOptions Create(string name)
        {
            var options = new TOptions();
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

            return options;
        }
    }
}
