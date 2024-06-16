using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Extensions
{
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Dump implementation of DistinctBy method.
        /// </summary>
        /// <param name="collection">Source collection</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="comparer">(Optional) <see cref="IEqualityComparer{T}"/> for <typeparamref name="TKey"/></param>
        /// <returns>Collection of distinct element by <typeparamref name="TKey"/></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> collection, 
            Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            } 

            if (comparer == null)
            {
                comparer = default;
            }

            var knownKeys = new HashSet<TKey>();

            foreach (var item in collection) 
            {
                if (knownKeys.Add(keySelector(item)))
                {
                    yield return item;
                }
            }
        }
    }
}
