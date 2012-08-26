// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Linq;

namespace Its.Validation
{
    /// <summary>
    ///   Validates a target against a specified condition.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        ///   Allows access to rule results.
        /// </summary>
        /// <typeparam name="T"> The type of the result </typeparam>
        /// <returns> An instance of the specified type <typeparamref name="T" /> , or null if it has not been defined on this rule or in this rules <see
        ///    cref="ValidationScope" /> . </returns>
        T Result<T>();
    }
}