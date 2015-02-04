// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using Its.Recipes;

namespace Its.Validation
{
    internal class FailureMessageTemplate<TTarget> : FailureMessageTemplate
    {
        private readonly Func<FailedEvaluation, TTarget, string> buildMessage;

        public FailureMessageTemplate(Func<FailedEvaluation, TTarget, string> buildMessage)
        {
            if (buildMessage == null)
            {
                throw new ArgumentNullException("buildMessage");
            }
            this.buildMessage = buildMessage;
        }

        public override string GetMessage(RuleEvaluation evaluation)
        {
            var failedEvaluation = evaluation as FailedEvaluation ?? new FailedEvaluation();
            
            var template = buildMessage(failedEvaluation,
                                        evaluation.Target
                                                  .IfTypeIs<TTarget>()
                                                  .ElseDefault());

            return MessageGenerator.Detokenize(template, failedEvaluation.Parameters);
        }

        public override string ToString()
        {
            return GetMessage(null);
        }
    }
}