// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Validation.Configuration;
using Its.Validation.UnitTests.TestClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestClass, TestFixture]
    public class PrevalidationTests
    {
        [TestInitialize, SetUp]
        public void TestInitialize()
        {
            MessageGenerator.Current = null;
        }

        [Test, TestMethod]
        public void When_individual_ValidationRule_Check_is_called_with_no_ValidationScope_then_preconditions_are_checked()
        {
            var rule = Validate.That<string>(s => s.Length > 5).When(s => s != null);

            Assert.IsTrue(rule.Check(null));
        }

        [Test, TestMethod]
        public virtual void Rules_can_be_excluded_based_on_precondition_rule_failing()
        {
            var nullRule = Validate.That<Species>(s => s.Name != null);
            var nameRule = Validate.That<Species>(s => s.Name.Length > 0);
            var plan = new ValidationPlan<Species>
            {
                nullRule,
                nameRule.When(nullRule)
            };

            var nameless = new Species { Name = null };

            var report = plan.Execute(nameless);

            // when we execute, the null rule's failure should result in the second rule never being evaluated, with the following effect. (were the second rule evaluated, it would throw NullReferenceException.)
            Assert.AreEqual(1, report.Failures.Count());
            Assert.AreEqual(nullRule, report.Failures.First().Rule);
        }

        [Test, TestMethod]
        public virtual void Intermediate_preconditions_that_return_false_prevent_later_rules_from_being_evaluated()
        {
            var a = Validate.That<string>(s => false);
            var b = new MockValidationRule<string>(false);
            var c = new MockValidationRule<string>(false);

            var plan = new ValidationPlan<string>
            {
                a,
                b.When(a),
                c.When(b)
            };

            var report = plan.Execute("");

            Console.WriteLine(report);

            Assert.AreEqual(0, b.CallCount);
            Assert.AreEqual(0, c.CallCount);
        }

        [Test, TestMethod]
        public virtual void
            Intermediate_preconditions_that_return_true_prevent_later_rules_from_being_evaluated_using_mock_validation_rule
            ()
        {
            var a = Validate.That<string>(s => false);
            var b = new MockValidationRule<string>(true);
            var c = new MockValidationRule<string>(false);

            var plan = new ValidationPlan<string>
            {
                a,
                b.When(a),
                c.When(b)
            };

            var report = plan.Execute("");

            Console.WriteLine(report);

            Assert.AreEqual(0, b.CallCount);
            Assert.AreEqual(0, c.CallCount);
        }

        [Test, TestMethod]
        public virtual void
            Intermediate_preconditions_that_return_true_prevent_later_rules_from_being_evaluated_using_actual_validation_rule
            ()
        {
            var speciesNotNull = Validate.That<Species>(t => t != null);
            var validSpeciesName = Validate.That<Species>(t => !string.IsNullOrEmpty(t.Name));
            var notTooManyIndividuals = Validate
                .That<Species>(t => t.Individuals.Count() <= 45)
                .When(s => s.Individuals != null);
            var noDuplicates =
                Validate.That<Species>(t => t.Individuals.Distinct().Count() == t.Individuals.Count());

            var plan = new ValidationPlan<Species>
            {
                speciesNotNull,
                new ValidationPlan<Species>
                {
                    validSpeciesName,
                    notTooManyIndividuals,
                    noDuplicates.When(notTooManyIndividuals),
                }.When(speciesNotNull)
            };

            var report = plan.Execute(new Species { Name = "cat", Individuals = null });

            Assert.AreEqual(0, report.Failures.Count());
        }

        [Test, TestMethod]
        public virtual void Plans_can_be_excluded_based_on_precondition_rule_failing()
        {
            var nullRule = Validate.That<Species>(s => s.Name != null);
            var namePlan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Name.Length > 0),
                Validate.That<Species>(s => s.Name.Length > 0),
            };
            var plan = new ValidationPlan<Species>
            {
                nullRule,
                namePlan.When(nullRule)
            };

            var nameless = new Species { Name = null };

            var report = plan.Execute(nameless);

            // when we execute, the null rule's failure should result in the second rule never being evaluated, with the following effect. (were the second rule evaluated, it would throw NullReferenceException.)
            Assert.AreEqual(1, report.Failures.Count());
            Assert.AreEqual(nullRule, report.Failures.First().Rule);
        }

        [Test, TestMethod]
        public virtual void When_func_precondition_fails_and_is_not_in_plan_no_validation_failure_is_created_for_it()
        {
            Func<string, bool> preconditionThatFails = s => false;
            var dependentRule = Validate.That<string>(s => false).WithErrorCode("dependent rule");

            var plan = new ValidationPlan<string>
            {
                dependentRule.When(preconditionThatFails)
            };

            var report = plan.Execute("");

            Console.WriteLine(report);

            Assert.AreEqual(0, report.Failures.Count());
        }

        [Test, TestMethod]
        public virtual void When_func_precondition_succeeds_and_is_not_in_plan_no_validation_success_is_created_for_it()
        {
            Func<string, bool> preconditionThatSucceeds = s => true;
            var dependentRule = Validate.That<string>(s => false);

            var plan = new ValidationPlan<string>
            {
                dependentRule.When(preconditionThatSucceeds)
            };

            var report = plan.Execute("");

            Console.WriteLine(report);

            Assert.AreEqual(0, report.Successes.Count());
        }

        [Test, TestMethod]
        public virtual void When_rule_precondition_fails_and_is_not_in_plan_no_validation_failure_is_created_for_it()
        {
            var preconditionThatFails = Validate.That<string>(s => false);
            var dependentRule = Validate.That<string>(s => false).WithErrorCode("dependent rule");

            var plan = new ValidationPlan<string>
            {
                dependentRule.When(preconditionThatFails)
            };

            var report = plan.Execute("");

            Console.WriteLine(report);

            Assert.AreEqual(0, report.Failures.Count());
        }

        [Test, TestMethod]
        public virtual void Rules_can_be_excluded_based_on_precondition_failing_precondition_is_not_in_validation_plan()
        {
            Func<Species, bool> nullRule = s => s.Name != null;

            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Name.Length > 0).When(nullRule)
            };
            var nameless = new Species { Name = null };

            var report = plan.Execute(nameless);

            // when we execute, the null rule's failure should result in the second rule never being evaluated, with the following effect. (were the second rule evaluated, it would throw NullReferenceException.)
            Assert.AreEqual(0, report.Failures.Count());
        }

        [Test, TestMethod]
        public virtual void When_when_overload_accepting_func_is_used_no_validation_failure_is_generated()
        {
            var dodo = new Species();

            var plan = new ValidationPlan<Species>
            {
                Validate
                    .That<Species>(s => s.Individuals.Every(
                        i => i.Species == s))
                    .When(s => s.Individuals.Any())
            };

            var report = plan.Execute(dodo);

            Assert.AreEqual(0, report.Failures.Count());
        }

        [Test, TestMethod]
        public virtual void When_precondition_is_first_class_rule_in_plan_precondition_is_only_evaluated_once()
        {
            var precondition = new Mock<IValidationRule<Species>>();
            precondition
                .Setup(pc => pc.Check(It.IsAny<Species>(), It.IsAny<ValidationScope>()))
                .Returns(false);

            var plan = new ValidationPlan<Species>
            {
                precondition.Object,
                // these rules will throw if the precondition does not short circuit them
                Validate.That<Species>(species => species.Name.Length > 0)
                    .When(precondition.Object),
                Validate.That<Species>(species => !species.Name.Contains("!"))
                    .When(precondition.Object)
            };

            var nameless = new Species { Name = null };
            plan.Execute(nameless);

            // we only want the precondition called once. the value should be stored.
            precondition.Verify(pc => pc.Check(It.IsAny<Species>()), Times.Once());
        }

        [Test, TestMethod]
        public virtual void
            When_precondition_is_first_class_rule_in_plan_and_has_different_error_code_assigned_precondition_is_only_evaluated_once
            ()
        {
            var count = 0;
            Func<Species, bool> counter = s =>
            {
                count++;
                return false;
            };

            var precondition = Validate.That<Species>(s => counter(s));

            var plan = new ValidationPlan<Species>
            {
                precondition.WithErrorCode("some code"),
                // these rules will throw if the precondition does not short circuit them
                Validate.That<Species>(species => species.Name.Length > 0)
                    .When(precondition),
                Validate.That<Species>(species => !species.Name.Contains("!"))
                    .When(precondition)
            };

            var nameless = new Species { Name = null };
            Assert.DoesNotThrow(() => plan.Execute(nameless));

            Assert.AreEqual(1, count);
        }

        [Test, TestMethod]
        public virtual void
            When_precondition_is_not_in_plan_and_precondition_succeeds_precondition_is_only_evaluated_once()
        {
            var count = 0;
            Func<Species, bool> counter = s =>
            {
                count++;
                return true;
            };

            var precondition = Validate.That<Species>(s => counter(s));

            var plan = new ValidationPlan<Species>
            {
                // these rules will throw if the precondition does not short circuit them
                Validate.That<Species>(species => true)
                    .When(precondition),
                Validate.That<Species>(species => true)
                    .When(precondition)
            };

            var nameless = new Species { Name = null };
            plan.Execute(nameless);

            // we only want the precondition called once. the value should be stored.
            Assert.AreEqual(1, count);
        }

        [Test, TestMethod]
        public virtual void When_precondition_is_not_in_plan_and_precondition_fails_precondition_is_only_evaluated_once
            ()
        {
            var count = 0;
            Func<Species, bool> counter = s =>
            {
                count++;
                return false;
            };

            var precondition = Validate.That<Species>(s => counter(s));

            var plan = new ValidationPlan<Species>
            {
                // these rules will throw if the precondition does not short circuit them
                Validate.That<Species>(species => true)
                    .When(precondition),
                Validate.That<Species>(species => true)
                    .When(precondition)
            };

            plan.Execute(null);

            // we only want the precondition called once. the value should be stored.
            Assert.AreEqual(1, count);
        }

        [Test, TestMethod]
        public virtual void When_precondition_is_used_in_nested_plans_it_is_not_reevaluated()
        {
            var count = 0;
            Func<string, bool> counter = s =>
            {
                count++;
                return true;
            };

            var precondition = Validate.That<string>(counter).WithMessage("precondition");

            var plan = new ValidationPlan<string>
            {
                Validate.That<string>(s => s.Length > 0).When(precondition),
                new ValidationPlan<string>
                {
                    Validate.That<string>(s => s.Contains("a")).When(precondition),
                    Validate.That<string>(s => s.Contains("b")).When(precondition),
                }
            };

            plan.Execute("ab");

            Assert.AreEqual(1, count);
        }

        [Test, TestMethod]
        public virtual void When_precondition_is_used_in_iterative_rule_it_is_re_evaluated_for_different_targets()
        {
            var notNegative = Validate.That<int>(i => i.As("j") > 0).WithMessage("{j} is too low");
            var notHigherThan10 =
                Validate.That<int>(i => i.As("i") < 11).WithMessage("{i} is too high").When(notNegative);

            var plan = new ValidationPlan<IEnumerable<int>>
            {
                Validate.That<IEnumerable<int>>(ints =>
                                                ints.Every(notHigherThan10.Check))
            };

            var report = plan.Execute(new[] { -2, 1, 23, 3, 4, 13 });

            Console.WriteLine(report);
            Console.WriteLine("rule executed: " + report.RulesExecuted);

            Assert.AreEqual(2, report.Failures.Count(f => f.Rule == notHigherThan10));
        }

        [Test, TestMethod]
        public virtual void When_precondition_and_cloned_precondition_are_both_used_they_are_evaluated_as_equivalent()
        {
            var notEmptyRule = Validate.That<string>(s => !string.IsNullOrEmpty(s));
            var tooLongRule = Validate.That<string>(s => s.Length < 10);

            var plan = new ValidationPlan<string>
            {
                notEmptyRule.WithErrorCode("empty!"),
                tooLongRule.When(notEmptyRule).WithErrorCode("too long!"),
            };

            var report = plan.Execute("");
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "empty!"));

            report = plan.Execute("sdff3'4otj;4kth;kdfjsekfjpf4");
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "too long!"));
        }

        [Test, TestMethod]
        public virtual void
            When_specifying_multiple_preconditions_and_one_precondition_fails_subsequent_preconditions_are_not_evaluated
            ()
        {
            var precondition1 = new Mock<IValidationRule<string>>();
            precondition1
                .Setup(r => r.Check(It.IsAny<string>()))
                .Returns(false);
            var precondition2 = new Mock<IValidationRule<string>>();
            precondition2
                .Setup(r => r.Check(It.IsAny<string>())).Returns(true);

            // this rule will fail but should be short circuited
            var rule = Validate.That<string>(s => false)
                .When(precondition1.Object)
                .When(precondition2.Object);

            var plan = new ValidationPlan<string> { rule };

            var report = plan.Execute("");

            precondition1.Verify(m => m.Check(It.IsAny<string>()), Times.Once());
            precondition2.Verify(m => m.Check(It.IsAny<string>()), Times.Never());
            Assert.True(!report.Failures.Any(f => f.Rule == rule));
        }

        [Test, TestMethod]
        public virtual void Multiple_rules_dependent_on_same_precondition_are_evaluated_when_precondition_is_cloned()
        {
            var precondition = Validate.That<string>(s => s != null);

            var dependent1 = Validate.That<string>(s => !s.Contains("a"));
            var dependent2 = Validate.That<string>(s => !s.Contains("b"));
            var plan = new ValidationPlan<string>
            {
                // a clone of the precondition is added to the plan, while the dependent rules call When on the precondition itself
                precondition.WithErrorCode("precondition"),
                dependent1.When(precondition).WithErrorCode("a"),
                dependent2.When(precondition).WithErrorCode("b")
            };

            var report = plan.Execute("b");
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "b"));
        }

        [Test, TestMethod]
        public virtual void Multiple_rules_dependent_on_same_precondition_are_evaluated_when_precondition_is_uncloned()
        {
            var precondition = Validate.That<string>(s => s != null);

            var dependent1 = Validate.That<string>(s => !s.Contains("a"));
            var dependent2 = Validate.That<string>(s => !s.Contains("b"));
            var plan = new ValidationPlan<string>
            {
                // the precondition is unmodified in the plan and thus uncloned
                precondition,
                dependent1.When(precondition).WithErrorCode("a"),
                dependent2.When(precondition).WithErrorCode("b")
            };

            var report = plan.Execute("b");
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "b"));
        }

        [Test, TestMethod]
        public virtual void
            When_rule_is_declared_first_as_precondition_then_as_rule_only_one_validation_failure_is_generated()
        {
            var precondition = Validate.That<string>(s => s != null).WithErrorCode("null");
            var rule = Validate.That<string>(s => s.Length > 10);

            var plan = new ValidationPlan<string>
            {
                rule.When(precondition).WithErrorCode("too short"),
                precondition
            };

            var report = plan.Execute(null);

            foreach (var failure in report.Failures)
            {
                Console.WriteLine(failure);
            }

            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "null"));
        }

        [NUnit.Framework.Ignore("Scenario not currently supported"), Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        [Test, TestMethod]
        public virtual void When_rule_is_declared_first_as_precondition_then_as_rule_it_is_only_evaluated_once()
        {
            var precondition = new MockValidationRule<string>(false);
            var rule = Validate.That<string>(s => s.Length > 10);

            var plan = new ValidationPlan<string>
            {
                rule.When(precondition).WithErrorCode("too short"),
                precondition
            };

            var report = plan.Execute(null);

            foreach (var failure in report.Failures)
            {
                Console.WriteLine(failure);
            }

            Assert.AreEqual(1, precondition.CallCount);
        }

        [Test, TestMethod]
        public void A_single_rule_with_a_precondition_is_checked_by_Check_overload_1()
        {
            var rule = Validate.That<string>(s =>
            {
                Assert.Fail("Precondition should not be checked");
                return s.Contains("a");
            }).When(s => s != null);

            rule.Check(null);
        }

        [Test, TestMethod]
        public void A_single_rule_with_a_precondition_is_checked_by_Check_overload_2()
        {
            var rule = Validate.That<string>(s =>
            {
                Assert.Fail("Precondition should not be checked");
                return s.Contains("a");
            }).When(s => s != null);

            rule.Check(null, null);
        }
    }
}