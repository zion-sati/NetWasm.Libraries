// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class CallSiteRuntimeResolver : CallSiteVisitor<ServiceProviderEngineScope, object?>
{
    internal static readonly CallSiteRuntimeResolver Instance = new();

    internal object? Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
    {
        var targetScope = callSite.Cache.Location == CallSiteResultCacheLocation.Root ? scope.Root : scope;
        if (callSite.Cache.Location is CallSiteResultCacheLocation.Root or CallSiteResultCacheLocation.Scope &&
            targetScope.TryGetResolved(callSite.Cache.Key, out var cached))
        {
            return cached;
        }

        var value = Visit(callSite, scope);
        if (callSite.Cache.Location is CallSiteResultCacheLocation.Root or CallSiteResultCacheLocation.Scope)
        {
            targetScope.SetResolved(callSite.Cache.Key, value);
        }

        if (value is not null && callSite.Cache.Location is not CallSiteResultCacheLocation.None)
        {
            targetScope.CaptureDisposable(value);
        }

        return value;
    }

    internal object? ResolveGenerated(ConstructorCallSite callSite, ServiceProviderEngineScope scope, IReadOnlyList<object?> supplied)
    {
        scope.ThrowIfDisposed();

        var arguments = new object?[callSite.ParameterCallSites.Length];
        for (var index = 0; index < arguments.Length; index++)
        {
            var suppliedIndex = callSite.SuppliedParameterIndexes[index];
            if (suppliedIndex >= 0)
            {
                if (suppliedIndex >= supplied.Count)
                {
                    throw new ArgumentException("Generated activation supplied-argument metadata is inconsistent.", nameof(supplied));
                }

                arguments[index] = supplied[suppliedIndex];
            }
            else if (callSite.ParameterCallSites[index] is ServiceCallSite parameter)
            {
                arguments[index] = Resolve(parameter, scope);
            }
            else if (callSite.Activation.Parameters[index].HasDefaultValue)
            {
                arguments[index] = callSite.Activation.Parameters[index].DefaultValue;
            }
            else
            {
                ThrowHelper.ThrowNoService();
            }
        }

        return callSite.Activation.Activate(arguments);
    }

    protected override object? VisitConstant(ConstantCallSite callSite, ServiceProviderEngineScope scope) => callSite.Value;

    protected override object? VisitFactory(FactoryCallSite callSite, ServiceProviderEngineScope scope) => callSite.Factory(scope);

    protected override object? VisitConstructor(ConstructorCallSite callSite, ServiceProviderEngineScope scope)
    {
        var parameters = callSite.ParameterCallSites;
        var arguments = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            arguments[index] = parameters[index] is ServiceCallSite parameter
                ? Resolve(parameter, scope)
                : callSite.Activation.Parameters[index].DefaultValue;
        }

        return callSite.Activation.Activate(arguments);
    }

    protected override object? VisitServiceProvider(ServiceProviderCallSite callSite, ServiceProviderEngineScope scope) => scope;

    protected override object? VisitEnumerable(IEnumerableCallSite callSite, ServiceProviderEngineScope scope)
    {
        var values = new object?[callSite.ServiceCallSites.Length];
        for (var index = 0; index < callSite.ServiceCallSites.Length; index++)
        {
            values[index] = Resolve(callSite.ServiceCallSites[index], scope);
        }

        return callSite.Materialize(values);
    }
}
