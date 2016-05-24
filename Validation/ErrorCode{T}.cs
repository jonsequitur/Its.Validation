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
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorCode&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        public ErrorCode(T code)
        {
            Value = code;
        }

        /// <summary>
        /// Gets or sets the error code value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() => Value?.ToString() ?? string.Empty;
    }
}