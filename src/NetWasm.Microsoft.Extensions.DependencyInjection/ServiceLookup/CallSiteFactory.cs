// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Generated;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup;

/// <summary>
/// Builds and caches the upstream-shaped call-site graph from an immutable descriptor snapshot.
/// Constructor selection is supplied by generated activation metadata.
/// </summary>
internal sealed class CallSiteFactory : IServiceProviderIsService
{
    private readonly ServiceDescriptor[] _descriptors;
    private readonly Dictionary<ServiceIdentifier, ServiceDescriptorCacheItem> _descriptorLookup = new();
    private readonly Dictionary<ServiceCacheKey, ServiceCallSite> _callSiteCache = new();
    private readonly Dictionary<(Type ImplementationType, object? ServiceKey), ServiceDescriptor> _implementationLookup = new();
    private readonly Dictionary<Type, HashSet<Type>> _assignabilityLookup = new();
    private readonly IReadOnlyDictionary<(Type ServiceType, Type ImplementationType, object? ServiceKey), GeneratedActivationDescriptor>
        _generatedActivations = GeneratedActivationRegistry.CaptureActivations();
    private readonly IReadOnlyDictionary<(Type ServiceType, Type ImplementationType, object? ServiceKey), IReadOnlyList<GeneratedActivationDescriptor>>
        _generatedOpenGenericActivations = GeneratedActivationRegistry.CaptureOpenGenericActivations();
    private readonly HashSet<(
        Type ServiceType,
        Type ImplementationType,
        bool IsKeyedService,
        object? ServiceKey)> _generatedOpenGenericRegistrations =
        GeneratedActivationRegistry.CaptureOpenGenericRegistrations();

    internal CallSiteFactory(ICollection<ServiceDescriptor> descriptors)
    {
        var suppliedDescriptors = new ServiceDescriptor[descriptors.Count];
        descriptors.CopyTo(suppliedDescriptors, 0);
        _descriptors = ExpandOpenGenericDescriptors(suppliedDescriptors);
        Populate();
    }

    private ServiceDescriptor[] ExpandOpenGenericDescriptors(ServiceDescriptor[] descriptors)
    {
        var expanded = new List<ServiceDescriptor>(descriptors.Length);
        for (var index = 0; index < descriptors.Length; index++)
        {
            var descriptor = descriptors[index];
            expanded.Add(descriptor);
            var implementationType = descriptor.IsKeyedService
                ? descriptor.KeyedImplementationType
                : descriptor.ImplementationType;
            if (implementationType is not Type implementation ||
                !_generatedOpenGenericActivations.TryGetValue(
                    (descriptor.ServiceType, implementation, descriptor.ServiceKey),
                    out var activations))
            {
                continue;
            }

            for (var activationIndex = 0; activationIndex < activations.Count; activationIndex++)
            {
                var activation = activations[activationIndex];
                expanded.Add(new GeneratedServiceDescriptor(
                    activation.ServiceType,
                    activation.ServiceKey,
                    activation.ImplementationType,
                    descriptor.Lifetime,
                    activation));
            }
        }

        return expanded.ToArray();
    }

    private void Populate()
    {
        for (var index = 0; index < _descriptors.Length; index++)
        {
            var descriptor = _descriptors[index];
            var identifier = descriptor.IsKeyedService
                ? ServiceIdentifier.FromKeyedServiceType(descriptor.ServiceType, descriptor.ServiceKey)
                : ServiceIdentifier.FromServiceType(descriptor.ServiceType);
            _descriptorLookup.TryGetValue(identifier, out var cacheItem);
            cacheItem ??= new ServiceDescriptorCacheItem();
            _descriptorLookup[identifier] = cacheItem.Add(descriptor);

            var implementationType = descriptor.IsKeyedService
                ? descriptor.KeyedImplementationType
                : descriptor.ImplementationType;
            if (implementationType is Type implementation)
            {
                _implementationLookup[(implementation, descriptor.ServiceKey)] = descriptor;
                if (TryGetGeneratedActivation(descriptor, implementation, out var activation))
                {
                    AddAssignableType(implementation, implementation);
                    AddAssignableType(implementation, descriptor.ServiceType);
                    for (var assignableIndex = 0; assignableIndex < activation.AssignableTypes.Count; assignableIndex++)
                    {
                        AddAssignableType(implementationType, activation.AssignableTypes[assignableIndex]);
                    }
                }
            }
        }
    }

