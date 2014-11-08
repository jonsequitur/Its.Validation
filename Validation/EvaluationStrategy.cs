// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Validation.Configuration;

namespace Its.Validation
{
    internal class EvaluationStrategy<T>
    {
        private readonly Func<IEnumerable<IValidationRule<T>>, T, ValidationScope, bool> evaluate;

        public EvaluationStrategy(Func<IEnumerable<IValidationRule<T>>, T, ValidationScope, bool> evaluate)
        {
            if (evaluate == null)
            {
                throw new ArgumentNullException("evaluate");
            }
            this.evaluate = evaluate;
        }

        public virtual bool Evaluate(IEnumerable<IValidationRule<T>> rules, T target, ValidationScope scope)
        {
            return evaluate(rules, target, scope);
        }

        public static EvaluationStrategy<T> HaltOnFirstFailure = new EvaluationStrategy<T>(
            (rules, target, scope) => rules.All(rule => rule.Check(target, scope)));

        public static EvaluationStrategy<T> EvaluateAll = new EvaluationStrategy<T>(
            (rules, target, scope) => rules.Every(rule => rule.Check(target, scope)));
    }
}