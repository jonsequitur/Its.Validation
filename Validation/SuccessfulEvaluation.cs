// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Diagnostics;

namespace Its.Validation
{
    /// <summary>
    ///   The result of a rule that passed.
    /// </summary>
    [DebuggerDisplay("Pass: {MemberPath}: {Message}")]
    public class SuccessfulEvaluation : RuleEvaluation
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SuccessfulEvaluation" /> class.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <param name="rule"> The rule. </param>
        /// <param name="messageGenerator"> </param>
        public SuccessfulEvaluation(object target = null, IValidationRule rule = null, IValidationMessageGenerator messageGenerator = null) : base(target, rule, messageGenerator)
        {
        }

        /// <summary>
        ///   Gets the message template for the rule evaluation.
        /// </summary>
        /// <remarks>
        ///   This template may contain tokens intended to be filled in with parameters collected during the rule evaluation.
        /// </remarks>
        public override string MessageTemplate
        {
            get
            {
                return Result<SuccessMessageTemplate, string>(template => template.GetMessage(this),
                                                           orElse: () => base.MessageTemplate);
            }
        }
    }
}