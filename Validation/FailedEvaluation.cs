// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Diagnostics;

namespace Its.Validation
{
    /// <summary>
    ///   Describes a validation failure.
    /// </summary>
    [DebuggerDisplay("Fail: {MemberPath}: {Message}")]
    public class FailedEvaluation : RuleEvaluation
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="FailedEvaluation" /> class.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <param name="rule"> The rule. </param>
        /// <param name="messageGenerator"> The <see cref="IValidationMessageGenerator" /> used to generate validation messages for this failure. </param>
        public FailedEvaluation(object target = null, IValidationRule rule = null, IValidationMessageGenerator messageGenerator = null)
            : base(target, rule, messageGenerator)
        {
        }

        /// <summary>
        ///   Gets or sets the error code.
        /// </summary>
        /// <value> The error code. </value>
        public string ErrorCode => Result<ErrorCode<string>, string>(ec => ec.Value,
                                                                     orElse: () => string.Empty);

        /// <summary>
        ///   Gets the message template for the rule evaluation.
        /// </summary>
        /// <remarks>
        ///   This template may contain tokens intended to be filled in with parameters collected during the rule evaluation.
        /// </remarks>
        public override string MessageTemplate => Result<FailureMessageTemplate, string>(template => template.GetMessage(this),
                                                                                         orElse: () => base.MessageTemplate);
    }
}