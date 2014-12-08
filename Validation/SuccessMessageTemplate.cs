// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;

namespace Its.Validation
{
    internal class SuccessMessageTemplate : MessageTemplate
    {
        private readonly Func<SuccessfulEvaluation, string> buildMessage;

        protected SuccessMessageTemplate()
        {
        }

        public SuccessMessageTemplate(string value) : this(_ => value)
        {
        }

        public SuccessMessageTemplate(Func<SuccessfulEvaluation, string> buildMessage)
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
            var template = buildMessage(successfulEvaluation);
            if (evaluation == null)
            {
                return template;
            }
            return MessageGenerator.Detokenize(template, successfulEvaluation.Parameters);
        }

        public override string ToString()
        {
            return GetMessage(null);
        }
    }
}