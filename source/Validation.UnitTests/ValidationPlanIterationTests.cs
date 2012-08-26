// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;
using Its.Validation.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestClass, TestFixture]
    public class ValidationPlanIterationTests
    {
        [Test, TestMethod]
        public void Flat_plan_has_correct_sequence()
        {
            var plan = new ValidationPlan<string>
            {
                Validate.That<string>(s => true),
                Validate.That<string>(s => true),
                Validate.That<string>(s => true),
                Validate.That<string>(s => true),
            };

            Assert.That(plan.AllRules().IsSameSequenceAs(plan));
        }

        [Test, TestMethod]
        public void Flat_plan_with_chained_and_ordered_dependencies_has_correct_sequence()
        {
            var rule1 = Validate.That<string>(s => true).WithErrorMessage("1");
            var rule2 = Validate.That<string>(s => true).WithErrorMessage("2");
            var rule3 = Validate.That<string>(s => true).WithErrorMessage("3");
            var rule4 = Validate.That<string>(s => true).WithErrorMessage("4");

            var plan = new ValidationPlan<string>
            {
                rule1,
                rule2.When(rule1),
                rule3.When(rule2),
                rule4.When(rule3),
            };

            Assert.That(plan.AllRules().IsSameSequenceAs(new[] { rule1, rule2, rule3, rule4 }));
        }

        [Test, TestMethod]
        public void Flat_plan_with_chained_and_unordered_dependencies_has_correct_sequence()
        {
            var rule1 = Validate.That<string>(s => true).WithErrorMessage("1");
            var rule2 = Validate.That<string>(s => true).WithErrorMessage("2").When(rule1);
            var rule3 = Validate.That<string>(s => true).WithErrorMessage("3").When(rule2);
            var rule4 = Validate.That<string>(s => true).WithErrorMessage("4").When(rule3);

            var plan = new ValidationPlan<string>
            {
                rule4,
                rule3,
                rule2,
                rule1,
            };

            Assert.That(plan.AllRules().IsSameSequenceAs(new[] { rule1, rule2, rule3, rule4 }));
        }

        [Test, TestMethod]
        public void Flat_plan_with_dependencies_on_rules_not_in_plan_has_correct_sequence()
        {
            var rule1 = Validate.That<string>(s => true).WithErrorCode("1");
            var rule2 = Validate.That<string>(s => true).WithErrorCode("2");
            var rule3 = Validate.That<string>(s => true).WithErrorCode("3");
            var rule4 = Validate.That<string>(s => true).WithErrorCode("4");

            var plan = new ValidationPlan<string>
            {
                rule2.When(rule1),
                rule3.When(rule2),
                rule4.When(rule3),
            };

            Assert.That(plan.AllRules().IsSameSequenceAs(new[] { rule1, rule2, rule3, rule4 }));
        }

        [Test, TestMethod]
        public void Plan_with_dependency_from_nested_plan_to_outer_rule_has_correct_sequence()
        {
            var rule1_1 = Validate.That<string>(s => true).WithErrorMessage("1.1");
            var rule1_2 = Validate.That<string>(s => true).WithErrorMessage("1.2");
            var rule1_3 = Validate.That<string>(s => true).WithErrorMessage("1.3");
            var rule1_4 = Validate.That<string>(s => true).WithErrorMessage("1.4");
            var rule2_1 = Validate.That<string>(s => true).WithErrorMessage("2.1");
            var rule2_2 = Validate.That<string>(s => true).WithErrorMessage("2.2");
            var rule2_3 = Validate.That<string>(s => true).WithErrorMessage("2.3");
            var nestedPlan = new ValidationPlan<string>
            {
                rule2_1,
                rule2_2,
                rule2_3
            };

            var plan = new ValidationPlan<string>
            {
                rule1_1,
                rule1_2,
                rule1_3,
                nestedPlan.When(rule1_2),
                rule1_4,
            };

            Assert.That(plan.AllRules().IsSameSequenceAs(new[]
            {
                rule1_1,
                rule1_2,
                rule1_3,
                rule2_1,
                rule2_2,
                rule2_3,
                nestedPlan,
                rule1_4
            }));
        }

        [Test, TestMethod]
        public void Plan_with_dependency_from_inner_rule_of_nested_plan_to_outer_rule_has_correct_sequence()
        {
            var rule1_1 = Validate.That<string>(s => true).WithErrorMessage("1.1");
            var rule1_2 = Validate.That<string>(s => true).WithErrorMessage("1.2");
            var rule1_3 = Validate.That<string>(s => true).WithErrorMessage("1.3");
            var rule1_4 = Validate.That<string>(s => true).WithErrorMessage("1.4");
            var rule1_5 = Validate.That<string>(s => true).WithErrorMessage("1.5");
            var rule2_1 = Validate.That<string>(s => true).WithErrorMessage("2.1");
            var rule2_2 = Validate.That<string>(s => true).WithErrorMessage("2.2");
            var rule2_3 = Validate.That<string>(s => true).WithErrorMessage("2.3");

            var plan2 = new ValidationPlan<string>
            {
                rule2_1,
                rule2_2,
                rule2_3,
            }.WithErrorMessage("2");

            var plan1 = new ValidationPlan<string>
            {
                rule1_1,
                rule1_2,
                rule1_3.When(plan2),
                rule1_4,
                plan2,
                rule1_5
            }.WithErrorMessage("1") as ValidationPlan<string>;

            Assert.That(plan1.AllRules().IsSameSequenceAs(
                new[]
                {
                    rule1_1,
                    rule1_2,
                    rule2_1,
                    rule2_2,
                    rule2_3,
                    plan2,
                    rule1_3,
                    rule1_4,
                    rule1_5
                }));
        }

        [Test, TestMethod]
        public void Nested_plan_with_multiple_dependencies_on_same_rule_has_correct_sequence()
        {
            var rule1_1 = Validate.That<string>(s => true).WithErrorMessage("1.1");
            var rule1_2 = Validate.That<string>(s => true).WithErrorMessage("1.2");
            var rule1_3 = Validate.That<string>(s => true).WithErrorMessage("1.3");
            var rule1_4 = Validate.That<string>(s => true).WithErrorMessage("1.4");
            var rule1_5 = Validate.That<string>(s => true).WithErrorMessage("1.5");
            var rule2_1 = Validate.That<string>(s => true).WithErrorMessage("2.1");
            var rule2_2 = Validate.That<string>(s => true).WithErrorMessage("2.2");
            var rule2_3 = Validate.That<string>(s => true).WithErrorMessage("2.3");

            var plan2 = new ValidationPlan<string>
            {
                rule2_1,
                rule2_2.When(rule1_2),
                rule2_3,
            }.WithErrorMessage("2");

            var plan1 = new ValidationPlan<string>
            {
                rule1_1,
                rule1_2,
                rule1_3.When(rule1_2),
                rule1_4,
                plan2.When(rule1_2),
                rule1_5
            }.WithErrorMessage("1") as ValidationPlan<string>;

            Assert.That(plan1.AllRules().IsSameSequenceAs(
                new[]
                {
                    rule1_1,
                    rule1_2,
                    rule1_3,
                    rule1_4,
                    rule2_1,
                    rule2_2,
                    rule2_3,
                    plan2,
                    rule1_5
                }));
        }

        [Test, TestMethod]
        public void Nested_plan_without_dependencies_has_correct_sequence()
        {
            var rule1_1 = Validate.That<string>(s => true).WithErrorMessage("1.1");
            var rule1_2 = Validate.That<string>(s => true).WithErrorMessage("1.2");
            var rule1_3 = Validate.That<string>(s => true).WithErrorMessage("1.3");
            var rule1_4 = Validate.That<string>(s => true).WithErrorMessage("1.4");
            var rule2_1 = Validate.That<string>(s => true).WithErrorMessage("2.1");
            var rule2_2 = Validate.That<string>(s => true).WithErrorMessage("2.2");
            var rule2_3 = Validate.That<string>(s => true).WithErrorMessage("2.3");

            var nestedPlan = new ValidationPlan<string>
            {
                rule2_1,
                rule2_2,
                rule2_3,
            }.WithErrorMessage("1.5");

            var plan = new ValidationPlan<string>
            {
                rule1_1,
                rule1_2,
                rule1_3,
                rule1_4,
                nestedPlan
            };

            Assert.That(plan.AllRules().IsSameSequenceAs(new[]
            {
                rule1_1,
                rule1_2,
                rule1_3,
                rule1_4,
                rule2_1,
                rule2_2,
                rule2_3,
                nestedPlan
            }));
        }
    }
}