// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Its.Validation
{
    /// <summary>
    ///   Validates a target object against a specified condition.
    /// </summary>
    /// <typeparam name="TTarget"> The <see cref="Type" /> that the IValidationRule validates. </typeparam>
    [DebuggerDisplay("Rule: {Message}")]
    public class ValidationRule<TTarget> : IValidationRule<TTarget>
    {
        private readonly Func<TTarget, bool> condition;
        protected internal IList<IValidationRule<TTarget>> preconditions;
        protected Dictionary<Type, object> extensions;
        protected internal bool CreatesEvaluationsAsInternal;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationRule&lt;TTarget&gt;" /> class.
        /// </summary>
        internal ValidationRule()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationRule{TTarget}" /> class.
        /// </summary>
        /// <param name="condition"> The condition. </param>
        public ValidationRule(Func<TTarget, bool> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException("condition");
            }
            this.condition = condition;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationRule{TTarget}" /> class.
        /// </summary>
        /// <param name="fromRule"> The rule upon which the new rule wil be based. </param>
        /// <remarks>
        ///   This is used to clone a rule.
        /// </remarks>
        protected ValidationRule(ValidationRule<TTarget> fromRule)
        {
            condition = fromRule.condition;
            CreatesEvaluationsAsInternal = fromRule.CreatesEvaluationsAsInternal;

            if (fromRule.preconditions != null)
            {
                preconditions = new List<IValidationRule<TTarget>>(fromRule.preconditions);
            }

            if (fromRule.extensions != null)
            {
                extensions = new Dictionary<Type, object>(fromRule.extensions);
            }

            if (fromRule.MessageGenerator != null)
            {
                MessageGenerator = fromRule.MessageGenerator;
            }
        }

        internal void AddPrecondition(IValidationRule<TTarget> precondition)
        {
            if (preconditions == null)
            {
                preconditions = new List<IValidationRule<TTarget>>();
            }

            preconditions.Add(precondition);
        }

        /// <summary>
        ///   Gets or sets the <see cref="IValidationMessageGenerator" /> to be used to create messages for this rule.
        /// </summary>
        public IValidationMessageGenerator MessageGenerator
        {
            get
            {
                return Result<IValidationMessageGenerator>();
            }
            set
            {
                Set<IValidationMessageGenerator>(value);
            }
        }

        /// <summary>
        ///   Determines whether the specified target is valid.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <returns> <c>true</c> if the specified target is valid; otherwise, <c>false</c> . </returns>
        public bool Check(TTarget target)
        {
            using (var scope = new ValidationScope { Rule = this })
            {
                return Check(target, scope);
            }
        }

        /// <summary>
        ///   Determines whether the specified target is valid.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <param name="scope"> The ValidationScope for the operation. </param>
        /// <returns> <c>true</c> if the specified target is valid; otherwise, <c>false</c> . </returns>
        public virtual bool Check(TTarget target, ValidationScope scope)
        {
            // when there is no active ValidationScope, simply perform the check
            if (scope == null)
            {
                if (preconditions != null && preconditions.Any(rule => !rule.Check(target)))
                {
                    // this rule is considered to have succeeded, i.e. not failed, because it has not been evaluated, so return true:
                    return true;
                }
                return PerformCheck(target, scope);
            }

            if (CheckPreconditions(scope.Parent ?? scope, target))
            {
                return true;
            }

            // get the result of the rule
            var result = PerformCheck(target, scope);

            var messageGenerator = MessageGenerator ?? scope.MessageGenerator;

            // extract paramaters from the scope and store them if needed in a FailedEvaluation
            var parameters = scope.FlushParameters();
            if (!result)
            {
                // record the failure in the ValidationScope
                scope.AddEvaluation(new FailedEvaluation(target, this, messageGenerator)
                {
                    IsInternal = CreatesEvaluationsAsInternal,
                    Parameters = parameters
                });
            }
            else
            {
                scope.AddEvaluation(new SuccessfulEvaluation(target, this, messageGenerator)
                {
                    IsInternal = CreatesEvaluationsAsInternal,
                    Parameters = parameters
                });
            }

            return result;
        }

        protected bool CheckPreconditions(ValidationScope scope, TTarget target)
        {
            if (preconditions != null)
            {
                // check previously-evaluated preconditions. any occurrence of the same combination of rule and target will short circuit the current operation.
                if (scope.AllFailures.Any(f =>
                                          preconditions.Any(pre =>
                                                            Equals(f.Target, target) &&
                                                            ValidationRuleComparer.Instance.Equals(pre, f.Rule))))
                {
                    // this failure indicates a short-circuited precondition. this mechanism identifies preconditions of preconditions.
                    // TODO: (CheckPreconditions) mark the failure so that it can be identified as such for debug purposes
                    scope.AddEvaluation(new FailedEvaluation(target, this)
                    {
                        IsInternal = true
                    });
                    return true;
                }

                // check unevaluated preconditions
                foreach (var rule in preconditions)
                {
                    var tempRule = rule;
                    if (!scope.Evaluations.Any(ex => Equals(ex.Target, target) &&
                                                     ValidationRuleComparer.Instance.Equals(ex.Rule, tempRule)))
                    {
                        using (var internalScope = new ValidationScope { Rule = this })
                        {
                            internalScope.RuleEvaluated += (s, e) =>
                            {
                                var failure = e.RuleEvaluation as FailedEvaluation;
                                if (failure != null)
                                {
                                    failure.IsInternal = true;
                                }
                            };

                            if (!rule.Check(target))
                            {
                                return true;
                            }
                        }

                        // finally we have to check if the rule's preconditions were in turn unsatisfied.
                        var vRule = rule as ValidationRule<TTarget>;
                        if (vRule != null)
                        {
                            if (vRule.CheckPreconditions(scope, target))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///   Performs the actual rule check.
        /// </summary>
        protected virtual bool PerformCheck(TTarget target, ValidationScope scope = null)
        {
            return condition(target);
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns> A <see cref="System.String" /> that represents this instance. </returns>
        public override string ToString()
        {
            var failureMessage = Result<FailureMessageTemplate>();
            if (failureMessage != null)
            {
                return failureMessage.GetMessage(null);
            }
            var value = Result<ErrorCode<string>>();
            var code = value != null ? value.Value : "";
            return code;
        }

        protected internal virtual ValidationRule<TTarget> Clone()
        {
            return new ValidationRule<TTarget>(this);
        }

        internal void Set<T>(T value)
        {
            if (extensions == null)
            {
                extensions = new Dictionary<Type, object>();
            }
            extensions[typeof (T)] = value;
        }

        public T Result<T>()
        {
            if (extensions == null)
            {
                return default(T);
            }

            object value;
            if (extensions.TryGetValue(typeof (T), out value))
            {
                return (T) value;
            }

            return default(T);
        }

        protected internal virtual string Message
        {
            get
            {
                return ToString();
            }
        }

        [DebuggerStepThrough]
        internal class ValidationRuleComparer : IEqualityComparer<IValidationRule>
        {
            public static ValidationRuleComparer Instance = new ValidationRuleComparer();

            public bool Equals(IValidationRule x, IValidationRule y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                var ruleX = x as ValidationRule<TTarget>;
                if (ruleX != null)
                {
                    var ruleY = y as ValidationRule<TTarget>;
                    if (ruleY != null)
                    {
                        return ruleX.condition == ruleY.condition;
                    }
                }

                return x.Equals(y);
            }

            public int GetHashCode(IValidationRule irule)
            {
                var rule = irule as ValidationRule<TTarget>;
                if (rule != null && rule.condition != null)
                {
                    return rule.condition.GetHashCode();
                }

                return irule.GetHashCode();
            }
        }
    }
}