// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Its.Validation
{
    internal class MemberPath
    {
        public MemberPath(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; private set; }

        internal static string FromExpression(LambdaExpression expression)
        {
            var nameParts = new Stack<string>();
            var part = expression.Body;

            while (part != null)
            {
                if (part.NodeType == ExpressionType.Call || part.NodeType == ExpressionType.ArrayIndex)
                {
                    throw new NotSupportedException("MemberPath must consist only of member access sub-expressions");
                }

                if (part.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpressionPart = (MemberExpression) part;
                    nameParts.Push("." + memberExpressionPart.Member.Name);
                    part = memberExpressionPart.Expression;
                }
                else
                {
                    break;
                }
            }

            if (nameParts.Count > 0)
            {
                return nameParts.Aggregate((left, right) => left + right).TrimStart('.');
            }

            return string.Empty;
        }
    }
}