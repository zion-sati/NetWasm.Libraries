// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

internal sealed class ServiceProviderEngineScope : IServiceScope, IServiceProvider, IServiceScopeFactory, IKeyedServiceProvider, IGeneratedActivator, IDisposable, IAsyncDisposable
{
    private readonly Dictionary<ServiceCacheKey, object?> _resolvedServices = new();
    private readonly List<object> _disposables = new();
    private bool _disposed;

    internal ServiceProviderEngineScope(ServiceProvider provider, bool isRootScope)
    {
        ResolvedServices = _resolvedServices;
        Provider = provider;
        IsRoot = isRootScope;
        Root = isRootScope ? this : provider.Root;
    }

    internal Dictionary<ServiceCacheKey, object?> ResolvedServices { get; }

    internal ServiceProvider Provider { get; }

    internal ServiceProviderEngineScope Root { get; }

    internal bool IsRoot { get; }

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ThrowIfDisposed();
        return Provider.GetService(ServiceIdentifier.FromServiceType(serviceType), this);
    }

    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        return new ServiceProviderEngineScope(Provider, false);
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        global::Microsoft.Extensions.DependencyInjection.ServiceProvider.ValidateServiceKey(serviceType, serviceKey);
        ThrowIfDisposed();
        return Provider.GetService(ServiceIdentifier.FromKeyedServiceType(serviceType, serviceKey!), this);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        var service = GetKeyedService(serviceType, serviceKey);
        return service ?? throw new InvalidOperationException("No keyed service is registered for the requested service type and key.");
    }

    object? IGeneratedActivator.Create(Type instanceType, object?[] parameters, Type?[] parameterTypes, string? selectedIdentity) =>
        Provider.CreateGenerated(this, instanceType, parameters, parameterTypes, selectedIdentity);

    internal bool TryGetResolved(ServiceCacheKey key, out object? value) => _resolvedServices.TryGetValue(key, out value);

    internal void SetResolved(ServiceCacheKey key, object? value) => _resolvedServices[key] = value;

    internal void CaptureDisposable(object value)
    {
        if (value is IDisposable || value is IAsyncDisposable)
        {
            _disposables.Add(value);
        }
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ServiceProvider));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        for (var index = _disposables.Count - 1; index >= 0; index--)
        {
            if (_disposables[index] is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else
            {
                throw new InvalidOperationException("An asynchronous disposable was resolved from a synchronous scope.");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        for (var index = _disposables.Count - 1; index >= 0; index--)
        {
            if (_disposables[index] is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_disposables[index] is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
