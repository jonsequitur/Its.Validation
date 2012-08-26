// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Its.Validation.Configuration;

namespace Its.Validation
{
    /// <summary>
    ///   The validation plan.
    /// </summary>
    /// <typeparam name="TTarget"> The <see cref="Type" /> that the ValidationPlan validates. </typeparam>
    [DebuggerDisplay("Plan: {Message}")]
    public class ValidationPlan<TTarget> : ValidationRule<TTarget>, IEnumerable<IValidationRule<TTarget>>
    {
        /// <summary>
        ///   The rules.
        /// </summary>
        private readonly List<IValidationRule<TTarget>> rules = new List<IValidationRule<TTarget>>();

        private EvaluationStrategy<TTarget> strategy;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationPlan&lt;TTarget&gt;" /> class.
        /// </summary>
        public ValidationPlan()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationPlan{TTarget}" /> class.
        /// </summary>
        /// <param name="messageGenerator"> The message generator. </param>
        public ValidationPlan(IValidationMessageGenerator messageGenerator)
        {
            if (messageGenerator == null)
            {
                throw new ArgumentNullException("messageGenerator");
            }
            MessageGenerator = messageGenerator;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationPlan{TTarget}" /> class.
        /// </summary>
        /// <param name="validationRules"> The validation rules. </param>
        public ValidationPlan(params IValidationRule<TTarget>[] validationRules) : this()
        {
            if (validationRules == null)
            {
                throw new ArgumentNullException("validationRules");
            }
            rules.AddRange(validationRules);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationPlan&lt;TTarget&gt;" /> class.
        /// </summary>
        protected ValidationPlan(ValidationPlan<TTarget> fromPlan)
            : base(fromPlan)
        {
            if (fromPlan == null)
            {
                throw new ArgumentNullException("fromPlan");
            }
            rules = fromPlan.rules;
            MessageGenerator = fromPlan.MessageGenerator;
        }

        internal EvaluationStrategy<TTarget> Strategy
        {
            get
            {
                return strategy ??
                       Result<EvaluationStrategy<TTarget>>() ??
                       EvaluationStrategy<TTarget>.EvaluateAll;
            }
            set
            {
                strategy = value;
            }
        }

        /// <summary>
        ///   Adds a rule to the validation plan.
        /// </summary>
        /// <param name="rule"> The rule to add. </param>
        public ValidationPlan<TTarget> AddRule(
            IValidationRule<TTarget> rule)
        {
            rules.Add(rule);
            return this;
        }

        /// <summary>
        ///   Adds a rule to the validation plan.
        /// </summary>
        /// <param name="rule"> The rule to add. </param>
        /// <param name="setup"> A function that further sets up the added rule. </param>
        /// <returns> </returns>
        public ValidationPlan<TTarget> AddRule(
            Func<TTarget, bool> rule,
            Func<ValidationRule<TTarget>, ValidationRule<TTarget>> setup = null)
        {
            var validationRule = new ValidationRule<TTarget>(rule);
            if (setup != null)
            {
                validationRule = setup(validationRule);
            }
            rules.Add(validationRule);
            return this;
        }

        /// <summary>
        ///   Adds the specified rule.
        /// </summary>
        /// <param name="rule"> The rule. </param>
        /// <returns> The rule </returns>
        /// <remarks>
        ///   This method is equivalent to <see cref="AddRule" /> . It is included to provide support for collection initializer syntax.
        /// </remarks>
        [Browsable(false)]
        public void Add(IValidationRule<TTarget> rule)
        {
            AddRule(rule);
        }

        /// <summary>
        ///   Evaluates all constituent validation rules against the specified target.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <param name="haltOnFirstFailure"> When true, stops execution on the first failed rule </param>
        /// <returns> </returns>
        public ValidationReport Execute(TTarget target, bool haltOnFirstFailure = false)
        {
            using (var scope = new ValidationScope { MessageGenerator = MessageGenerator, Rule = this })
            {
                if (haltOnFirstFailure)
                {
                    scope.HaltsOnFirstFailure = true;
                }

                foreach (var rule in rules)
                {
                    var ruleTemp = rule;
                    if (!scope.HasFailed(target, ruleTemp))
                    {
                        var success = rule.Check(target);

                        // ValidationRule<TTarget> handles this internally but otherwise add it to the scope.
                        if (!(rule is ValidationRule<TTarget>))
                        {
                            if (!success)
                            {
                                scope.AddEvaluation(new FailedEvaluation(target, rule, MessageGenerator));
                            }
                            else
                            {
                                scope.AddEvaluation(new SuccessfulEvaluation(target, rule, MessageGenerator));
                            }
                        }
                    }

                    if (scope.ShouldHalt())
                    {
                        break;
                    }
                }

                return ValidationReport.FromScope(scope);
            }
        }

        public static ValidationPlan<TTarget> Merge(params KeyValuePair<string, ValidationPlan<TTarget>>[] plans)
        {
            var newPlan = new ValidationPlan<TTarget>();
            foreach (var pair in plans)
            {
                var plan = pair.Value;
                newPlan
                    .AddRule(Validate.That<TTarget>(t => !plan.Execute(t).HasFailures)
                                 .WithErrorCode(pair.Key));
            }

            return newPlan;
        }

        protected override bool PerformCheck(TTarget target, ValidationScope scope = null)
        {
            using (scope = new ValidationScope(scope) { Rule = this })
            {
                if (scope.HaltsOnFirstFailure)
                {
                    // allow the scope to override in favor of halting on first failure. (overrides in the other direction are not supported.)
                    return EvaluationStrategy<TTarget>.HaltOnFirstFailure.Evaluate(this, target, scope);
                }

                return Strategy.Evaluate(this, target, scope);
            }
        }

        protected internal override ValidationRule<TTarget> Clone()
        {
            return new ValidationPlan<TTarget>(this);
        }

        /// <summary>
        ///   Iterates all rules in order of dependency.
        /// </summary>
        internal IEnumerable<IValidationRule<TTarget>> AllRules()
        {
            var yielded = new HashSet<IValidationRule<TTarget>>(ValidationRuleComparer.Instance);

            foreach (var irule in rules)
            {
                // preconditions
                var rule = irule as ValidationRule<TTarget>;
                if (rule != null)
                {
                    foreach (var precondition in AllPreconditions(rule))
                    {
                        if (yielded.Add(precondition))
                        {
                            yield return precondition;
                        }
                    }
                }

                // consituent rules
                var nestedPlan = irule as ValidationPlan<TTarget>;
                if (nestedPlan != null)
                {
                    foreach (var innerRule in nestedPlan.AllRules())
                    {
                        if (yielded.Add(innerRule))
                        {
                            yield return innerRule;
                        }
                    }
                }

                if (yielded.Add(irule))
                {
                    yield return irule;
                }
            }
        }

        private static IEnumerable<IValidationRule<TTarget>> AllPreconditions(ValidationRule<TTarget> rule)
        {
            if (rule != null && rule.preconditions != null)
            {
                foreach (var precondition in rule.preconditions)
                {
                    // if the precondition is a ValidationPlan, yield all of its rules and the plan itself
                    // this will traverse preconditions
                    var preconditionPlan = precondition as ValidationPlan<TTarget>;
                    if (preconditionPlan != null)
                    {
                        foreach (var planRule in preconditionPlan.AllRules())
                        {
                            yield return planRule;
                        }

                        yield return preconditionPlan;
                        continue;
                    }

                    // otherwise, recursively check the rule's preconditions, so that the last will be yielded first
                    var preconditionRule = precondition as ValidationRule<TTarget>;
                    if (preconditionRule != null)
                    {
                        foreach (var prePreCondition in AllPreconditions(preconditionRule))
                        {
                            yield return prePreCondition;
                        }
                    }

                    // if the precondition is not a ValidationPlan, yield just the rule
                    yield return precondition;
                }
            }
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns> A <see cref="System.String" /> that represents this instance. </returns>
        public override string ToString()
        {
            return string.Format("{2} (ValidationPlan<{0}> with {1} rules)",
                                 typeof (TTarget).Name,
                                 rules.Count,
                                 base.ToString());
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        public IEnumerator GetEnumerator()
        {
            return rules.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns> A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection. </returns>
        IEnumerator<IValidationRule<TTarget>> IEnumerable<IValidationRule<TTarget>>.GetEnumerator()
        {
            return rules.GetEnumerator();
        }
    }
}