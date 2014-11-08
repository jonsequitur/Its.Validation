// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Its.Validation
{
    /// <summary>
    /// Describes the dot-notation path through an object graph to a specific member.
    /// </summary>
    internal class MemberPath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberPath"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public MemberPath(string value)
        {
            Value = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the member path value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Determines a member path given an Expression.
        /// </summary>
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

            return nameParts.Count > 0
                       ? nameParts.Aggregate((left, right) => left + right).TrimStart('.')
                       : string.Empty;
        }
    }
}