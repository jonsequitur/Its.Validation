// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using Its.Validation.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestClass, TestFixture]
    public class ValidationRuleExtensionsTests
    {
        [Test, TestMethod]
        public void Validation_plan_can_be_traversed_by_dependency_graph()
        {
            var rule1 = Validate.That<object>(t => false);
            var rule2 = Validate.That<object>(t => false);
            var rule3 = Validate.That<object>(t => false);
            var rule4 = Validate.That<object>(t => false);
            var rule5 = Validate.That<object>(t => false).When(rule1, rule2, rule3, rule4);

            Assert.That(
                rule5.Preconditions().IsSameSequenceAs(
                    new[] { rule1, rule2, rule3, rule4 }));
        }
    }
}