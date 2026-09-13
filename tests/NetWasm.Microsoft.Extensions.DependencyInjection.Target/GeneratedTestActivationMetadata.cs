using System;
using System.Collections.Generic;
using System.Linq;

namespace NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures
{

    public interface IClock
    {
        string Name { get; }
    }

    public sealed class Clock(string name = "") : IClock
    {
        public string Name { get; } = name;
    }

    public sealed class Consumer(IClock clock)
    {
        public IClock Clock { get; } = clock;
    }

    public sealed class ConsumerWithZone(IClock clock, string zone = "UTC")
    {
        public IClock Clock { get; } = clock;
        public string Zone { get; } = zone;
    }

    public sealed class MultiArgumentConsumer(IClock clock, string zone, int count)
    {
        public IClock Clock { get; } = clock;
        public string Zone { get; } = zone;
        public int Count { get; } = count;
    }

    public sealed class AmbiguousArguments(string first, string second)
    {
        public string First { get; } = first;
        public string Second { get; } = second;
    }

    public sealed class AmbiguousConstructors(IClock clock)
    {
        public IClock Clock { get; } = clock;
    }

    public sealed class AvailabilityConsumer
    {
        public AvailabilityConsumer()
        {
        }

        public AvailabilityConsumer(IClock clock)
        {
            UsedClock = true;
        }

        public bool UsedClock { get; }
    }

    public class BaseDependency
    {
    }

    public sealed class DerivedDependency : BaseDependency
    {
    }

    public sealed class InterfaceConsumer(IClock clock)
    {
        public IClock Clock { get; } = clock;
    }

    public sealed class BaseConsumer(BaseDependency dependency)
    {
        public BaseDependency Dependency { get; } = dependency;
    }

    public sealed class PreferredCatalogConsumer
    {
    }

    public sealed class LongestCatalogConsumer
    {
    }

    public sealed class DefaultConsumer(string value)
    {
        public string Value { get; } = value;
    }

    public interface IEmpty
    {
    }
}

namespace Microsoft.Extensions.DependencyInjection.Generated
{
    using TargetFixtures = global::NetWasm.Microsoft.Extensions.DependencyInjection.TargetFixtures;

    internal static partial class GeneratedActivationMetadataSource
    {
        static partial void Populate(
            List<GeneratedActivationDefinition> activations,
            List<GeneratedSequenceDefinition> sequences)
        {
            activations.Add(new GeneratedActivationDefinition(
                "clock",
                typeof(TargetFixtures.IClock),
                typeof(TargetFixtures.Clock),
                Array.Empty<Type>(),
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "consumer",
                typeof(TargetFixtures.Consumer),
                typeof(TargetFixtures.Consumer),
                new[] { typeof(TargetFixtures.IClock) },
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "consumer-with-zone",
                typeof(TargetFixtures.ConsumerWithZone),
                typeof(TargetFixtures.ConsumerWithZone),
                new[] { typeof(TargetFixtures.IClock), typeof(string) },
                new[]
                {
                new GeneratedActivationDefinition(
                    "consumer-with-zone-fallback",
                    typeof(TargetFixtures.ConsumerWithZone),
                    typeof(TargetFixtures.ConsumerWithZone),
                    new[] { typeof(TargetFixtures.IClock) },
                    Array.Empty<GeneratedActivationDefinition>(),
                    isPreferred: false,
                    Array.Empty<Type>()),
                },
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "multi-argument",
                typeof(TargetFixtures.MultiArgumentConsumer),
                typeof(TargetFixtures.MultiArgumentConsumer),
                new[] { typeof(TargetFixtures.IClock), typeof(string), typeof(int) },
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "ambiguous-arguments",
                typeof(TargetFixtures.AmbiguousArguments),
                typeof(TargetFixtures.AmbiguousArguments),
                new[] { typeof(string), typeof(string) },
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "ambiguous-constructors",
                typeof(TargetFixtures.AmbiguousConstructors),
                typeof(TargetFixtures.AmbiguousConstructors),
                new[] { typeof(TargetFixtures.IClock) },
                new[]
                {
                new GeneratedActivationDefinition(
                    "ambiguous-constructors-alt",
                    typeof(TargetFixtures.AmbiguousConstructors),
                    typeof(TargetFixtures.AmbiguousConstructors),
                    new[] { typeof(TargetFixtures.IClock) },
                    Array.Empty<GeneratedActivationDefinition>(),
                    isPreferred: false,
                    Array.Empty<Type>()),
                },
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "availability-factory",
                typeof(TargetFixtures.AvailabilityConsumer),
                typeof(TargetFixtures.AvailabilityConsumer),
                new[] { typeof(TargetFixtures.IClock) },
                new[]
                {
                new GeneratedActivationDefinition(
                    "availability-empty",
                    typeof(TargetFixtures.AvailabilityConsumer),
                    typeof(TargetFixtures.AvailabilityConsumer),
                    Array.Empty<Type>(),
                    Array.Empty<GeneratedActivationDefinition>(),
                    isPreferred: false,
                    Array.Empty<Type>()),
                },
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "derived",
                typeof(TargetFixtures.BaseDependency),
                typeof(TargetFixtures.DerivedDependency),
                Array.Empty<Type>(),
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "interface-consumer",
                typeof(TargetFixtures.InterfaceConsumer),
                typeof(TargetFixtures.InterfaceConsumer),
                new[] { typeof(TargetFixtures.IClock) },
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "base-consumer",
                typeof(TargetFixtures.BaseConsumer),
                typeof(TargetFixtures.BaseConsumer),
                new[] { typeof(TargetFixtures.BaseDependency) },
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));
            activations.Add(new GeneratedActivationDefinition(
                "default-consumer",
                typeof(TargetFixtures.DefaultConsumer),
                typeof(TargetFixtures.DefaultConsumer),
                new[] { typeof(string) },
                Array.Empty<GeneratedActivationDefinition>(),
                isPreferred: false,
                Array.Empty<Type>()));

            sequences.Add(new GeneratedSequenceDefinition(
                typeof(IEnumerable<TargetFixtures.IClock>),
                typeof(TargetFixtures.IClock),
                serviceKey: null,
                (IReadOnlyList<object?> values) => values.Cast<TargetFixtures.IClock>().ToArray()));
            sequences.Add(new GeneratedSequenceDefinition(
                typeof(IEnumerable<TargetFixtures.IClock>),
                typeof(TargetFixtures.IClock),
                serviceKey: "blue",
                (IReadOnlyList<object?> values) => values.Cast<TargetFixtures.IClock>().ToArray()));
            sequences.Add(new GeneratedSequenceDefinition(
                typeof(IEnumerable<TargetFixtures.IClock>),
                typeof(TargetFixtures.IClock),
                serviceKey: KeyedService.AnyKey,
                (IReadOnlyList<object?> values) => values.Cast<TargetFixtures.IClock>().ToArray()));
            sequences.Add(new GeneratedSequenceDefinition(
                typeof(IEnumerable<TargetFixtures.IEmpty>),
                typeof(TargetFixtures.IEmpty),
                serviceKey: null,
                (IReadOnlyList<object?> values) => values.Cast<TargetFixtures.IEmpty>().ToArray()));
            sequences.Add(new GeneratedSequenceDefinition(
                typeof(IEnumerable<int>),
                typeof(int),
                serviceKey: null,
                (IReadOnlyList<object?> values) => values.Cast<int>().ToArray()));
        }
    }
}
