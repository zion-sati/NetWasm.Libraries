// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for getting services from an <see cref="IServiceProvider" />.
    /// </summary>
    public static class ServiceProviderServiceExtensions
    {
        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
        public static T? GetService<T>(this IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return (T?)provider.GetService(typeof(T));
        }

        /// <summary>
        /// Get service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>A service object of type <paramref name="serviceType"/>.</returns>
        /// <exception cref="System.InvalidOperationException">There is no service of type <paramref name="serviceType"/>.</exception>
        public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(serviceType);

            if (provider is ISupportRequiredService requiredServiceSupportingProvider)
            {
                return requiredServiceSupportingProvider.GetRequiredService(serviceType);
            }

            object? service = provider.GetService(serviceType);
            if (service == null)
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, serviceType));
            }

            return service;
        }

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
        public static T GetRequiredService<T>(this IServiceProvider provider) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(provider);

            return (T)provider.GetRequiredService(typeof(T));
        }

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the services from.</param>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public static IEnumerable<T> GetServices<T>(this IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return provider.GetRequiredService<IEnumerable<T>>();
        }

        /// <summary>
        /// Get an enumeration of services of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the services from.</param>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>An enumeration of services of type <paramref name="serviceType"/>.</returns>
        [RequiresDynamicCode("The native code for an IEnumerable<serviceType> might not be available at runtime.")]
        public static IEnumerable<object?> GetServices(this IServiceProvider provider, Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(serviceType);

            throw new NotSupportedException(
                "Dynamic service enumeration by Type requires generated NetWasm activation metadata.");
        }

        // NetWasm 0.4.1 exposed keyed methods on this type. Keep their static
        // signatures for existing callers, but only the upstream declaring type
        // owns extension methods so source lookup remains unambiguous.
        public static T? GetKeyedService<T>(IServiceProvider provider, object? serviceKey) =>
            ServiceProviderKeyedServiceExtensions.GetKeyedService<T>(provider, serviceKey);

        public static object? GetKeyedService(IServiceProvider provider, Type serviceType, object? serviceKey) =>
            ServiceProviderKeyedServiceExtensions.GetKeyedService(provider, serviceType, serviceKey);

        public static T GetRequiredKeyedService<T>(IServiceProvider provider, object? serviceKey)
            where T : notnull =>
            ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService<T>(provider, serviceKey);

        public static object GetRequiredKeyedService(IServiceProvider provider, Type serviceType, object? serviceKey) =>
            ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService(provider, serviceType, serviceKey);

        public static IEnumerable<T> GetKeyedServices<T>(IServiceProvider provider, object? serviceKey) =>
            ServiceProviderKeyedServiceExtensions.GetKeyedServices<T>(provider, serviceKey);

        [RequiresDynamicCode("The native code for an IEnumerable<serviceType> might not be available at runtime.")]
        public static IEnumerable<object?> GetKeyedServices(IServiceProvider provider, Type serviceType, object? serviceKey) =>
            ServiceProviderKeyedServiceExtensions.GetKeyedServices(provider, serviceType, serviceKey);

        /// <summary>
        /// Creates a new <see cref="IServiceScope"/> that can be used to resolve scoped services.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to create the scope from.</param>
        /// <returns>A <see cref="IServiceScope"/> that can be used to resolve scoped services.</returns>
        public static IServiceScope CreateScope(this IServiceProvider provider)
        {
            return provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        /// <summary>
        /// Creates a new <see cref="AsyncServiceScope"/> that can be used to resolve scoped services.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to create the scope from.</param>
        /// <returns>An <see cref="AsyncServiceScope"/> that can be used to resolve scoped services.</returns>
        public static AsyncServiceScope CreateAsyncScope(this IServiceProvider provider)
        {
            return new AsyncServiceScope(provider.CreateScope());
        }

        /// <summary>
        /// Creates a new <see cref="AsyncServiceScope"/> that can be used to resolve scoped services.
        /// </summary>
        /// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory"/> to create the scope from.</param>
        /// <returns>An <see cref="AsyncServiceScope"/> that can be used to resolve scoped services.</returns>
        public static AsyncServiceScope CreateAsyncScope(this IServiceScopeFactory serviceScopeFactory)
        {
            return new AsyncServiceScope(serviceScopeFactory.CreateScope());
        }
    }
}
