﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static IModelDetailView FindModelDetailView(this DevExpress.ExpressApp.XafApplication application, System.Type objectType) 
            => (IModelDetailView) application.Model.Views[application.FindDetailViewId(objectType)];
    }
}