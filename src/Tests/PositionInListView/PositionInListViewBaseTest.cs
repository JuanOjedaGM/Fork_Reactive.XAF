﻿using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
    public abstract class PositionInListViewBaseTest:BaseTest{
        protected static PositionInListViewModule PositionInListViewModuleModule(params ModuleBase[] modules){
            var positionInListViewModule = Platform.Win.NewApplication<PositionInListViewModule>().AddModule<PositionInListViewModule>(typeof(PIL));
            var xafApplication = positionInListViewModule.Application;
            xafApplication.MockPlatformListEditor();
            xafApplication.Modules.AddRange(modules);
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return positionInListViewModule;
        }
    }
}