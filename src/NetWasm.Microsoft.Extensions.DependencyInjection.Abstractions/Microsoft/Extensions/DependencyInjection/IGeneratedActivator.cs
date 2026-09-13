using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Internal bridge implemented by a generated-activation provider.</summary>
internal interface IGeneratedActivator
{
    object? Create(Type instanceType, object?[] parameters, Type?[] parameterTypes, string? selectedIdentity);
}
