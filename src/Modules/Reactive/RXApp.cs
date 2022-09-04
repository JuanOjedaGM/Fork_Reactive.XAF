﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Harmony;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ModuleExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Security;

namespace Xpand.XAF.Modules.Reactive{
	public static class RxApp{
        
        static readonly Subject<ApplicationModulesManager> ApplicationModulesManagerSubject=new();
        static readonly Subject<(List<Controller> __result, Type baseType, IModelApplication modelApplication, View view)> WhenControllerCreatedSubject=new();
        

        static RxApp() => PatchXafApplication();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateModuleManager(ApplicationModulesManager __result) => ApplicationModulesManagerSubject.OnNext(__result);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void CreateControllers(Type baseType,
            IModelApplication modelApplication,
            View view,List<Controller> __result) {
            WhenControllerCreatedSubject.OnNext(( __result, baseType, modelApplication,
                view));
        }

        public static IObservable<List<Controller>> ToControllers(
            this IObservable<(XafApplication Application, List<Controller> controllers, Type baseType, IModelApplication
                modelApplication, View view)> source)
            => source.Select(t => t.controllers);


        public static IObservable<(List<Controller> controllers, Type baseType, IModelApplication modelApplication, View view)> ControllerCreated 
            => WhenControllerCreatedSubject.AsObservable();

        public static IObservable<T> When<T>(
            this IObservable<(List<Controller> controllers, Type baseType, IModelApplication modelApplication, View view)> source) 
            => source.SelectMany(t => t.controllers.Where(controller => controller is T)).Cast<T>();


        private static void PatchXafApplication(){
            var xafApplicationType = typeof(XafApplication);
            new HarmonyMethod(GetMethodInfo(nameof(CreateModuleManager)))
                .Finalize(xafApplicationType.Method(nameof(CreateModuleManager)),true);
            new HarmonyMethod(typeof(XafApplicationRxExtensions), nameof(XafApplicationRxExtensions.Exit))
                .PreFix(xafApplicationType.Method(nameof(XafApplication.Exit)),true);

            if (DesignerOnlyCalculator.IsRunTime) {
                new HarmonyMethod(GetMethodInfo(nameof(CreateControllers)))
                    .Finalize(typeof(ControllersManager).Method(nameof(ControllersManager.CreateControllers),new []{typeof(Type),typeof(IModelApplication),typeof(View)}),true);
            }
            
        }

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(RxApp).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).First(info => info.Name == methodName);

        internal static IObservable<Unit> NonPersistentChangesEnabledAttribute(this XafApplication application) 
            => application.WhenObjectViewCreated().Where(view => view.ObjectTypeInfo.FindAttributes<NonPersistentChangesEnabledAttribute>().Any())
                .Do(view => view.ObjectSpace.NonPersistentChangesEnabled = true)
                .ToUnit();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.Attributes()
                .Merge(manager.AddNonSecuredTypes())
                .Merge(manager.MergedExtraEmbeddedModels())
                .Merge(manager.ConnectObjectString())
                .Merge(manager.WhenApplication(application =>application.WhenNonPersistentPropertyCollectionSource()
                    .Merge(application.PatchAuthentication())
                    .Merge(application.PatchObjectSpaceProvider())
                    .Merge(application.NonPersistentChangesEnabledAttribute())
                    .Merge(application.PopulateAdditionalObjectSpaces())
                    .Merge(application.ReloadWhenChanged())
                    .Merge(application.ShowInstanceDetailView())
                    // .Merge(application.ShowPersistentObjectsInNonPersistentView())
                .Merge(manager.SetupPropertyEditorParentView())));



        private static IObservable<Unit> ShowInstanceDetailView(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => application.WhenViewOnFrame().ShowInstanceDetailView(application.TypesInfo
                    .PersistentTypes.Attributed<ShowInstanceDetailViewAttribute>().Types().Select(info => info.Type).ToArray())).ToUnit();
            
