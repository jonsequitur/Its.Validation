// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

namespace Its.Validation
{
    /// <summary>
    ///   Builds messages describing <see cref="RuleEvaluation" /> s.
    /// </summary>
    public interface IValidationMessageGenerator
    {
        /// <summary>
        ///   Gets the message for the specified <see cref="RuleEvaluation" /> .
        /// </summary>
        /// <returns> A validation failure message <see cref="string" /> . </returns>
        string GetMessage(RuleEvaluation evaluation);
    }
}