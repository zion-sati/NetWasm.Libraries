// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Ported from dotnet/runtime System.Collections.Immutable
// ImmutableArrayExtensions.cs at commit 811225a482702af7ecc35d817966bc70b88a3a23.
// Reflection, serialization, and threading-only APIs are not part of this port.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Linq
{
    /// <summary>LINQ operations specialized for <see cref="ImmutableArray{T}"/>.</summary>
    public static class ImmutableArrayExtensions
    {
        public static IEnumerable<TResult> Select<T, TResult>(
            this ImmutableArray<T> immutableArray,
            Func<T, TResult> selector)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return Enumerable.Select(immutableArray.array!, selector);
        }

        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this ImmutableArray<TSource> immutableArray,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            Requires.NotNull(collectionSelector, nameof(collectionSelector));
            Requires.NotNull(resultSelector, nameof(resultSelector));
            return SelectManyIterator(immutableArray.array!, collectionSelector, resultSelector);
        }

        public static IEnumerable<T> Where<T>(
            this ImmutableArray<T> immutableArray,
            Func<T, bool> predicate)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return Enumerable.Where(immutableArray.array!, predicate);
        }

        public static bool Any<T>(this ImmutableArray<T> immutableArray) =>
            immutableArray.Length > 0;

        public static bool Any<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            Requires.NotNull(predicate, nameof(predicate));
            foreach (var value in immutableArray.array!)
            {
                if (predicate(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool All<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            Requires.NotNull(predicate, nameof(predicate));
            foreach (var value in immutableArray.array!)
            {
                if (!predicate(value))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SequenceEqual<TDerived, TBase>(
            this ImmutableArray<TBase> immutableArray,
            ImmutableArray<TDerived> items,
            IEqualityComparer<TBase>? comparer = null)
            where TDerived : TBase
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            items.ThrowNullRefIfNotInitialized();
            if (ReferenceEquals(immutableArray.array, items.array))
            {
                return true;
            }

            if (immutableArray.Length != items.Length)
            {
                return false;
            }

            comparer ??= EqualityComparer<TBase>.Default;
            for (var index = 0; index < immutableArray.Length; index++)
            {
                if (!comparer.Equals(immutableArray.array![index], items.array![index]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SequenceEqual<TDerived, TBase>(
            this ImmutableArray<TBase> immutableArray,
            IEnumerable<TDerived> items,
            IEqualityComparer<TBase>? comparer = null)
            where TDerived : TBase
        {
            Requires.NotNull(items, nameof(items));
            immutableArray.ThrowNullRefIfNotInitialized();
            comparer ??= EqualityComparer<TBase>.Default;

            var index = 0;
            foreach (var item in items)
            {
                if (index == immutableArray.Length ||
                    !comparer.Equals(immutableArray.array![index], item))
                {
                    return false;
                }

                index++;
            }

            return index == immutableArray.Length;
        }

        public static bool SequenceEqual<TDerived, TBase>(
            this ImmutableArray<TBase> immutableArray,
            ImmutableArray<TDerived> items,
            Func<TBase, TBase, bool> predicate)
            where TDerived : TBase
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            items.ThrowNullRefIfNotInitialized();
            if (ReferenceEquals(immutableArray.array, items.array))
            {
                return true;
            }

            if (immutableArray.Length != items.Length)
            {
                return false;
            }

            for (var index = 0; index < immutableArray.Length; index++)
            {
                if (!predicate(immutableArray.array![index], items.array![index]))
                {
                    return false;
                }
            }

            return true;
        }

        public static T? Aggregate<T>(this ImmutableArray<T> immutableArray, Func<T, T, T> func)
        {
            Requires.NotNull(func, nameof(func));
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.Length == 0)
            {
                return default;
            }

            var result = immutableArray[0];
            for (var index = 1; index < immutableArray.Length; index++)
            {
                result = func(result, immutableArray[index]);
            }

            return result;
        }

        public static TAccumulate Aggregate<TAccumulate, T>(
            this ImmutableArray<T> immutableArray,
            TAccumulate seed,
            Func<TAccumulate, T, TAccumulate> func)
        {
            Requires.NotNull(func, nameof(func));
            immutableArray.ThrowNullRefIfNotInitialized();
            var result = seed;
            foreach (var value in immutableArray.array!)
            {
                result = func(result, value);
            }

            return result;
        }

        public static TResult Aggregate<TAccumulate, TResult, T>(
            this ImmutableArray<T> immutableArray,
            TAccumulate seed,
            Func<TAccumulate, T, TAccumulate> func,
            Func<TAccumulate, TResult> resultSelector)
        {
            Requires.NotNull(resultSelector, nameof(resultSelector));
            return resultSelector(Aggregate(immutableArray, seed, func));
        }

        public static T ElementAt<T>(this ImmutableArray<T> immutableArray, int index) =>
            immutableArray[index];

        public static T? ElementAtOrDefault<T>(this ImmutableArray<T> immutableArray, int index) =>
            index < 0 || index >= immutableArray.Length ? default : immutableArray[index];

        public static T First<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            foreach (var value in immutableArray.array!)
            {
                if (predicate(value))
                {
                    return value;
                }
            }

            ThrowHelper.ThrowNoElementsException();
            return default!;
        }

        public static T First<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.Length == 0)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return immutableArray[0];
        }

        public static T? FirstOrDefault<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return immutableArray.Length == 0 ? default : immutableArray[0];
        }

        public static T? FirstOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            foreach (var value in immutableArray.array!)
            {
                if (predicate(value))
                {
                    return value;
                }
            }

            return default;
        }

        public static T Last<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.Length == 0)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return immutableArray[immutableArray.Length - 1];
        }

        public static T Last<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            for (var index = immutableArray.Length - 1; index >= 0; index--)
            {
                if (predicate(immutableArray[index]))
                {
                    return immutableArray[index];
                }
            }

            ThrowHelper.ThrowNoElementsException();
            return default!;
        }

        public static T? LastOrDefault<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return immutableArray.Length == 0 ? default : immutableArray[immutableArray.Length - 1];
        }

        public static T? LastOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            for (var index = immutableArray.Length - 1; index >= 0; index--)
            {
                if (predicate(immutableArray[index]))
                {
                    return immutableArray[index];
                }
            }

            return default;
        }

        public static T Single<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.Length != 1)
            {
                if (immutableArray.Length == 0)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                ThrowHelper.ThrowMoreThanOneElementException();
            }

            return immutableArray[0];
        }

        public static T Single<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            var found = false;
            var result = default(T);
            foreach (var value in immutableArray.array!)
            {
                if (!predicate(value))
                {
                    continue;
                }

                if (found)
                {
                    ThrowHelper.ThrowMoreThanOneMatchException();
                }

                found = true;
                result = value;
            }

            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return result!;
        }

        public static T? SingleOrDefault<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.Length == 0)
            {
                return default;
            }

            if (immutableArray.Length > 1)
            {
                ThrowHelper.ThrowMoreThanOneElementException();
            }

            return immutableArray[0];
        }

        public static T? SingleOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            var found = false;
            var result = default(T);
            foreach (var value in immutableArray.array!)
            {
                if (!predicate(value))
                {
                    continue;
                }

                if (found)
                {
                    ThrowHelper.ThrowMoreThanOneMatchException();
                }

                found = true;
                result = value;
            }

            return result;
        }

        public static Dictionary<TKey, T> ToDictionary<TKey, T>(
            this ImmutableArray<T> immutableArray,
            Func<T, TKey> keySelector)
            where TKey : notnull =>
            ToDictionary(immutableArray, keySelector, EqualityComparer<TKey>.Default);

        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(
            this ImmutableArray<T> immutableArray,
            Func<T, TKey> keySelector,
            Func<T, TElement> elementSelector)
            where TKey : notnull =>
            ToDictionary(immutableArray, keySelector, elementSelector, EqualityComparer<TKey>.Default);

        public static Dictionary<TKey, T> ToDictionary<TKey, T>(
            this ImmutableArray<T> immutableArray,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            Requires.NotNull(keySelector, nameof(keySelector));
            immutableArray.ThrowNullRefIfNotInitialized();
            var result = new Dictionary<TKey, T>(immutableArray.Length, comparer);
            foreach (var value in immutableArray.array!)
            {
                result.Add(keySelector(value), value);
            }

            return result;
        }

        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(
            this ImmutableArray<T> immutableArray,
            Func<T, TKey> keySelector,
            Func<T, TElement> elementSelector,
            IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            Requires.NotNull(keySelector, nameof(keySelector));
            Requires.NotNull(elementSelector, nameof(elementSelector));
            immutableArray.ThrowNullRefIfNotInitialized();
            var result = new Dictionary<TKey, TElement>(immutableArray.Length, comparer);
            foreach (var value in immutableArray.array!)
            {
                result.Add(keySelector(value), elementSelector(value));
            }

            return result;
        }

        public static T[] ToArray<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.array!.Length == 0)
            {
                return ImmutableArray<T>.Empty.array!;
            }

            return (T[])immutableArray.array.Clone();
        }

        public static T First<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            if (builder.Count == 0)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return builder[0];
        }

        public static T? FirstOrDefault<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            return builder.Count == 0 ? default : builder[0];
        }

        public static T Last<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            if (builder.Count == 0)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return builder[builder.Count - 1];
        }

        public static T? LastOrDefault<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            return builder.Count == 0 ? default : builder[builder.Count - 1];
        }

        public static bool Any<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            return builder.Count > 0;
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(
            TSource[] array,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (var item in array)
            {
                foreach (var result in collectionSelector(item))
                {
                    yield return resultSelector(item, result);
                }
            }
        }
    }
}
