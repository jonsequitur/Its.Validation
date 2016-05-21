// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Its.Validation
{
    /// <summary>
    ///   Builds messages describing <see cref="FailedEvaluation" /> s.
    /// </summary>
    public class DebugMessageGenerator : IValidationMessageGenerator
    {
        /// <summary>
        ///   Stores error codes and corresponding messages
        /// </summary>
        private readonly Dictionary<string /* error code */, string /* message */> messages = new Dictionary<string, string>();

        /// <summary>
        ///   Gets the message for the specified <see cref="RuleEvaluation" /> .
        /// </summary>
        /// <param name="evaluation"> The <see cref="RuleEvaluation" /> . </param>
        /// <returns> A validation message <see cref="string" /> . </returns>
        public string GetMessage(RuleEvaluation evaluation)
        {
            if (evaluation.Rule != null)
            {
                var failure = evaluation as FailedEvaluation;
                if (!string.IsNullOrEmpty(failure?.ErrorCode))
                {
                    if (messages.ContainsKey(failure.ErrorCode))
                    {
                        return messages[failure.ErrorCode];
                    }
                }

                if (!string.IsNullOrEmpty(evaluation.MessageTemplate) && evaluation.Parameters != null)
                {
                    return MessageGenerator.Detokenize(evaluation.MessageTemplate, evaluation.Parameters);
                }
            }

            return BuildMessage(evaluation);
        }

        /// <summary>
        ///   Builds a message describing the <see cref="RuleEvaluation" /> .
        /// </summary>
        /// <param name="evaluation"> The rule evaluation for which to build a message. </param>
        /// <returns> A message describing the evaluation. </returns>
        private static string BuildMessage(RuleEvaluation evaluation)
        {
            MessageContext.SetCurrentRuleEvaluation(evaluation);

            var parts = new List<string>();

            if (evaluation.Target != null)
            {
                parts.Add($"Target: {evaluation.Target}");
            }

            if (evaluation.Parameters != null)
            {
                parts.AddRange(evaluation.Parameters.Select(parameter => $"{parameter.Key}: {parameter.Value}"));
            }

            // add details about any extensions
            // reflecting out the field bypasses the requirement to know the rule's generic type
            var extensionsField = evaluation.Rule?
                                            .GetType()
                                            .GetField("extensions",
                                                      BindingFlags.NonPublic |
                                                      BindingFlags.Instance);
            if (extensionsField != null)
            {
                var extensions = extensionsField.GetValue(evaluation.Rule) as Dictionary<Type, object>;
                if (extensions != null)
                {
                    foreach (var extension in extensions)
                    {
                        // extension.Key is the Type
                        // the extension class should have a meaningful ToString override
                        if ((extension.Value is SuccessMessageTemplate) && evaluation is FailedEvaluation)
                        {
                            continue;
                        }

                        if ((extension.Value is FailureMessageTemplate) && evaluation is SuccessfulEvaluation)
                        {
                            continue;
                        }

                        parts.Add("Extension: " + extension.Key + ": " + extension.Value);
                    }
                }

                parts.Add("Rule: " + evaluation.Rule);
            }

            return string.Join(" / ", parts.ToArray());
        }
    }
}