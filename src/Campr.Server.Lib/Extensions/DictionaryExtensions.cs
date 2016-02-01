using System.Collections.Generic;

namespace Campr.Server.Lib.Extensions
{
    public static class DictionaryExtensions
    {

        public static T2 TryGetValue<T1, T2>(this IReadOnlyDictionary<T1, T2> dict, T1 key, T2 defaultValue = default(T2))
        {
            T2 result;
            return dict.TryGetValue(key, out result)
                ? result 
                : defaultValue;
        }

        public static T2 TryGetValue<T1, T2>(this IDictionary<T1, T2> dict, T1 key, T2 defaultValue = default(T2))
        {
            T2 result;
            return dict.TryGetValue(key, out result)
                ? result
                : defaultValue;
        }

        // Code duplication is needed here to make the compiler happy.
        public static T2 TryGetValue<T1, T2>(this Dictionary<T1, T2> dict, T1 key, T2 defaultValue = default(T2))
        {
            T2 result;
            return dict.TryGetValue(key, out result)
                ? result
                : defaultValue;
        }
    }
}
