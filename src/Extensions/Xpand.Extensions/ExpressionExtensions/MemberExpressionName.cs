﻿using System;
using System.Linq.Expressions;

namespace Xpand.Extensions.ExpressionExtensions{
    public static partial class ExpressionExtensions{
        public static string MemberExpressionName<TObject>(this Expression<Func<TObject, object>> memberName) =>
            memberName.Body is UnaryExpression unaryExpression
                ? ((MemberExpression) unaryExpression.Operand).Member.Name
                : ((MemberExpression) memberName.Body).Member.Name;
        public static string MemberExpressionName<TObject,TMemeberValue>(this Expression<Func<TObject, TMemeberValue>> memberName) =>
            memberName.Body is UnaryExpression unaryExpression
                ? ((MemberExpression) unaryExpression.Operand).Member.Name
                : ((MemberExpression) memberName.Body).Member.Name;
    }
}