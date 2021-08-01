﻿using System;
using System.Reactive.Linq;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TValue> To<TSource,TValue>(this IObservable<TSource> source,TValue value) => source.Select(_ => value);

        public static IObservable<T> To<T>(this IObservable<object> source,bool newInstance=false) => source.Select(_ =>!newInstance? default:(T)typeof(T).CreateInstance()).WhenNotDefault();

        public static IObservable<TResult> To<TResult,T>(this IObservable<T> source) => source.Select(_ => default(TResult)).WhenNotDefault();
    }
}