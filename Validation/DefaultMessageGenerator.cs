// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

namespace Its.Validation
{
    /// <summary>
    ///   Builds messages describing <see cref="RuleEvaluation" /> s.
    /// </summary>
    public class DefaultMessageGenerator : IValidationMessageGenerator
    {
        /// <summary>
        ///   Gets the message for the specified <see cref="RuleEvaluation" /> .
        /// </summary>
        /// <param name="evaluation"> The <see cref="RuleEvaluation" /> . </param>
        /// <returns> A validation failure message <see cref="string" /> . </returns>
        public string GetMessage(RuleEvaluation evaluation)
        {
            string template = evaluation.MessageTemplate;
            if (!string.IsNullOrEmpty(template))
            {
                if (evaluation.Parameters != null)
                {
                    return MessageGenerator.Detokenize(template, evaluation.Parameters);
                }
                return template;
            }

            var failure = evaluation as FailedEvaluation;
            if (failure != null)
            {
                return failure.ErrorCode;
            }

            return string.Empty;
        }
    }
}