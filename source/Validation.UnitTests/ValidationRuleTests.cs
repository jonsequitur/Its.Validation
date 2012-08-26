// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using Its.Validation.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestClass, TestFixture]
    public class ValidationRuleTests
    {
        [Test, TestMethod]
        public void Two_validation_rules_having_the_same_condition_do_not_have_the_same_hash_code()
        {
            Func<string, bool> condition = s => true;
            var rule1 = Validate.That(condition);
            var rule2 = Validate.That(condition);

            Assert.That(rule1.GetHashCode(), Is.Not.EqualTo(rule2.GetHashCode()));
        }

        [Test, TestMethod]
        public void Two_validation_rules_having_the_same_condition_are_not_equal()
        {
            Func<string, bool> condition = s => true;
            var rule1 = Validate.That(condition);
            var rule2 = Validate.That(condition);

            Assert.That(rule1 != rule2);
        }
    }
}