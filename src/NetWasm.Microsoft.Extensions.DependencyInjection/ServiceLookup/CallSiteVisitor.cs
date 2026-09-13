// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal abstract class CallSiteVisitor<TArgument, TResult>
{
    internal TResult Visit(ServiceCallSite callSite, TArgument argument) => callSite.Kind switch
    {
        CallSiteKind.Constant => VisitConstant((ConstantCallSite)callSite, argument),
        CallSiteKind.Factory => VisitFactory((FactoryCallSite)callSite, argument),
        CallSiteKind.Constructor => VisitConstructor((ConstructorCallSite)callSite, argument),
        CallSiteKind.ServiceProvider => VisitServiceProvider((ServiceProviderCallSite)callSite, argument),
        CallSiteKind.Enumerable => VisitEnumerable((IEnumerableCallSite)callSite, argument),
        _ => throw new System.InvalidOperationException("Unknown generated call-site kind."),
    };

    protected abstract TResult VisitConstant(ConstantCallSite callSite, TArgument argument);
    protected abstract TResult VisitFactory(FactoryCallSite callSite, TArgument argument);
    protected abstract TResult VisitConstructor(ConstructorCallSite callSite, TArgument argument);
    protected abstract TResult VisitServiceProvider(ServiceProviderCallSite callSite, TArgument argument);
    protected abstract TResult VisitEnumerable(IEnumerableCallSite callSite, TArgument argument);
}
