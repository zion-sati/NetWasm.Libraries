using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Composition helpers emitted by generated activation source.</summary>
public static class GeneratedActivationRegistrationExtensions
{
    public static IServiceCollection AddGenerated(
        this IServiceCollection services,
        GeneratedActivationDescriptor activation,
        ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(activation);
        services.Add(new GeneratedServiceDescriptor(
            activation.ServiceType,
            activation.ServiceKey,
            activation.ImplementationType,
            lifetime,
            activation));
        return services;
    }

    public static IServiceCollection AddGenerated(
        this IServiceCollection services,
        object serviceKey,
        GeneratedActivationDescriptor activation,
        ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(activation);
        if (activation.ServiceKey is not null && !Equals(activation.ServiceKey, serviceKey))
        {
            throw new ArgumentException("The generated activation key does not match the registration key.", nameof(serviceKey));
        }

        services.Add(new GeneratedServiceDescriptor(
            activation.ServiceType,
            serviceKey,
            activation.ImplementationType,
            lifetime,
            activation));
        return services;
    }

    public static IServiceCollection AddGeneratedSingleton(this IServiceCollection services, GeneratedActivationDescriptor activation) =>
        services.AddGenerated(activation, ServiceLifetime.Singleton);

    public static IServiceCollection AddGeneratedScoped(this IServiceCollection services, GeneratedActivationDescriptor activation) =>
        services.AddGenerated(activation, ServiceLifetime.Scoped);

    public static IServiceCollection AddGeneratedTransient(this IServiceCollection services, GeneratedActivationDescriptor activation) =>
        services.AddGenerated(activation, ServiceLifetime.Transient);

    public static IServiceCollection AddGeneratedKeyedSingleton(
        this IServiceCollection services,
        object serviceKey,
        GeneratedActivationDescriptor activation) =>
        services.AddGenerated(serviceKey, activation, ServiceLifetime.Singleton);

    public static IServiceCollection AddGeneratedKeyedScoped(
        this IServiceCollection services,
        object serviceKey,
        GeneratedActivationDescriptor activation) =>
        services.AddGenerated(serviceKey, activation, ServiceLifetime.Scoped);

    public static IServiceCollection AddGeneratedKeyedTransient(
        this IServiceCollection services,
        object serviceKey,
        GeneratedActivationDescriptor activation) =>
        services.AddGenerated(serviceKey, activation, ServiceLifetime.Transient);
}
