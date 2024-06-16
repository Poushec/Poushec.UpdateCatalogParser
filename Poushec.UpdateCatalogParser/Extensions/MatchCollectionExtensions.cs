using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Poushec.UpdateCatalogParser.Extensions
{
    internal static class MatchCollectionExtensions
    {
        /// <summary>
        /// Dump implementation of Select() method for <see cref="MatchCollection"/>
        /// </summary>
        /// <param name="predicate">The select predicate</param>
        /// <returns>Collection of <typeparamref name="T"/> returned by the <paramref name="predicate"/></returns>
        public static IEnumerable<T> Select<T>(this MatchCollection matchCollection, Func<Match, T> predicate)
        {
            foreach (Match match in matchCollection)
            {
                yield return predicate(match);
            }
        }

        /// <summary>
        /// Dump implementation of Any() method for <see cref="MatchCollection"/>
        /// </summary>
        /// <returns>TRUE if a collection is not null and not empty, otherwise FALSE</returns>
        public static bool Any(this MatchCollection matchCollection) 
        {
            return matchCollection != null && matchCollection.Count > 0;
        }
    }
}
