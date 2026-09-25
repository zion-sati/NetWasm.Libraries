// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for getting keyed services from an <see cref="IServiceProvider" />.
    /// </summary>
    public static class ServiceProviderKeyedServiceExtensions
    {
        /// <summary>Gets a service of type <typeparamref name="T"/> with the specified key.</summary>
        public static T? GetKeyedService<T>(this IServiceProvider provider, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(provider);
            return (T?)provider.GetKeyedService(typeof(T), serviceKey);
        }

        /// <summary>Gets a service of the specified type and key.</summary>
        public static object? GetKeyedService(this IServiceProvider provider, Type serviceType, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(serviceType);
            if (provider is not IKeyedServiceProvider keyedProvider)
            {
                throw new InvalidOperationException("This service provider does not support keyed services.");
            }

            return keyedProvider.GetKeyedService(serviceType, serviceKey);
        }

        /// <summary>Gets a required service of type <typeparamref name="T"/> with the specified key.</summary>
        public static T GetRequiredKeyedService<T>(this IServiceProvider provider, object? serviceKey)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(provider);
            return (T)provider.GetRequiredKeyedService(typeof(T), serviceKey);
        }

        /// <summary>Gets a required service of the specified type and key.</summary>
        public static object GetRequiredKeyedService(this IServiceProvider provider, Type serviceType, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(serviceType);
            if (provider is not IKeyedServiceProvider keyedProvider)
            {
                throw new InvalidOperationException("This service provider does not support keyed services.");
            }

            return keyedProvider.GetRequiredKeyedService(serviceType, serviceKey);
        }

        /// <summary>Gets services of type <typeparamref name="T"/> with the specified key.</summary>
        public static IEnumerable<T> GetKeyedServices<T>(this IServiceProvider provider, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(provider);
            return provider.GetRequiredKeyedService<IEnumerable<T>>(serviceKey);
        }

        /// <summary>Gets services of the specified type and key.</summary>
        [RequiresDynamicCode("The native code for an IEnumerable<serviceType> might not be available at runtime.")]
        public static IEnumerable<object?> GetKeyedServices(this IServiceProvider provider, Type serviceType, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(serviceType);
            throw new NotSupportedException(
                "Dynamic keyed service enumeration by Type requires generated NetWasm activation metadata.");
        }
    }
}