        private static IObservable<Unit> ReloadWhenChanged(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => {
                var membersToReload = application.Model.BOModel.TypeInfos().AttributedMembers<ReloadWhenChangeAttribute>().ToArray();
                return application.WhenFrameViewChanged()
                    .WhenFrame(membersToReload.SelectMany(t =>t.attribute.AttributeTypes(t.memberInfo)).Distinct().ToArray())
                    .SelectUntilViewClosed(frame => membersToReload.WhenFrame(frame)
                        .SelectMany(ts => application.WhenProviderCommitted(ts.Key).To(ts).SelectMany()).ObserveOnContext()
                        .Do(t => {
                            if (t.attribute.ObjectPropertyChangeMethodName != null) {
                                frame.View.CurrentObject.CallMethod(t.attribute.ObjectPropertyChangeMethodName, t.info.Name);      
                            }
                            else if (frame.View == null) {
                                
                            }
                            else if (typeof(IReloadWhenChange).IsAssignableFrom(frame.View.ObjectTypeInfo.Type)) {
                                frame.View.CurrentObject.As<IReloadWhenChange>().WhenPropertyChanged(t.info.Name);
                            }
                            else if (frame.View.ObjectTypeInfo.Type.Implements("DevExpress.Xpo.IXPReceiveOnChangedFromArbitrarySource")) {
                                (frame.View.ObjectTypeInfo.Type.Method("FireChanged") ?? frame.View.ObjectTypeInfo.Type.Method("DevExpress.Xpo.IXPReceiveOnChangedFromArbitrarySource.FireChanged"))
                                    .Call(frame.View.CurrentObject, t.info.Name);
                            }
                        })
                        .ToUnit());
            });

        private static IEnumerable<Type> AttributeTypes(this ReloadWhenChangeAttribute attribute, IMemberInfo memberInfo) 
            => attribute.Types.Any()?attribute.Types: memberInfo.Owner.Type.RealType().YieldItem();

        private static IObservable<IGrouping<Type, (Type key, IMemberInfo info, ReloadWhenChangeAttribute attribute)>> WhenFrame(
            this IEnumerable<(ReloadWhenChangeAttribute attribute, IMemberInfo info)> membersToReload, Frame frame) 
            => membersToReload.Where(t => t.info.Owner == frame.View.ObjectTypeInfo||t.attribute.Types.Any(type => type.IsAssignableFrom(frame.View.ObjectTypeInfo.Type)))
                .SelectMany(t => t.attribute.AttributeTypes(t.info).Select(type => (key:type,t.info,t.attribute)))
                .GroupBy(t => t.key).ToNowObservable();

        private static IObservable<Unit> MergedExtraEmbeddedModels(this ApplicationModulesManager manager) 
            => manager.WhereApplication().ToObservable()
                .SelectMany(application => application.WhenCreateCustomUserModelDifferenceStore()
                    .Do(_ => {
                        var models = _.application.Modules.SelectMany(m => m.EmbeddedModels().Select(tuple => (id: $"{m.Name},{tuple.id}", tuple.model)))
                            .Where(tuple => {
                                var pattern = ConfigurationManager.AppSettings["EmbeddedModels"]??@"(\.MDO)|(\.RDO)";
                                return !Regex.IsMatch(tuple.id, pattern, RegexOptions.Singleline);
                            })
                            .ToArray();
                        foreach (var model in models){
                            _.e.AddExtraDiffStore(model.id, new StringModelStore(model.model));
                        }

                        if (models.Any()){
                            _.e.AddExtraDiffStore("After Setup", new ModelStoreBase.EmptyModelStore());
                        }
                    })).ToUnit();

        private static IObservable<Unit> SetupPropertyEditorParentView(this ApplicationModulesManager applicationModulesManager) 
            => applicationModulesManager.WhereApplication().ToObservable().SelectMany(_ => _.SetupPropertyEditorParentView());

        
        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        internal static IObservable<ApplicationModulesManager> ApplicationModulesManager => ApplicationModulesManagerSubject.AsObservable();
    }

}