﻿using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static TNode GetParent<TNode>(this IModelNode modelNode) where TNode : class{
            if (modelNode is TNode node)
                return node;
            var parent = modelNode.Parent;
            while (!(parent is TNode)) {
                parent = parent.Parent;
                if (parent == null)
                    break;
            }
            return (TNode) parent;
        }

    }
}
