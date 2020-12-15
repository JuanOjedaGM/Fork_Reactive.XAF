﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
#if !NETCOREAPP3_1
using DevExpress.ExpressApp.Chart.Web;
using DevExpress.ExpressApp.Dashboards.Web;
using DevExpress.ExpressApp.FileAttachments.Web;
using DevExpress.ExpressApp.HtmlPropertyEditor.Web;
using DevExpress.ExpressApp.Maps.Web;
using DevExpress.ExpressApp.Notifications.Web;
using DevExpress.ExpressApp.PivotChart.Web;
using DevExpress.ExpressApp.PivotGrid.Web;
using DevExpress.ExpressApp.ReportsV2.Web;
using DevExpress.ExpressApp.ScriptRecorder.Web;
using DevExpress.ExpressApp.TreeListEditors.Web;
using DevExpress.ExpressApp.Validation.Web;
using DevExpress.ExpressApp.Web.SystemModule;
using TestApplication.Web.LookupCascade;
using Xpand.XAF.Modules.LookupCascade;
#endif

using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.Reactive.Extensions;
using DevExpress.ExpressApp.Updating;
using Xpand.TestsLib.Common.BO;


namespace TestApplication.Web{
    public class WebModule:AgnosticModule{
        public WebModule(){
#if !NETCOREAPP3_1

	        #region XAF Module
	        RequiredModuleTypes.Add(typeof(ScriptRecorderAspNetModule));
	        RequiredModuleTypes.Add(typeof(ChartAspNetModule));
	        RequiredModuleTypes.Add(typeof(ChartAspNetModule));
	        RequiredModuleTypes.Add(typeof(DashboardsAspNetModule));
	        RequiredModuleTypes.Add(typeof(FileAttachmentsAspNetModule));
	        RequiredModuleTypes.Add(typeof(HtmlPropertyEditorAspNetModule));
	        RequiredModuleTypes.Add(typeof(MapsAspNetModule));
	        RequiredModuleTypes.Add(typeof(NotificationsAspNetModule));
	        RequiredModuleTypes.Add(typeof(PivotChartAspNetModule));
	        RequiredModuleTypes.Add(typeof(PivotGridAspNetModule));
	        RequiredModuleTypes.Add(typeof(ReportsAspNetModuleV2));
	        RequiredModuleTypes.Add(typeof(ScriptRecorderAspNetModule));
	        RequiredModuleTypes.Add(typeof(TreeListEditorsAspNetModule));
	        RequiredModuleTypes.Add(typeof(ValidationAspNetModule));
	        RequiredModuleTypes.Add(typeof(SystemAspNetModule));
	        #endregion
	        RequiredModuleTypes.Add(typeof(LookupCascadeModule));
#endif
        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) => 
	        base.GetModuleUpdaters(objectSpace, versionFromDB).Add(new OrderModuleUpdater(objectSpace, versionFromDB));


        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            

#if !NETCOREAPP3_1
            if (!Debugger.IsAttached){

                // moduleManager.Extend(Enum.GetValues(typeof(PredefinedMap)).OfType<PredefinedMap>().Where(map =>map!=PredefinedMap.None&& map.Platform()==Platform.Web));

            }
	        moduleManager.LookupCascade().ToUnit()
		        .Subscribe(this);
#endif
        }

    }

}