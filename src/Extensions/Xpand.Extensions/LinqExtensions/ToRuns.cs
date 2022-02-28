﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<List<T>> ToRuns<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector){
            using (var enumerator = source.GetEnumerator()){
                if (!enumerator.MoveNext())
                    yield break;

                var currentSet = new List<T>();
                var lastKey = keySelector(enumerator.Current);
                currentSet.Add(enumerator.Current);

                while (enumerator.MoveNext()){
                    var newKey = keySelector(enumerator.Current);
                    if (!Equals(newKey, lastKey)){
                        yield return currentSet;
                        lastKey = newKey;
                        currentSet = new List<T>();
                    }

                    currentSet.Add(enumerator.Current);
                }

                yield return currentSet;
            }
        }

    }
}