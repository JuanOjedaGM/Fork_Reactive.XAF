﻿using System;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static ParametrizedAction AsParametrizedAction(this ActionBase action) => action as ParametrizedAction;
    }
}