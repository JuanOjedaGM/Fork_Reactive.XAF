﻿using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Utils.Svg;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ShowMessageExtensions {
        private static readonly ISubject<MessageOptions> MessageSubject = Subject.Synchronize(new Subject<MessageOptions>());

        internal static IObservable<Unit> ShowMessages(this XafApplication application)
            => Observable.If(() => application.GetPlatform()==Platform.Win,MessageSubject.BufferWhen(
                    application.WhenSynchronizationContext().Select(context => context),source => source.ObserveLatestOnContext())
	            .Do(options => application.ShowViewStrategy.ShowMessage(options))
	            .ToUnit());
        
        public static IObservable<T> ShowXafMessage<T>(this IObservable<T> source, InformationType informationType = InformationType.Info, int displayInterval = MessageDisplayInterval,
            InformationPosition position = InformationPosition.Left, [CallerMemberName] string memberName = "")
            => source.Do(obj => obj.ShowMessage( informationType,position, displayInterval, memberName, $"{obj}"));
        
        public static IObservable<T> ShowXafMessage<T>(this IObservable<T> source, Func<T,string> messageSelector,InformationType informationType=InformationType.Info,int displayInterval=MessageDisplayInterval,InformationPosition position=InformationPosition.Left, [CallerMemberName] string memberName = "")
            => source.Do(obj => obj.ShowMessage( informationType,position, displayInterval, "", messageSelector(obj)));

        public static IObservable<T> ShowXafMessage<T>(this IObservable<T> source, Func<T,int, string> messageSelector,InformationType informationType = InformationType.Info, int displayInterval = MessageDisplayInterval, InformationPosition position = InformationPosition.Left)
            => source.Select((arg1, i) => {
                arg1.ShowMessage(informationType,position, displayInterval, "", messageSelector(arg1, i));
                return arg1;
            });

        public static IObservable<T> ShowXafMessage<T>(this IObservable<T> source, Func<T, string> messageSelector, Func<T, InformationType> infoSelector,
            Func<T,int> displayInterval=null, InformationPosition position = InformationPosition.Left,Action<T> onOk = null, Action<T> onCancel = null,Func<T,SvgImage> imageSelector=null,[CallerMemberName] string memberName = "")
            => source.Do(obj => obj.ShowMessage(infoSelector.Invoke(obj),position, displayInterval?.Invoke(obj)??MessageDisplayInterval, memberName, messageSelector?.Invoke(obj)??$"{obj}",onOk:onOk,onCancel:onCancel,imageSelector:imageSelector));
        
        public static IObservable<T> ShowXafSuccessMessage<T>(this IObservable<T> source, Func<T, string> messageSelector,
            Func<T,int> displayInterval=null, InformationPosition position = InformationPosition.Left,Action<T> onOk = null, Action<T> onCancel = null,Func<T,SvgImage> imageSelector=null,[CallerMemberName] string memberName = "")
            => source.ShowXafMessage(messageSelector,_ => InformationType.Success,displayInterval,position,onOk,onCancel,imageSelector,memberName);

        public static IObservable<Unit> ShowXafInfoMessage<T>(this Func<IObservable<T>> showSignal,Func<T, string> messageSelector,Func<T,SvgImage> imageSelector=null,[CallerMemberName]string caller="") 
            => Unit.Default.Observe().ShowXafInfoMessage(_ => showSignal(),messageSelector,imageSelector,caller ).ToUnit();
        
        public static IObservable<T> ShowXafInfoMessage<T,T2>(this IObservable<T> source, 
            Func<T, IObservable<T2>> showSignal, Func<T2, string> messageSelector,Func<T2,SvgImage> imageSelector=null, [CallerMemberName] string caller = "") 
            => source.MergeIgnored(arg => showSignal(arg).ShowXafInfoMessage(messageSelector,imageSelector:imageSelector,memberName:caller));

        public static IObservable<T> ShowXafInfoMessage<T>(this IObservable<T> source,
            Func<T, string> messageSelector = null, Func<T, int> displayInterval = null,
            InformationPosition position = InformationPosition.Left, Action<T> onOk = null, Action<T> onCancel = null,Func<T,SvgImage> imageSelector=null,
            [CallerMemberName] string memberName = "")
            => source.ShowXafMessage(messageSelector,_ => InformationType.Info,displayInterval,position,onOk,onCancel,imageSelector,memberName);
        
        public static IObservable<T> ShowXafWarningMessage<T>(this IObservable<T> source, Func<T, string> messageSelector,
            Func<T,int> displayInterval=null, InformationPosition position = InformationPosition.Left,Action<T> onOk = null, Action<T> onCancel = null,Func<T,SvgImage> imageSelector=null,[CallerMemberName] string memberName = "")
            => source.ShowXafMessage(messageSelector,_ => InformationType.Warning,displayInterval,position,onOk,onCancel,imageSelector,memberName);
        
        public static IObservable<T> ShowXafErrorMessage<T>(this IObservable<T> source, Func<T, string> messageSelector, 
            Func<T,int> displayInterval=null, InformationPosition position = InformationPosition.Left,Action<T> onOk = null, Action<T> onCancel = null,[CallerMemberName] string memberName = "")
            => source.ShowXafMessage(messageSelector,_ => InformationType.Error,displayInterval,position,onOk,onCancel,null,memberName);

        public const int MessageDisplayInterval = 5000;

        public static IObservable<XafApplication> ShowXafMessage(this IObservable<XafApplication> source,
            InformationType informationType = InformationType.Info, int displayInterval = MessageDisplayInterval,
            InformationPosition position = InformationPosition.Left, [CallerMemberName] string memberName = "")
            => source.Do(application => application.ShowMessage(informationType,position, displayInterval, memberName, null));

        public static IObservable<Frame> ShowXafMessage(this IObservable<Frame> source,InformationType informationType = InformationType.Info, int displayInterval = MessageDisplayInterval,
            InformationPosition position = InformationPosition.Left, [CallerMemberName] string memberName = "")
            => source.Do(frame => frame.ShowMessage(informationType,position, displayInterval, memberName,  $"{frame}"));

        private static void ShowMessage<T>(this T obj, InformationType informationType,InformationPosition position, int displayInterval, string memberName, string message,
            WinMessageType winMessageType = WinMessageType.Alert, Action<T> onOk = null, Action<T> onCancel = null,Func<T,SvgImage> imageSelector=null) {
            if (message != null) {
                MessageSubject.OnNext(new MessageOptions() {
                    Duration = displayInterval, Message = $"{memberName}{Environment.NewLine}{message}",
                    Type = informationType, Win = { Type = winMessageType,ImageOptions = obj.ImageOptions( imageSelector)},Web = { Position = position},
                    OkDelegate = () => onOk?.Invoke(obj),CancelDelegate = () => onCancel?.Invoke(obj)
                });
            }
        }

        private static object ImageOptions<T>(this T obj, Func<T, SvgImage> imageSelector) {
            if (imageSelector != null) {
                var imageOptions = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.Utils.ImageOptions").CreateInstance();
                imageOptions.SetPropertyValue("SvgImage", imageSelector(obj));
                imageOptions.SetPropertyValue("SvgImageSize", new Size(50, 50));
                return imageOptions;
            }
            return null;
        }
    }
}