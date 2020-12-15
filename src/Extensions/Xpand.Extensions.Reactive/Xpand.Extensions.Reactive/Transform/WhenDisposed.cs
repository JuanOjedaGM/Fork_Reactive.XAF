﻿using System;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<(TDisposable component, EventArgs args)> Disposed<TDisposable>(this IObservable<TDisposable> source) where TDisposable:IComponent => source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposed += h, h => item.Disposed -= h,ImmediateScheduler.Instance))
                .Select(pattern => pattern)
                .TransformPattern<EventArgs,TDisposable>();

        public static IObservable<(TDisposable component, EventArgs args)> WhenDisposed<TDisposable>(this TDisposable source) where TDisposable : IComponent =>
            source.ReturnObservable().Disposed();
    }
}