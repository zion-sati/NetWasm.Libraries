// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class CallSiteValidator
{
    private readonly Dictionary<Type, Type?> _singletonRoots = new();

    internal void ValidateCallSite(ServiceCallSite callSite)
    {
        Validate(callSite, callSite.Cache.Location == CallSiteResultCacheLocation.Root ? callSite.ServiceType : null, new HashSet<Type>());
    }

    internal static void ValidateResolution(ServiceCallSite callSite, ServiceProviderEngineScope scope)
    {
        if (scope.IsRoot && callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
        {
            throw new InvalidOperationException("Cannot resolve a scoped service from the root provider when scope validation is enabled.");
        }
    }

    private void Validate(ServiceCallSite callSite, Type? singletonRoot, HashSet<Type> visited)
    {
        if (!visited.Add(callSite.ServiceType))
        {
            throw new InvalidOperationException("A circular dependency was detected in the service graph.");
        }

        if (singletonRoot is not null)
        {
            _singletonRoots[callSite.ServiceType] = singletonRoot;
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                throw new InvalidOperationException("A singleton cannot depend on a scoped service.");
            }
        }

        if (callSite is ConstructorCallSite constructor)
        {
            for (var index = 0; index < constructor.ParameterCallSites.Length; index++)
            {
                if (constructor.ParameterCallSites[index] is ServiceCallSite parameter)
                {
                    Validate(parameter, singletonRoot, visited);
                }
            }
        }

        visited.Remove(callSite.ServiceType);
    }
}
