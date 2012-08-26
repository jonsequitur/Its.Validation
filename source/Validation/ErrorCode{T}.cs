// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;

namespace Its.Validation
{
    /// <summary>
    ///   A validation error code to be associated with a validation rule.
    /// </summary>
    /// <typeparam name="T"> The <see cref="Type" /> of the error code. </typeparam>
    public class ErrorCode<T>
    {
        public ErrorCode(T code)
        {
            Value = code;
        }

        public T Value { get; set; }

        public override string ToString()
        {
            if (Value != null)
            {
                return Value.ToString();
            }

            return string.Empty;
        }
    }
}