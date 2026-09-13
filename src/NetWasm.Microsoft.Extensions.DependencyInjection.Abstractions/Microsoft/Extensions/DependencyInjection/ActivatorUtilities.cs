// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Generated;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Creates instances through generated activation metadata.</summary>
public static class ActivatorUtilities
{
    public static object CreateInstance(IServiceProvider provider, Type instanceType, params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(instanceType);
        ArgumentNullException.ThrowIfNull(parameters);

        if (provider is not IGeneratedActivator activator)
        {
            throw new NotSupportedException("ActivatorUtilities requires a generated NetWasm service provider.");
        }

        return activator.Create(instanceType, parameters, GetSuppliedTypes(parameters), selectedIdentity: null)
            ?? throw new InvalidOperationException("Generated activation returned null.");
    }

    public static T CreateInstance<T>(IServiceProvider provider, params object?[] parameters)
    {
        return (T)CreateInstance(provider, typeof(T), parameters);
    }

    public static object GetServiceOrCreateInstance(IServiceProvider provider, Type instanceType)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(instanceType);
        return provider.GetService(instanceType) ?? CreateInstance(provider, instanceType);
    }

    public static T GetServiceOrCreateInstance<T>(IServiceProvider provider)
    {
        return (T)GetServiceOrCreateInstance(provider, typeof(T));
    }

    public static ObjectFactory CreateFactory(Type instanceType, Type[] argumentTypes)
    {
        ArgumentNullException.ThrowIfNull(instanceType);
        ArgumentNullException.ThrowIfNull(argumentTypes);
        for (var index = 0; index < argumentTypes.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(argumentTypes[index], nameof(argumentTypes));
        }

        var selection = GeneratedActivationComposition.CreateFactoryCreator(GeneratedActivationMetadataSource.Lookup).Create(instanceType, argumentTypes);
        return (provider, arguments) =>
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(arguments);
            if (arguments.Length != argumentTypes.Length)
            {
                throw new ArgumentException("The supplied argument count does not match the generated factory shape.", nameof(arguments));
            }

            if (provider is not IGeneratedActivator activator)
            {
                throw new NotSupportedException("ActivatorUtilities requires a generated NetWasm service provider.");
            }

            return activator.Create(instanceType, arguments, argumentTypes, selection.Identity)
                ?? throw new InvalidOperationException("Generated activation returned null.");
        };
    }

    public static ObjectFactory CreateFactory<T>(Type[] argumentTypes) =>
        CreateFactory(typeof(T), argumentTypes);

    private static Type?[] GetSuppliedTypes(object?[] parameters)
    {
        var types = new Type?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            types[index] = parameters[index]?.GetType();
        }

        return types;
    }

    internal static bool TryGetGeneratedSequence(
        Type sequenceType,
        object? serviceKey,
        out GeneratedSequenceDefinition sequence)
    {
        ArgumentNullException.ThrowIfNull(sequenceType);
        var generatedMetadata = GeneratedActivationMetadataSource.Lookup;
        if (generatedMetadata.Sequences.TryGetValue(new GeneratedSequenceKey(sequenceType, serviceKey), out sequence!))
        {
            return true;
        }

        return serviceKey is not null &&
            generatedMetadata.Sequences.TryGetValue(new GeneratedSequenceKey(sequenceType, KeyedService.AnyKey), out sequence!);
    }
}
