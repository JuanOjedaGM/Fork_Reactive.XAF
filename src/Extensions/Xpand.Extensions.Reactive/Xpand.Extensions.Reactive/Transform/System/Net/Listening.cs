﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform.System.Net{
    public static class SystemNetTransform{
        
        public static IObservable<IPEndPoint> Listening(this IEnumerable<IPEndPoint> source,bool repeatWhenOffine=true,TimeSpan? timeSpan=null){
            timeSpan ??= TimeSpan.FromMilliseconds(500);
            return source.ToObservable(ImmediateScheduler.Instance)
                .SelectMany(endPoint => {
                    var inUsed = Observable.While(() => !endPoint.Listening(), Observable.Empty<IPEndPoint>().Delay(timeSpan.Value))
                        .Concat(endPoint.ReturnObservable());
                    var notInUse = Observable.While(endPoint.Listening, Observable.Empty<IPEndPoint>().Delay(timeSpan.Value))
                        .Concat(endPoint.ReturnObservable());
                    return repeatWhenOffine ? inUsed.RepeatWhen(_ => _.SelectMany(o => notInUse)) : inUsed;
                });
        }

        public static bool Listening(this IPEndPoint endPoint) => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Contains(endPoint);
    }
}