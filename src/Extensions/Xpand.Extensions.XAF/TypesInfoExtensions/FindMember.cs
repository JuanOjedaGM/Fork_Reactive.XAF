﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.ExpressionExtensions;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
	public static partial class TypesInfoExtensions {
		public static IEnumerable<IMemberInfo> Members<TAttribute>(this IEnumerable<(TAttribute attribute, IMemberInfo memberInfo)> source) where TAttribute : Attribute
			=> source.Select(t => t.memberInfo);
		
		public static IEnumerable<(TAttribute attribute, IMemberInfo memberInfo)> AttributedMembers<TAttribute>(this IEnumerable<ITypeInfo> source)  where TAttribute:Attribute 
			=> source.SelectMany(info => info.AttributedMembers<TAttribute>());

		public static IEnumerable<ITypeInfo> Types<TAttribute>(this IEnumerable<(TAttribute attribute, ITypeInfo typeInfo)> source) where TAttribute : Attribute
			=> source.Select(t => t.typeInfo);
		
		public static IEnumerable<(TAttribute attribute, ITypeInfo typeInfo)> Attributed<TAttribute>(this IEnumerable<ITypeInfo> source)  where TAttribute:Attribute 
			=> source.SelectMany(info => info.Attributed<TAttribute>());
		public static IEnumerable<IMemberInfo> Members<TAttribute>(this IEnumerable<ITypeInfo> source)  where TAttribute:Attribute 
			=> source.SelectMany(info => info.AttributedMembers<TAttribute>()).Members();

		public static IEnumerable<(TAttribute attribute,IMemberInfo memberInfo)> AttributedMembers<TAttribute>(this ITypeInfo info) where TAttribute:Attribute 
			=> info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<TAttribute>().Select(attribute => (attribute, memberInfo)));
		
		public static IEnumerable<(TAttribute attribute,ITypeInfo typeInfo)> Attributed<TAttribute>(this ITypeInfo info) where TAttribute:Attribute 
			=> info.FindAttributes<TAttribute>().Select(attribute => (attribute,info));
		
        public static IMemberInfo FindMember<T>(this ITypeInfo typeInfo,Expression<Func<T, object>> memberName) 
            => typeInfo.FindMember(memberName.MemberExpressionName());
	}
}