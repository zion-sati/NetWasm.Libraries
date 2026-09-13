// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// MEDI-compatible facade over the generated call-site engine.
/// </summary>
public sealed class ServiceProvider : IServiceProvider, ISupportRequiredService, IServiceScopeFactory,
    IServiceProviderIsService, IKeyedServiceProvider, IServiceProviderIsKeyedService,
    IGeneratedActivator, IDisposable, IAsyncDisposable
{
    private readonly Dictionary<ServiceIdentifier, ServiceAccessor> _serviceAccessors = new();
    private readonly CallSiteValidator? _callSiteValidator;
    private readonly ServiceProviderOptions _options;
    private bool _disposed;

    internal ServiceProviderEngine Engine { get; }

    internal CallSiteFactory CallSiteFactory { get; }

    internal ServiceProviderEngineScope Root { get; }

    internal ServiceProvider(ICollection<ServiceDescriptor> serviceDescriptors, ServiceProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceDescriptors);
        ArgumentNullException.ThrowIfNull(options);
        _options = options;

        Root = new ServiceProviderEngineScope(this, isRootScope: true);
        CallSiteFactory = new CallSiteFactory(serviceDescriptors);
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProvider)), new ServiceProviderCallSite(typeof(IServiceProvider)));
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceScopeFactory)), new ConstantCallSite(typeof(IServiceScopeFactory), this));
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProviderIsService)), new ConstantCallSite(typeof(IServiceProviderIsService), CallSiteFactory));
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IKeyedServiceProvider)), new ConstantCallSite(typeof(IKeyedServiceProvider), this));
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProviderIsKeyedService)), new ConstantCallSite(typeof(IServiceProviderIsKeyedService), this));

        if (options.ValidateScopes)
        {
            _callSiteValidator = new CallSiteValidator();
        }

        Engine = new RuntimeServiceProviderEngine(CallSiteRuntimeResolver.Instance);

        if (options.ValidateOnBuild)
        {
            ValidateOnBuild(serviceDescriptors);
        }
    }

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return GetService(ServiceIdentifier.FromServiceType(serviceType), Root);
    }

    public object GetRequiredService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        var service = GetService(serviceType);
        return service ?? throw new InvalidOperationException("No service is registered for the requested service type.");
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ValidateServiceKey(serviceType, serviceKey);
        return GetService(ServiceIdentifier.FromKeyedServiceType(serviceType, serviceKey!), Root);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ValidateServiceKey(serviceType, serviceKey);
        return GetKeyedService(serviceType, serviceKey)
            ?? throw new InvalidOperationException("No keyed service is registered for the requested service type and key.");
    }

    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        return Root.CreateScope();
    }

    public bool IsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ThrowIfDisposed();
        return CallSiteFactory.IsService(serviceType);
    }

    public bool IsKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ValidateServiceKey(serviceType, serviceKey);
        ThrowIfDisposed();
        return CallSiteFactory.IsKeyedService(serviceType, serviceKey!);
    }

    internal object? GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope scope)
    {
        ThrowIfDisposed();
        scope.ThrowIfDisposed();

        if (!_serviceAccessors.TryGetValue(serviceIdentifier, out var accessor))
        {
            accessor = CreateServiceAccessor(serviceIdentifier);
            _serviceAccessors[serviceIdentifier] = accessor;
        }

        if (accessor.CallSite is ServiceCallSite callSite)
        {
            if (_callSiteValidator is not null)
            {
                CallSiteValidator.ValidateResolution(callSite, scope);
            }
        }

        return accessor.RealizedService(scope);
    }

    object? IGeneratedActivator.Create(Type instanceType, object?[] parameters, Type?[] parameterTypes, string? selectedIdentity)
        => CreateGenerated(Root, instanceType, parameters, parameterTypes, selectedIdentity);

    internal object? CreateGenerated(
        ServiceProviderEngineScope scope,
        Type instanceType,
        object?[] parameters,
        Type?[] parameterTypes,
        string? selectedIdentity)
    {
        ArgumentNullException.ThrowIfNull(instanceType);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        if (parameters.Length != parameterTypes.Length)
        {
            throw new ArgumentException("Generated activation argument values and static argument types must have the same length.", nameof(parameterTypes));
        }

        ThrowIfDisposed();
        scope.ThrowIfDisposed();

        var callSite = CallSiteFactory.GetCallSiteForImplementation(
            instanceType,
            new CallSiteChain(),
            parameterTypes,
            allowUnresolved: true,
            selectedIdentity);
        if (callSite is not ConstructorCallSite constructor)
        {
            throw new InvalidOperationException("Generated activation metadata is required for implementation-type registrations.");
        }

        return CallSiteRuntimeResolver.Instance.ResolveGenerated(constructor, scope, parameters);
    }

    private ServiceAccessor CreateServiceAccessor(ServiceIdentifier serviceIdentifier)
    {
        var callSite = CallSiteFactory.GetCallSite(serviceIdentifier, new CallSiteChain());
        if (callSite is null)
        {
            return new ServiceAccessor(null, _ => null);
        }

        _callSiteValidator?.ValidateCallSite(callSite);
        return new ServiceAccessor(callSite, Engine.RealizeService(callSite));
    }

    private void ValidateOnBuild(ICollection<ServiceDescriptor> serviceDescriptors)
    {
        var failures = new List<Exception>();
        foreach (var descriptor in serviceDescriptors)
        {
            try
            {
                var callSite = CallSiteFactory.GetCallSite(descriptor, new CallSiteChain());
                if (callSite is not null)
                {
                    _callSiteValidator?.ValidateCallSite(callSite);
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        if (failures.Count != 0)
        {
            throw new AggregateException("Some services are not able to be constructed.", failures.ToArray());
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ServiceProvider));
    }

    internal static void ValidateServiceKey(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (ReferenceEquals(serviceKey, KeyedService.AnyKey) &&
            !ActivatorUtilities.TryGetGeneratedSequence(serviceType, KeyedService.AnyKey, out _))
        {
            throw new InvalidOperationException("KeyedService.AnyKey cannot be used to resolve a single service.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Root.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default;
        }

        _disposed = true;
        return Root.DisposeAsync();
    }

    private sealed class ServiceAccessor
    {
        internal ServiceAccessor(ServiceCallSite? callSite, Func<ServiceProviderEngineScope, object?> realizedService)
        {
            CallSite = callSite;
            RealizedService = realizedService;
        }

        internal ServiceCallSite? CallSite { get; }

        internal Func<ServiceProviderEngineScope, object?> RealizedService { get; }
    }
}
