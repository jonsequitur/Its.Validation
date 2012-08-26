// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Its.Validation.Configuration
{
    public static class ValidationRuleExtensions
    {
        /// <summary>
        ///   Prevents exceptions of the specified type from being thrown from executions of the specified rule, instead passing them to a handler <see
        ///    cref="Action{TException}" /> .
        /// </summary>
        /// <typeparam name="TException"> The exception type to handle. </typeparam>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="getDetails"> A handler which will be called if an exception of <typeparamref name="TException" /> is thrown by the rule. </param>
        /// <returns> </returns>
        public static ValidationRule<TTarget> Handle<TException, TTarget>(this ValidationRule<TTarget> rule,
                                                                          Action<TException> getDetails = null)
            where TException : Exception
        {
            return Validate.That<TTarget>(t =>
            {
                try
                {
                    return rule.Check(t);
                }
                catch (TException ex)
                {
                    if (getDetails != null)
                    {
                        getDetails(ex);
                    }
                    return false;
                }
            }
                );
        }

        /// <summary>
        ///   Executes the specified rule and returns a <see cref="ValidationReport" /> .
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object to be validated. </typeparam>
        /// <param name="rule"> The rule to execute. </param>
        /// <param name="target"> The object to be validated. </param>
        /// <returns> </returns>
        public static ValidationReport Execute<TTarget>(this IValidationRule<TTarget> rule, TTarget target)
        {
            var plan = rule as ValidationPlan<TTarget>;
            if (plan != null)
            {
                return plan.Execute(target);
            }
            return new ValidationPlan<TTarget>(rule).Execute(target);
        }

        /// <summary>
        ///   Returns the direct preconditions of the rule.
        /// </summary>
        public static IEnumerable<IValidationRule<TTarget>> Preconditions<TTarget>(this IValidationRule<TTarget> rule)
        {
            var validationRule = rule as ValidationRule<TTarget>;
            if (validationRule == null || validationRule.preconditions == null)
            {
                yield break;
            }

            foreach (var precondition in validationRule.preconditions)
            {
                yield return precondition;
            }
        }
    }
}