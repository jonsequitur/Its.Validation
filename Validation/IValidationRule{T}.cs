// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;

namespace Its.Validation
{
    /// <summary>
    ///   Validates a target object against a specified condition.
    /// </summary>
    /// <typeparam name="TTarget"> The <see cref="Type" /> that the IValidationRule validates. </typeparam>
    public interface IValidationRule<in TTarget> : IValidationRule
    {
        /// <summary>
        ///   Determines whether the specified target is valid.
        /// </summary>
        /// <param name="target"> The object to be validated. </param>
        /// <returns> <c>true</c> if the specified target is valid; otherwise, <c>false</c> . </returns>
        bool Check(TTarget target);

        /// <summary>
        ///   Determines whether the specified target is valid.
        /// </summary>
        /// <param name="target"> The object to be validated. </param>
        /// <param name="scope"> The <see cref="ValidationScope" /> in which to perform the validation. </param>
        /// <returns> <c>true</c> if the specified target is valid; otherwise, <c>false</c> . </returns>
        bool Check(TTarget target, ValidationScope scope);
    }
}