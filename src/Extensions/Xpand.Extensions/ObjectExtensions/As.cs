﻿using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static T As<T>(this object obj) 
            => obj is T variable ? variable : default;
        public static T As<T>(this T obj,string typeName) {
            var type = obj?.GetType();
            return type == null ? default : type.IsInterface ? type.Implements(typeName) ? obj : default :
                type.InheritsFrom(typeName) ? obj : default;
        }
    }
}