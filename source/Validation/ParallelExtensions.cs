// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Validation.Configuration;

namespace Its.Validation
{
    /// <summary>
    ///   Supports evaluating validation rules in parallel.
    /// </summary>
    public static class ParallelExtensions
    {
        /// <summary>
        ///   Checks each item in a sequence against a validation rule in parallel.
        /// </summary>
        /// <typeparam name="T"> The type of the items to be validated. </typeparam>
        /// <param name="source"> The items to be validated. </param>
        /// <param name="rule"> The validation rule to check. </param>
        /// <returns> True if all items in the sequence are valid; otherwise, false. </returns>
        public static bool Parallel<T>(this IEnumerable<T> source, IValidationRule<T> rule)
        {
            return source.Parallel(rule.Check);
        }

        /// <summary>
        ///   Checks each item in a sequence against a validation rule in parallel.
        /// </summary>
        /// <typeparam name="T"> The type of the items to be validated. </typeparam>
        /// <param name="source"> The items to be validated. </param>
        /// <param name="validate"> A function that performs a validation against each item. </param>
        /// <returns> True if all items in the sequence are valid; otherwise, false. </returns>
        public static bool Parallel<T>(this IEnumerable<T> source, Func<T, bool> validate)
        {
            return source
                .Select(s => new { Target = s, ParentScope = ValidationScope.Current })
                .ToArray() // force evaluation so that ValidationScope will be referenced from the current thread before parallellization
                //.Do(validation => ValidationScope.Current = validation.ParentScope)
                .AsParallel()
                .Select(s =>
                {
                    using (new ValidationScope(s.ParentScope))
                    {
                        return validate(s.Target);
                    }
                })
                .AsEnumerable()
                .Every(s => s);
        }
    }
}