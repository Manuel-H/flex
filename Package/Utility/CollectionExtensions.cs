using System;
using System.Collections.Generic;

namespace com.Dunkingmachine.Utility
{
    public static class CollectionExtensions
    {
        public static T Initialize<T, T2>(this T array, Func<int, T2> initalizer) where T : IList<T2>
        {
            for (var i = 0; i < array.Count; i++)
            {
                array[i] = initalizer(i);
            }

            return array;
        }
    }
}