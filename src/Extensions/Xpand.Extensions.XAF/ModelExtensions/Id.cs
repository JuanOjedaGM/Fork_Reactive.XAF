﻿using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static string Id(this IModelNode modelNode,string id=null) {
            if (id != null) {
                ((ModelNode)modelNode).Id = id;
            }
            return ((ModelNode)modelNode).Id;
        }
    }
}