﻿using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n) {
            using var it = source.GetEnumerator();
            bool hasRemainingItems;
            var cache = new Queue<T>(n + 1);

            do{
                var b = hasRemainingItems = it.MoveNext();
                if (b){
                    cache.Enqueue(it.Current);
                    if (cache.Count > n)
                        yield return cache.Dequeue();
                }
            } while (hasRemainingItems);
        }
    }
}