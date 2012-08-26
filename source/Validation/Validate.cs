// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Its.Validation.Configuration
{
    /// <summary>
    ///   Provides methods for creating and configuring validation rules.
    /// </summary>
    public static class Validate
    {
        /// <summary>
        ///   Stores a parameter value at rule evaluation time and designates a name for that parameter so that it can be accessed later.
        /// </summary>
        /// <typeparam name="TParam"> The type of the param. </typeparam>
        /// <param name="value"> The value of the parameter. </param>
        /// <param name="parameterName"> The name by which the parameter will be referenced, for example in validation message format strings. </param>
        /// <returns> The parameter. </returns>
        /// <remarks>
        ///   This operator is used to store the value of a parameter within a validation rule so that it can be evaluated later, generally for the purpose of including it in a validation error message. This does not affect the evaluation of the rule itself.
        /// </remarks>
        public static TParam As<TParam>(this TParam value, string parameterName)
        {
            if (ValidationScope.Current != null)
            {
                ValidationScope.Current.AddParameter(parameterName, value);
            }

            return value;
        }

        /// <summary>
        ///   Stores a converted parameter value at rule evaluation time and designates a name for that parameter so that it can be accessed later.
        /// </summary>
        /// <typeparam name="TEvaluated"> The type of the evaluated. </typeparam>
        /// <typeparam name="TParam"> The type of the param. </typeparam>
        /// <param name="value"> The value of the parameter. </param>
        /// <param name="parameterName"> The name by which the parameter will be referenced, for example in validation message format strings. </param>
        /// <param name="convert"> Converts the parameter value to be stored for later evaluation. </param>
        /// <returns> The parameter. </returns>
        /// <remarks>
        ///   This operator is used to store the value of a parameter within a validation rule so that it can be evaluated later, generally for the purpose of including it in a validation error message. This does not affect the evaluation of the rule itself.
        /// </remarks>
        public static TEvaluated As<TEvaluated, TParam>(this TEvaluated value, string parameterName,
                                                        Func<TEvaluated, TParam> convert)
        {
            if (ValidationScope.Current != null)
            {
                ValidationScope.Current.AddParameter(parameterName, convert(value));
            }

            return value;
        }

        /// <summary>
        ///   Checks every item in the list against the specified <paramref name="condition" /> .
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="list"> The list of targets. </param>
        /// <param name="condition"> The condition to validate. </param>
        /// <returns> True if <paramref name="condition" /> evaluates to true for every target in the list; otherwise, false. </returns>
        /// <remarks>
        ///   This operator is provided to supplement the <see cref="Enumerable.All{T}" /> operator for validations. Whereas <see
        ///    cref="Enumerable.All{T}" /> will stop iterating upon the first false result, <see cref="Every{TTarget}" /> will evaluate each item in the list once, to provide a complete validation report.
        /// </remarks>
        public static bool Every<TTarget>(this IEnumerable<TTarget> list, Func<TTarget, bool> condition)
        {
            var result = true;
            foreach (var target in list)
            {
                if (!condition(target))
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        ///   Validates every item in a sequence.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="list"> A sequence of items of type <typeparamref name="TTarget" /> to be validated. </param>
        /// <param name="rule"> The rule to validate each item against. </param>
        /// <remarks>
        ///   This method is provided as an alternative to <see cref="Enumerable.All{TSource}" /> , which will stop evaluating upon finding the first false result. <see
        ///    cref="Every{TTarget}(System.Collections.Generic.IEnumerable{TTarget},System.Func{TTarget,bool})" /> will force evaluation of the entire sequence and return all results.
        /// </remarks>
        /// <returns> </returns>
        public static bool Every<TTarget>(this IEnumerable<TTarget> list, IValidationRule<TTarget> rule)
        {
            return list.Every(rule.Check);
        }

        /// <summary>
        ///   Associates a member name with a validation rule.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="memberPath"> A member to which the validation rule is associated. </param>
        /// <returns> A clone of the source validation rule. </returns>
        /// <remarks>
        ///   This need not match the actual object model. It is a mechanism for grouping rules and does not affect the execution or evaluation of the rules in any way.
        /// </remarks>
        public static ValidationRule<TTarget> ForMember<TTarget>(this ValidationRule<TTarget> rule, string memberPath)
        {
            return rule.With(new MemberPath(memberPath));
        }

        /// <summary>
        ///   Associates a member name with a validation rule.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <typeparam name="TReturn"> The type of the member. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="expression"> An expression identifying the member to which the validation rule is associated. </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> ForMember<TTarget, TReturn>(this ValidationRule<TTarget> rule,
                                                                          Expression<Func<TTarget, TReturn>> expression)
        {
            return rule.ForMember(MemberPath.FromExpression(expression));
        }

        /// <summary>
        ///   Builds a <see cref="ValidationRule{TTarget}" /> .
        /// </summary>
        /// <typeparam name="TTarget"> The type of the validation target against which the rule can be checked. </typeparam>
        /// <param name="condition"> The condition. </param>
        /// <returns> A <see cref="ValidationRule{TTarget}" /> for the specified condition. </returns>
        public static ValidationRule<TTarget> That<TTarget>(Func<TTarget, bool> condition)
        {
            return new ValidationRule<TTarget>(condition);
        }

        /// <summary>
        ///   Declares a precondition, which must evaluate to true before the <paramref name="rule" /> will be evaluated.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="preconditions"> The preconditions. </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> When<TTarget>(this ValidationRule<TTarget> rule,
                                                            params IValidationRule<TTarget>[] preconditions)
        {
            // TODO: (WhenAll) naming? is WhenAny possible/desirable? 
            // TODO: (WhenAll) what about Or?
            if (preconditions == null)
            {
                throw new ArgumentNullException("preconditions");
            }

            var newRule = rule.Clone();
            foreach (var precondition in preconditions)
            {
                newRule.AddPrecondition(precondition);
            }

            return newRule;
        }

        /// <summary>
        ///   Declares a precondition, which must evaluate to true before the <paramref name="rule" /> will be evaluated.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="precondition"> The precondition. </param>
        /// <returns> A clone of the source validation rule. </returns>
        /// <remarks>
        ///   When the precondition fails, it will not generate a <see cref="FailedEvaluation" /> .
        /// </remarks>
        public static ValidationRule<TTarget> When<TTarget>(this ValidationRule<TTarget> rule,
                                                            Func<TTarget, bool> precondition)
        {
            if (precondition == null)
            {
                throw new ArgumentNullException("precondition");
            }

            var preconditionRule = new ValidationRule<TTarget>(precondition) { CreatesEvaluationsAsInternal = true };

            return rule.When(preconditionRule);
        }

        /// <summary>
        ///   Associates an error code with a validation rule.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the target. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="errorCode"> The error code. </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> WithErrorCode<TTarget>(this ValidationRule<TTarget> rule,
                                                                     string errorCode)
        {
            return rule.With(new ErrorCode<string>(errorCode));
        }

        /// <summary>
        ///   Assigns a message to a validation rule, which may be used for both failures and successes.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object being validated. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="message"> The message. </param>
        /// <returns> A clone of validation rule. </returns>
        public static ValidationRule<TTarget> WithMessage<TTarget>(this ValidationRule<TTarget> rule, string message)
        {
            return rule.With(new MessageTemplate(message));
        }

        /// <summary>
        ///   Assigns a message to the validation rule that will be displayed when the rule fails.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object being validated. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="message"> The message. </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> WithErrorMessage<TTarget>(
            this ValidationRule<TTarget> rule,
            string message)
        {
            return rule.With(new FailureMessageTemplate(message));
        }

        /// <summary>
        ///   Provides an error message to be shown when the rule fails.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object being validated. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="buildMessage"> A function that returns a message appropriate to the validation failure. </param>
        /// <returns> </returns>
        public static ValidationRule<TTarget> WithErrorMessage<TTarget>(
            this ValidationRule<TTarget> rule,
            Func<FailedEvaluation, string> buildMessage)
        {
            var clone = rule.Clone();
            clone.Set<FailureMessageTemplate>(new FailureMessageTemplate(buildMessage));
            return clone;
        }

        /// <summary>
        ///   Assigns a message to the validation rule that will be displayed when the rule passes.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object being validated. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="message"> The message. </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> WithSuccessMessage<TTarget>(this ValidationRule<TTarget> rule,
                                                                          string message)
        {
            return rule.With(new SuccessMessageTemplate(message));
        }

        /// <summary>
        ///   Assigns a message to the validation rule that will be displayed when the rule passes.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object being validated. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="buildMessage"> A function that returns a message for a <see cref="SuccessfulEvaluation" /> . </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> WithSuccessMessage<TTarget>(this ValidationRule<TTarget> rule,
                                                                          Func<SuccessfulEvaluation, string> buildMessage)
        {
            var clone = rule.Clone();
            clone.Set<SuccessMessageTemplate>(new SuccessMessageTemplate(buildMessage));
            return clone;
        }

        /// <summary>
        ///   Adds a dynamic property to a rule, which will be copied to any <see cref="FailedEvaluation" /> generated by the rule.
        /// </summary>
        /// <typeparam name="TTarget"> The <see cref="Type" /> of the target. </typeparam>
        /// <typeparam name="T"> The <see cref="Type" /> of the dynamic property. </typeparam>
        /// <param name="rule"> The rule. </param>
        /// <param name="value"> The value. </param>
        /// <returns> A clone of the source validation rule. </returns>
        public static ValidationRule<TTarget> With<TTarget, T>(this ValidationRule<TTarget> rule, T value)
        {
            var clone = rule.Clone();
            clone.Set(value);
            return clone;
        }

        internal static AsyncValidationRule<TTarget> Async<TTarget>(
            Func<TTarget, Task<TTarget>> setup,
            Func<TTarget, bool> validate)
        {
            return new AsyncValidationRule<TTarget>(setup, validate);
        }
    }
}