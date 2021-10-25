using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ObjectTemplate{
	public interface IModelReactiveModulesObjectTemplate : IModelReactiveModule{
		IModelObjectTemplate ObjectTemplate{ get; }
	}

	[DomainLogic(typeof(IModelObjectTemplate))]
	public static class ModelObjectTemplateLogic{
		
		public static IObservable<IModelObjectTemplate> ObjectTemplate(this IObservable<IModelReactiveModules> source) 
			=> source.Select(modules => modules.ObjectTemplate());

		public static IModelObjectTemplate ObjectTemplate(this IModelReactiveModules reactiveModules) 
			=> ((IModelReactiveModulesObjectTemplate) reactiveModules).ObjectTemplate;
		internal static IModelObjectTemplate ModelObjectTemplate(this IModelApplication modelApplication) 
			=> modelApplication.ToReactiveModule<IModelReactiveModulesObjectTemplate>().ObjectTemplate;

	}

	public interface IModelObjectTemplate : IModelNode{


	}
	
}