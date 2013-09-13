using System.Linq;

namespace Wording.Core
{
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class RandomValue
    {
        private static readonly Random Random = new Random();

        public static T GetRandomElement<T>(this IEnumerable<T> list)
        {
            if (list.Count() == 0)
            {
                return default(T);
            }

            return list.ElementAt(Random.Next(list.Count()));
        }
    }
}
