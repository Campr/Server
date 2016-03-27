using System;
using System.Collections.Generic;
using System.Linq;

namespace Campr.Server.Lib.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static IEnumerable<T> Compact<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Where(e => e != null);
        }
    }
}