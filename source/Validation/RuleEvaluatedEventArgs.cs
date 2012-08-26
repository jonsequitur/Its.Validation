// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;

namespace Its.Validation
{
    /// <summary>
    /// Carries information about a rule evaluation.
    /// </summary>
    public class RuleEvaluatedEventArgs : EventArgs
    {
        public RuleEvaluatedEventArgs(RuleEvaluation ruleEvaluation)
        {
            if (ruleEvaluation == null)
            {
                throw new ArgumentNullException("ruleEvaluation");
            }
            RuleEvaluation = ruleEvaluation;
        }

        /// <summary>
        /// Gets the rule evaluation that triggered the event.
        /// </summary>
        public RuleEvaluation RuleEvaluation { get; private set; }
    }
}