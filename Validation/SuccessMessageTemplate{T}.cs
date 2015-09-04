// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using Its.Recipes;

namespace Its.Validation
{
    internal class SuccessMessageTemplate<TTarget> : SuccessMessageTemplate
    {
        private readonly Func<SuccessfulEvaluation, TTarget, string> buildMessage;

        public SuccessMessageTemplate(Func<SuccessfulEvaluation, TTarget, string> buildMessage)
        {
            if (buildMessage == null)
            {
                throw new ArgumentNullException("buildMessage");
            }
            this.buildMessage = buildMessage;
        }

        public override string GetMessage(RuleEvaluation evaluation)
        {
            var successfulEvaluation = evaluation as SuccessfulEvaluation ?? new SuccessfulEvaluation();
            var template = buildMessage(successfulEvaluation,
                                        successfulEvaluation.Target
                                                            .IfTypeIs<TTarget>()
                                                            .ElseDefault());
            return MessageGenerator.Detokenize(template, successfulEvaluation.Parameters);
        }

        public override string ToString()
        {
            return GetMessage(null);
        }
    }
}