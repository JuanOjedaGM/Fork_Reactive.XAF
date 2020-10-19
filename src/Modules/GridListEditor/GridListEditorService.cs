﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.GridListEditor{
    public static class GridListEditorService{

        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) => 
	        manager.WhenApplication(application => application.RememberTopRow().ToUnit());

        internal static IObservable<TSource> TraceGridListEditor<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, GridListEditorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        public static IObservable<string> RememberTopRow(this XafApplication application) =>
	        application.WhenViewOnFrame(viewType:ViewType.ListView)
		        .SelectMany(frame => ModelRules(application, frame).To(frame))
		        .Select(frame => frame.View).Cast<ListView>()
		        .SelectMany(view => {
			        var gridListEditor = ( view.Editor);
			        var gridView = gridListEditor.GetPropertyValue("GridView");
			        var topRowIndex = gridView.GetPropertyValue("TopRowIndex");
			        return view.CollectionSource.WhenCollectionReloaded()
				        .Do(_ => gridView.SetPropertyValue("TopRowIndex",topRowIndex))
				        .To($"TopRowIndex: {topRowIndex}, View: {view}");
		        })
		        .TraceGridListEditor();

        private static IObservable<IModelGridListEditorTopRow> ModelRules(XafApplication application, Frame frame) =>
	        application.ReactiveModulesModel().GridListEditor().Rules().OfType<IModelGridListEditorTopRow>()
		        .Where(row =>row.ListView == frame.View.Model &&((ListView) frame.View).Editor.GetType().InheritsFrom("DevExpress.ExpressApp.Win.Editors.GridListEditor") )
		        .TraceGridListEditor(row => row.ListView.Id);
    }
}