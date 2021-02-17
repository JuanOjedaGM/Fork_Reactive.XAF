﻿using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static BoolList Clone(this BoolList boolList){
            var list = new BoolList();
            foreach (var key in boolList.GetKeys()){
                list.SetItemValue(key,boolList[key]);
            }

            return list;
        }

        public static bool DoTheExecute(this ActionBase actionBase,bool force=false) {
            BoolList active = null;
            BoolList enable = null;
            if (force&& (!actionBase.Active||!actionBase.Enabled)){
                active = actionBase.Active.Clone();
                enable = actionBase.Enabled.Clone();
                if (!actionBase.Active){
                    actionBase.Active.Clear();
                }
                if (!actionBase.Enabled){
                    actionBase.Enabled.Clear();
                }
            }
            if (!actionBase.Active||!actionBase.Enabled)
                return false;
            var simpleAction = actionBase as SimpleAction;
            simpleAction?.DoExecute();
            var singleChoiceAction = actionBase as SingleChoiceAction;
            singleChoiceAction?.DoExecute(singleChoiceAction.SelectedItem??singleChoiceAction.Items.FirstOrDefault());

            if (actionBase is PopupWindowShowAction popupWindowShowAction) {
                if (popupWindowShowAction.Application.GetPlatform() == Platform.Win) {
                    var helper = (IDisposable)Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.PopupWindowShowActionHelper"),popupWindowShowAction);
                    var view = actionBase.View();
                    void OnClosing(object sender, EventArgs args) {
                        helper.Dispose();
                        view.Closing -= OnClosing;
                    }
                    view.Closing += OnClosing;
                    helper.CallMethod("ShowPopupWindow");
                }
                else {
                    popupWindowShowAction?.DoExecute((Window)popupWindowShowAction.Controller.Frame);
                }
            }

            var parametrizedAction = actionBase as ParametrizedAction;
            parametrizedAction?.DoExecute(parametrizedAction.Value);
            if (active != null){
                foreach (var key in active.GetKeys()){
                    active.SetItemValue(key,active[key]);
                }
            }
            if (enable != null){
                foreach (var key in enable.GetKeys()){
                    enable.SetItemValue(key,enable[key]);
                }
            }
            return true;
        }

    }
}