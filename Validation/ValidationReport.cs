// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Its.Validation
{
    /// <summary>
    ///   Holds information about the results of a set of validation rule evaluations.
    /// </summary>
    public class ValidationReport
    {
        private readonly List<RuleEvaluation> evaluations = new List<RuleEvaluation>();

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationReport" /> class.
        /// </summary>
        public ValidationReport()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationReport" /> class.
        /// </summary>
        /// <param name="evaluations"> The rule evaluations to be included in the report. </param>
        public ValidationReport(IEnumerable<RuleEvaluation> evaluations)
        {
            if (evaluations == null)
            {
                throw new ArgumentNullException("evaluations");
            }
            this.evaluations = evaluations
                .NonInternal()
                .ToList();
        }

        /// <summary>
        ///   Gets all of the <see cref="RuleEvaluation" /> s that were executed.
        /// </summary>
        public IEnumerable<RuleEvaluation> Evaluations
        {
            get
            {
                return evaluations;
            }
        }

        /// <summary>
        ///   Gets the <see cref="SuccessfulEvaluation" /> s.
        /// </summary>
        public IEnumerable<SuccessfulEvaluation> Successes
        {
            get
            {
                return evaluations.OfType<SuccessfulEvaluation>().NonInternal();
            }
        }

        /// <summary>
        ///   Gets the <see cref="FailedEvaluation" /> s.
        /// </summary>
        public IEnumerable<FailedEvaluation> Failures
        {
            get
            {
                return evaluations.OfType<FailedEvaluation>().NonInternal();
            }
        }

        /// <summary>
        ///   Gets the rules that were executed in the creation of the report.
        /// </summary>
        public IEnumerable<IValidationRule> RulesExecuted
        {
            get
            {
                return evaluations.Select(e => e.Rule);
            }
        }

        /// <summary>
        ///   Gets a value indicating whether the report contains any failed evaluations.
        /// </summary>
        /// <value> <c>true</c> if this instance has failures; otherwise, <c>false</c> . </value>
        public bool HasFailures
        {
            get
            {
                return Failures.Any();
            }
        }

        internal static ValidationReport FromScope(ValidationScope scope)
        {
            return new ValidationReport(scope.Evaluations);
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns> A <see cref="System.String" /> that represents this instance. </returns>
        public override string ToString()
        {
            var failed = Evaluations.OfType<FailedEvaluation>()
                                    .Select(e => "    " + e.ToString())
                                    .Where(msg => !string.IsNullOrWhiteSpace(msg));

            var passed = Evaluations.OfType<SuccessfulEvaluation>()
                                    .Select(e => "    " + e.ToString())
                                    .Where(msg => !string.IsNullOrWhiteSpace(msg));

            return string.Format(
                "{0} failed (out of {1} evaluations)\n  Failed:\n{2}\n  Passed:\n{3}",
                Failures.Count(),
                Evaluations.Count(),
                string.Join(Environment.NewLine, failed),
                string.Join(Environment.NewLine, passed));
        }
    }
}