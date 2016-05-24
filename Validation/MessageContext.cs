// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Runtime.Remoting.Messaging;

namespace Its.Validation
{
    internal static class MessageContext
    {
        private static readonly string RuleEvaluationKey = "__Its_Validation__RuleEvaluation";

        internal static void SetCurrentRuleEvaluation(RuleEvaluation evaluation) => 
            CallContext.LogicalSetData(RuleEvaluationKey, evaluation);

        internal static RuleEvaluation GetCurrentRuleEvaluation() => 
            CallContext.LogicalGetData(RuleEvaluationKey) as RuleEvaluation;
    }
}