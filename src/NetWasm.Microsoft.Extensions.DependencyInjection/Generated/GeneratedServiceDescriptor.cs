using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Service descriptor carrying generated activation metadata without a runtime metadata lookup.</summary>
internal sealed class GeneratedServiceDescriptor : ServiceDescriptor
{
    public GeneratedServiceDescriptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime,
        GeneratedActivationDescriptor activation)
        : this(serviceType, serviceKey: null, implementationType, lifetime, activation)
    {
    }

    public GeneratedServiceDescriptor(
        Type serviceType,
        object? serviceKey,
        Type implementationType,
        ServiceLifetime lifetime,
        GeneratedActivationDescriptor activation)
        : base(serviceType, serviceKey, implementationType, lifetime)
    {
        ArgumentNullException.ThrowIfNull(activation);
        if (activation.ServiceType != serviceType || activation.ImplementationType != implementationType)
        {
            throw new ArgumentException("Generated activation identity does not match its service descriptor.", nameof(activation));
        }

        Activation = activation;
    }

    public GeneratedActivationDescriptor Activation { get; }

    public override string ToString()
    {
        var text = $"ServiceType: {Activation.DiagnosticServiceType} Lifetime: {Lifetime} ";
        if (IsKeyedService)
        {
            text += $"ServiceKey: {ServiceKey} ";
        }

        return text + $"ImplementationType: {Activation.DiagnosticImplementationType}";
    }
}
