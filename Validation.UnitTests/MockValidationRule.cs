// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Linq;

namespace Its.Validation.UnitTests
{
    public class MockValidationRule<TTarget> : ValidationRule<TTarget>
    {
        public MockValidationRule(bool returnValue) : base(target => returnValue)
        {
        }

        protected override bool PerformCheck(TTarget target, ValidationScope scope = null)
        {
            CallCount++;
            return base.PerformCheck(target, scope);
        }

        public int CallCount { get; private set; }
    }
}