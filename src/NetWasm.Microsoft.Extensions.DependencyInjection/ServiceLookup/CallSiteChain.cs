// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class CallSiteChain
{
    private readonly List<ServiceIdentifier> _chain = new();

    internal void CheckCircularDependency(ServiceIdentifier serviceIdentifier)
    {
        if (_chain.Contains(serviceIdentifier))
        {
            throw new InvalidOperationException("A circular dependency was detected in the service graph.");
        }

        _chain.Add(serviceIdentifier);
    }

    internal void Remove(ServiceIdentifier serviceIdentifier)
    {
        for (var index = _chain.Count - 1; index >= 0; index--)
        {
            if (_chain[index].Equals(serviceIdentifier))
            {
                _chain.RemoveAt(index);
                return;
            }
        }
    }
}
