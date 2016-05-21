// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Its.Validation
{
    internal static class RuleEvaluationExtensions
    {
        public static IEnumerable<TEvaluation> NonInternal<TEvaluation>(this IEnumerable<TEvaluation> source)
            where TEvaluation : RuleEvaluation =>
                source.Where(e => !e.IsInternal);
    }
}