using System.Linq;

namespace Wording.Core
{
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using System;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class RandomValue
    {
        private static Random random = new Random();

        public static T GetRandomElement<T>(this IEnumerable<T> list)
        {
            // If there are no elements in the collection, return the default value of T
            if (list.Count() == 0)
                return default(T);

            return list.ElementAt(random.Next(list.Count()));
        }
    }
}