    private void AddAssignableType(Type suppliedType, Type expectedType)
    {
        if (!_assignabilityLookup.TryGetValue(suppliedType, out var expectedTypes))
        {
            expectedTypes = new HashSet<Type>();
            _assignabilityLookup[suppliedType] = expectedTypes;
        }

        expectedTypes.Add(expectedType);
    }

    internal void Add(ServiceIdentifier identifier, ServiceCallSite callSite) =>
        _callSiteCache[new ServiceCacheKey(identifier, 0)] = callSite;

    public bool IsService(Type serviceType)
    {
        var identifier = ServiceIdentifier.FromServiceType(serviceType);
        return _descriptorLookup.ContainsKey(identifier) ||
            _callSiteCache.ContainsKey(new ServiceCacheKey(identifier, 0)) ||
            ActivatorUtilities.TryGetGeneratedSequence(serviceType, serviceKey: null, out _);
    }

    internal bool IsKeyedService(Type serviceType, object serviceKey)
    {
        var identifier = ServiceIdentifier.FromKeyedServiceType(serviceType, serviceKey);
        return TryGetDescriptorItem(identifier, out _) ||
            _callSiteCache.ContainsKey(new ServiceCacheKey(identifier, 0)) ||
            ActivatorUtilities.TryGetGeneratedSequence(serviceType, serviceKey, out _);
    }

    internal ServiceCallSite? GetCallSite(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
    {
        var key = new ServiceCacheKey(serviceIdentifier, 0);
        if (_callSiteCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!TryGetDescriptorItem(serviceIdentifier, out var descriptorItem))
        {
            var sequenceCallSite = CreateEnumerableCallSite(serviceIdentifier, callSiteChain);
            if (sequenceCallSite is not null)
            {
                _callSiteCache[key] = sequenceCallSite;
            }

            return sequenceCallSite;
        }

        callSiteChain.CheckCircularDependency(serviceIdentifier);
        try
        {
            var descriptor = descriptorItem.Last!;
            var callSite = CreateCallSite(descriptor, serviceIdentifier, callSiteChain);
            _callSiteCache[key] = callSite;
            return callSite;
        }
        finally
        {
            callSiteChain.Remove(serviceIdentifier);
        }
    }

    internal ServiceCallSite? GetCallSite(ServiceDescriptor descriptor, CallSiteChain callSiteChain, bool allowUnresolved = false)
    {
        if (IsGeneratedOpenGenericRegistration(descriptor))
        {
            return null;
        }

        var identifier = descriptor.IsKeyedService
            ? ServiceIdentifier.FromKeyedServiceType(descriptor.ServiceType, descriptor.ServiceKey)
            : ServiceIdentifier.FromServiceType(descriptor.ServiceType);
        if (!_descriptorLookup.TryGetValue(identifier, out var item))
        {
            return null;
        }

        var slot = item.GetSlot(descriptor);
        return CreateCallSite(descriptor, identifier, callSiteChain, slot, allowUnresolved);
    }

    private bool IsGeneratedOpenGenericRegistration(ServiceDescriptor descriptor)
    {
        var implementationType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationType
            : descriptor.ImplementationType;
        return implementationType is Type implementation &&
            _generatedOpenGenericRegistrations.Contains((
                descriptor.ServiceType,
                implementation,
                descriptor.IsKeyedService,
                descriptor.ServiceKey));
    }

    internal ServiceCallSite? GetCallSiteForImplementation(
        Type implementationType,
        CallSiteChain callSiteChain,
        IReadOnlyList<Type?>? suppliedTypes = null,
        bool allowUnresolved = false,
        string? selectedIdentity = null,
        object? serviceKey = null)
    {
        if (!_implementationLookup.TryGetValue((implementationType, ServiceKey: serviceKey), out var descriptor) &&
            (serviceKey is null || !_implementationLookup.TryGetValue((implementationType, ServiceKey: KeyedService.AnyKey), out descriptor)))
        {
            foreach (var pair in _generatedActivations)
            {
                if (pair.Key.ImplementationType == implementationType && pair.Key.ServiceKey == serviceKey)
                {
                    var activation = pair.Value;
                    var generated = new GeneratedServiceDescriptor(
                        activation.ServiceType,
                        activation.ServiceKey,
                        activation.ImplementationType,
                        ServiceLifetime.Transient,
                        activation);
                    var generatedIdentifier = serviceKey is null
                        ? ServiceIdentifier.FromServiceType(activation.ServiceType)
                        : ServiceIdentifier.FromKeyedServiceType(activation.ServiceType, serviceKey);
                    return CreateCallSite(
                        generated,
                        generatedIdentifier,
                        callSiteChain,
                        0,
                        allowUnresolved,
                        suppliedTypes,
                        selectedIdentity);
                }
            }

            return null;
        }

        var identifier = serviceKey is null
            ? ServiceIdentifier.FromServiceType(descriptor.ServiceType)
            : ServiceIdentifier.FromKeyedServiceType(descriptor.ServiceType, serviceKey);
        if (!TryGetDescriptorItem(identifier, out var item))
        {
            return null;
        }

        return CreateCallSite(
            descriptor,
            identifier,
            callSiteChain,
            item.GetSlot(descriptor),
            allowUnresolved,
            suppliedTypes,
            selectedIdentity);
    }

    private bool TryGetDescriptorItem(ServiceIdentifier serviceIdentifier, out ServiceDescriptorCacheItem descriptorItem)
    {
        if (_descriptorLookup.TryGetValue(serviceIdentifier, out descriptorItem!))
        {
            return true;
        }

        if (serviceIdentifier.IsKeyed &&
            _descriptorLookup.TryGetValue(
                ServiceIdentifier.FromKeyedServiceType(serviceIdentifier.ServiceType, KeyedService.AnyKey),
                out descriptorItem!))
        {
            return true;
        }

        descriptorItem = null!;
        return false;
    }

    private IEnumerableCallSite? CreateEnumerableCallSite(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
    {
        if (!ActivatorUtilities.TryGetGeneratedSequence(serviceIdentifier.ServiceType, serviceIdentifier.ServiceKey, out var sequenceDefinition))
        {
            return null;
        }

        if (sequenceDefinition.ElementType is not Type elementType)
        {
            throw new InvalidOperationException($"Generated sequence metadata for '{serviceIdentifier.ServiceType}' is missing its element identity.");
        }

        callSiteChain.CheckCircularDependency(serviceIdentifier);
        try
        {
            return CreateEnumerableCallSiteCore(serviceIdentifier, elementType, sequenceDefinition.Materialize, callSiteChain);
        }
        finally
        {
            callSiteChain.Remove(serviceIdentifier);
        }
    }

    private IEnumerableCallSite CreateEnumerableCallSiteCore(
        ServiceIdentifier serviceIdentifier,
        Type elementType,
        GeneratedEnumerableFactory materialize,
        CallSiteChain callSiteChain)
    {

        var sequence = serviceIdentifier.ServiceType;
        var descriptors = new List<ServiceDescriptor>();
        for (var index = 0; index < _descriptors.Length; index++)
        {
            var descriptor = _descriptors[index];
            if (descriptor.ServiceType != elementType)
            {
                continue;
            }

            if (!serviceIdentifier.IsKeyed)
            {
                if (!descriptor.IsKeyedService)
                {
                    descriptors.Add(descriptor);
                }
            }
            else if (descriptor.IsKeyedService &&
                (Equals(serviceIdentifier.ServiceKey, KeyedService.AnyKey) ||
                 Equals(descriptor.ServiceKey, serviceIdentifier.ServiceKey) ||
                 Equals(descriptor.ServiceKey, KeyedService.AnyKey)))
            {
                descriptors.Add(descriptor);
            }
        }

        var callSites = new ServiceCallSite[descriptors.Count];
        for (var index = 0; index < descriptors.Count; index++)
        {
            var descriptor = descriptors[index];
            var descriptorIdentifier = descriptor.IsKeyedService
                ? ServiceIdentifier.FromKeyedServiceType(elementType, descriptor.ServiceKey)
                : ServiceIdentifier.FromServiceType(elementType);
            var elementIdentifier = Equals(serviceIdentifier.ServiceKey, KeyedService.AnyKey)
                ? descriptorIdentifier
                : ServiceIdentifier.FromKeyedServiceType(elementType, serviceIdentifier.ServiceKey!);
            callSites[index] = CreateCallSite(
                descriptor,
                serviceIdentifier.IsKeyed ? elementIdentifier : descriptorIdentifier,
                callSiteChain,
                _descriptorLookup[descriptorIdentifier].GetSlot(descriptor));
        }

        return new IEnumerableCallSite(sequence, callSites, new ResultCache(ServiceLifetime.Transient, new ServiceCacheKey(serviceIdentifier, 0)), materialize);
    }

    private ServiceCallSite CreateCallSite(
        ServiceDescriptor descriptor,
        ServiceIdentifier identifier,
        CallSiteChain chain,
        int slot = 0,
        bool allowUnresolved = false,
        IReadOnlyList<Type?>? suppliedTypes = null,
        string? selectedIdentity = null)
    {
        _ = allowUnresolved;
        var cache = new ResultCache(descriptor.Lifetime, new ServiceCacheKey(identifier, slot));
        var instance = descriptor.IsKeyedService ? descriptor.KeyedImplementationInstance : descriptor.ImplementationInstance;
        if (instance is object)
        {
            return new ConstantCallSite(descriptor.ServiceType, instance);
        }

        if (descriptor.IsKeyedService && descriptor.KeyedImplementationFactory is Func<IServiceProvider, object?, object> keyedFactory)
        {
            return new FactoryCallSite(descriptor.ServiceType, provider => keyedFactory(provider, identifier.ServiceKey), cache);
        }

        if (descriptor.ImplementationFactory is Func<IServiceProvider, object> factory)
        {
            return new FactoryCallSite(descriptor.ServiceType, factory, cache);
        }

        var implementationType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationType
            : descriptor.ImplementationType;
        if (implementationType is not Type implementation ||
            !TryGetGeneratedActivation(descriptor, implementation, out var generatedActivation))
        {
            throw new InvalidOperationException("Generated activation metadata is required for implementation-type registrations.");
        }

        var candidates = new List<GeneratedActivationDescriptor>(1 + generatedActivation.Alternatives.Count)
        {
            generatedActivation,
        };
        for (var index = 0; index < generatedActivation.Alternatives.Count; index++)
        {
            candidates.Add(generatedActivation.Alternatives[index]);
        }

        string? shapeError = null;
        var sawShapeMatch = suppliedTypes is null;
        ConstructorCallSite? selectedCallSite = null;
        var selectedParameterCount = -1;
        foreach (var activation in candidates)
        {
            if (selectedIdentity is not null && activation.Identity != selectedIdentity)
            {
                continue;
            }

            int[]? suppliedIndexes = null;
            if (suppliedTypes is not null)
            {
                if (!TryMapSuppliedParameters(activation.Parameters, suppliedTypes, out var mappedIndexes, out var mapError))
                {
                    shapeError ??= mapError;
                    if (activation.IsPreferred)
                    {
                        break;
                    }

                    continue;
                }

                suppliedIndexes = mappedIndexes;
                sawShapeMatch = true;
            }

            var parameterCallSites = new ServiceCallSite?[activation.Parameters.Count];
            var valid = true;
            for (var index = 0; index < activation.Parameters.Count; index++)
            {
                if (suppliedTypes is not null && suppliedIndexes![index] >= 0)
                {
                    continue;
                }

                var parameter = activation.Parameters[index];
                var parameterKey = parameter.LookupMode == ServiceKeyLookupMode.InheritKey
                    ? identifier.ServiceKey
                    : parameter.ServiceKey;
                var parameterIdentifier = parameterKey is null
                    ? ServiceIdentifier.FromServiceType(parameter.ParameterType)
                    : ServiceIdentifier.FromKeyedServiceType(parameter.ParameterType, parameterKey);
                var parameterCallSite = GetCallSite(parameterIdentifier, chain);
                if (parameterCallSite is null && !parameter.HasDefaultValue)
                {
                    valid = false;
                    break;
                }

                parameterCallSites[index] = parameterCallSite;
            }

            if (valid)
            {
                var indexes = suppliedTypes is null
                    ? CreateUnsuppliedIndexes(activation.Parameters.Count)
                    : suppliedIndexes!;
                var candidateCallSite = new ConstructorCallSite(
                    descriptor.ServiceType,
                    activation.ImplementationType,
                    activation,
                    parameterCallSites,
                    indexes,
                    cache);
                if (selectedCallSite is not null)
                {
                    if (selectedParameterCount == activation.Parameters.Count)
                    {
                        throw new InvalidOperationException("Multiple generated constructors are applicable for the supplied activation shape.");
                    }

                    break;
                }

                selectedCallSite = candidateCallSite;
                selectedParameterCount = activation.Parameters.Count;
                if (activation.IsPreferred)
                {
                    break;
                }
            }
            else if (activation.IsPreferred)
            {
                break;
            }
        }

        if (selectedCallSite is not null)
        {
            return selectedCallSite;
        }

        if (shapeError is not null && suppliedTypes is not null && !sawShapeMatch)
        {
            throw new ArgumentException(shapeError, nameof(suppliedTypes));
        }

        ThrowHelper.ThrowNoService();
        throw new InvalidOperationException("No generated constructor is applicable.");
    }

    private static int[] CreateUnsuppliedIndexes(int count)
    {
        var indexes = new int[count];
        Array.Fill(indexes, -1);
        return indexes;
    }

    private bool TryGetGeneratedActivation(
        ServiceDescriptor descriptor,
        Type implementationType,
        out GeneratedActivationDescriptor activation)
    {
        if (descriptor is GeneratedServiceDescriptor generated)
        {
            activation = generated.Activation;
            return true;
        }

        return _generatedActivations.TryGetValue(
            (descriptor.ServiceType, implementationType, descriptor.ServiceKey),
            out activation!);
    }

    private bool TryMapSuppliedParameters(
        IReadOnlyList<GeneratedParameter> parameters,
        IReadOnlyList<Type?> suppliedTypes,
        out int[] suppliedIndexes,
        out string? error)
    {
        suppliedIndexes = CreateUnsuppliedIndexes(parameters.Count);
        error = null;
        if (suppliedTypes.Count > parameters.Count)
        {
            error = "Generated activation received more supplied arguments than any constructor metadata allows.";
            return false;
        }

        for (var suppliedIndex = 0; suppliedIndex < suppliedTypes.Count; suppliedIndex++)
        {
            var suppliedType = suppliedTypes[suppliedIndex];
            var match = -1;
            var matches = 0;
            for (var parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
            {
                if (suppliedIndexes[parameterIndex] >= 0)
                {
                    continue;
                }

                if (suppliedType is null || IsSuppliedTypeCompatible(suppliedType, parameters[parameterIndex].ParameterType))
                {
                    match = parameterIndex;
                    matches++;
                }
            }

            if (matches == 0)
            {
                error = suppliedType is null
                    ? "A null supplied argument has no remaining constructor parameter position."
                    : $"No generated constructor parameter has supplied type '{suppliedType}'.";
                return false;
            }

            if (matches > 1)
            {
                error = suppliedType is null
                    ? "A null supplied argument is ambiguous across constructor parameter positions."
                    : $"Supplied type '{suppliedType}' is ambiguous across constructor parameter positions.";
                return false;
            }

            suppliedIndexes[match] = suppliedIndex;
        }

        return true;
    }

    private bool IsSuppliedTypeCompatible(Type suppliedType, Type expectedType)
    {
        if (suppliedType == expectedType)
        {
            return true;
        }

        return _assignabilityLookup.TryGetValue(suppliedType, out var expectedTypes) && expectedTypes.Contains(expectedType);
    }

    private sealed class ServiceDescriptorCacheItem
    {
        private readonly Dictionary<ServiceDescriptor, int> _slotLookup = new();
        private readonly List<ServiceDescriptor> _descriptors = new();

        internal ServiceDescriptor? Last { get; private set; }

        internal ServiceDescriptorCacheItem Add(ServiceDescriptor descriptor)
        {
            Last = descriptor;
            _slotLookup[descriptor] = _slotLookup.Count;
            _descriptors.Add(descriptor);
            return this;
        }

        internal int GetSlot(ServiceDescriptor descriptor) => _slotLookup.TryGetValue(descriptor, out var slot) ? slot : 0;
    }
}
