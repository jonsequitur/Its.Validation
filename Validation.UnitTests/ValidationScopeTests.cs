// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Its.Validation.Configuration;
using Its.Validation.UnitTests.TestClasses;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestFixture]
    public class ValidationScopeTests
    {
        [Test]
        public void ValidationScopes_are_not_shared_across_threads()
        {
            IDictionary<string, object> scope1Params = null;
            IDictionary<string, object> scope2Params = null;
            var barrier = new Barrier(2);

            var task1 = Task.Factory.StartNew(() =>
            {
                using (var scope = new ValidationScope())
                {
                    scope.AddParameter("one", 1);
                    barrier.SignalAndWait();
                    scope1Params = scope.FlushParameters();
                    barrier.SignalAndWait();
                }
            });
            var task2 = Task.Factory.StartNew(() =>
            {
                using (var scope = new ValidationScope())
                {
                    scope.AddParameter("two", 2);
                    barrier.SignalAndWait();
                    scope2Params = scope.FlushParameters();
                    barrier.SignalAndWait();
                }
            });

            task1.Wait();
            task2.Wait();

            Assert.That(scope1Params != scope2Params);
            Assert.That(scope1Params.Count(), Is.EqualTo(1));
            Assert.That(scope2Params.Count(), Is.EqualTo(1));
            Assert.That(scope1Params["one"], Is.EqualTo(1));
            Assert.That(scope2Params["two"], Is.EqualTo(2));
        }

        [Test]
        public virtual void Outer_validation_scope_aggregates_rules_from_inner_scopes()
        {
            var plan = new ValidationPlan<string>
            {
                Validate.That<string>(s => true)
            };

            using (var scope = new ValidationScope())
            {
                using (new ValidationScope())
                {
                    plan.Execute("");
                }
                using (new ValidationScope())
                {
                    plan.Execute("");
                }
                using (new ValidationScope())
                {
                    plan.Execute("");
                }

                Assert.AreEqual(3, scope.Evaluations.Count());
            }
        }

        [Test]
        public void ValidationScope_AllFailures_reports_internal_failures()
        {
            var scope = new ValidationScope();

            scope.AddEvaluation(new FailedEvaluation { IsInternal = true });

            Assert.AreEqual(1, scope.AllFailures.Count());
        }

        [Test]
        public void ValidationScope_Failures_does_not_report_internal_failures()
        {
            var scope = new ValidationScope();

            scope.AddEvaluation(new FailedEvaluation { IsInternal = true });

            Assert.AreEqual(0, scope.Failures.Count());
        }

        [Test]
        public void Rule_evaluations_in_inner_scopes_are_reported_to_all_outer_scopes()
        {
            using (var outerScope = new ValidationScope())
            {
                using (var middleScope = new ValidationScope())
                {
                    using (var innerScope = new ValidationScope())
                    {
                        Validate.That<string>(s => false).Execute("");

                        Assert.That(outerScope.Evaluations.Count(), Is.EqualTo(1));
                        Assert.That(middleScope.Evaluations.Count(), Is.EqualTo(1));
                        Assert.That(innerScope.Evaluations.Count(), Is.EqualTo(1));
                    }
                }
            }
        }

        [Test]
        public void Rule_failures_in_inner_scopes_are_reported_to_all_outer_scopes()
        {
            using (var outerScope = new ValidationScope())
            {
                using (var middleScope = new ValidationScope())
                {
                    using (var innerScope = new ValidationScope())
                    {
                        Validate.That<string>(s => false).Execute("");

                        Assert.That(outerScope.Failures.Count(), Is.EqualTo(1));
                        Assert.That(middleScope.Failures.Count(), Is.EqualTo(1));
                        Assert.That(innerScope.Failures.Count(), Is.EqualTo(1));
                    }
                }
            }
        }

        [Test]
        public void Rule_successes_in_inner_scopes_are_reported_to_all_outer_scopes()
        {
            using (var outerScope = new ValidationScope())
            {
                using (var middleScope = new ValidationScope())
                {
                    using (var innerScope = new ValidationScope())
                    {
                        Validate.That<string>(s => true).Execute("");

                        Assert.That(outerScope.Successes.Count(), Is.EqualTo(1));
                        Assert.That(middleScope.Successes.Count(), Is.EqualTo(1));
                        Assert.That(innerScope.Successes.Count(), Is.EqualTo(1));
                    }
                }
            }
        }

        [Test]
        public void Manually_opening_a_scope_does_not_affect_results_of_plan_execution()
        {
            var unScopesTRexResult = IsAcceptablePet().Execute(Tyrannosaurus());
            Console.WriteLine(unScopesTRexResult);

            using (new ValidationScope())
            {
                var scopedResult = IsAcceptablePet().Execute(Tyrannosaurus());
                Assert.AreEqual(unScopesTRexResult.ToString(), scopedResult.ToString());
            }
        }

        [Test]
        public void ValidationScope_raises_events_as_evaluations_are_performed()
        {
            var wasCalled = false;
            using (var scope = new ValidationScope())
            {
                scope.RuleEvaluated += (sender, e) => { wasCalled = true; };
                new ValidationRule<string>(s => true).Execute("");
            }

            Assert.That(wasCalled);
        }

        [Test]
        public void ValidationScope_raises_events_as_evaluations_are_added_to_nested_scopes()
        {
            var wasCalled = false;

            using (var scope = new ValidationScope())
            {
                scope.RuleEvaluated += (sender, e) => { wasCalled = true; };
                using (new ValidationScope())
                {
                    new ValidationRule<string>(s => true).Execute("");
                }
            }

            Assert.That(wasCalled);
        }

        [Test]
        public void ValidationScope_raises_events_as_evaluations_are_added_to_child_scopes_on_other_threads()
        {
            int evaluationCount = 0;
            var plan = new ValidationPlan<IEnumerable<string>>();
            // parallelize
            plan.AddRule(
                ss => ss.Parallel(Validate.That<string>(s => false).Check));

            using (var scope = new ValidationScope())
            {
                scope.RuleEvaluated += (o, e) => Interlocked.Increment(ref evaluationCount);
                plan.Execute(Enumerable.Range(1, 30).Select(i => i.ToString()));
            }

            Assert.AreEqual(31, evaluationCount);
        }

        [Test]
        public void An_unentered_scope_does_not_receive_parameters()
        {
            var scope = new ValidationScope(false);

            42.As("some parameter");

            Assert.That(scope.FlushParameters(), Is.Null);
        }

        [Test]
        public void An_unentered_scope_can_be_disposed()
        {
            var scope = new ValidationScope(false);

            scope.Dispose();
        }

        [Test]
        public void An_unentered_scope_still_passes_evaluationsd_to_parent_scope()
        {
            var rule = Validate.That<bool>(f => false).WithErrorMessage("oops");
            ValidationReport report;
            using (var parent = new ValidationScope())
            {
                var child = new ValidationScope(false);
                rule.Check(false, child);
                report = ValidationReport.FromScope(parent);
            }

            Assert.That(report.Failures.Count(f => f.Message == "oops"), Is.EqualTo(1));
        }

        [Test]
        public void When_adding_parameters_to_disposed_scope_they_are_not_added_to_the_parent()
        {
            var rule = Validate.That<bool>(f => false).WithErrorMessage("oops");
            ValidationReport report;
            using (var parent = new ValidationScope())
            {
                var child = new ValidationScope(false);
                child.Dispose();
                rule.Check(false, child);
                report = ValidationReport.FromScope(parent);
            }

            Assert.That(report.Failures.Count(f => f.Message == "oops"), Is.EqualTo(0));
        }

        [NUnit.Framework.Ignore("Fix requires some design consideration")]
        [Test]
        public void When_parameters_collide_in_the_ValidationScope_they_can_be_reported_incorrectly()
        {
            // BUG: (When_parameters_collide_in_the_ValidationScope_they_can_be_reported_incorrectly) this also shows an interesting bug where As overwrites because it's in a loop. some strategy for parameter collisions should be implemented.
            var plan = new ValidationPlan<string>
            {
                Validate
                    .That<string>(s =>
                                  Enumerable.Range(0, 10).Every(i =>
                                                                s.Contains(i.ToString().As("i"))))
                    .WithErrorMessage("Missing: {i}")
            }.WithErrorMessage("Not all integers are in the sequence.");

            var report = plan.Execute("0456789");
            Console.WriteLine(report);

            Assert.That(report.ToString(), Contains.Substring("1"));
            Assert.That(report.ToString(), Contains.Substring("2"));
            Assert.That(report.ToString(), Contains.Substring("3"));

            // TODO (Validation_messages_can_be_attached_to_the_top_level_ValidationPlan) write test
            Assert.Fail("Test not written yet.");
        }

        private static ValidationRule<Species> IsAcceptablePet()
        {
            var isNotNull = Validate.That<Species>(
                species => species.As("species", s => s.Name) != null);
            var hasFourLegs = Validate.That<Species>(
                s => s.NumberOfLegs.As("legs") == 4)
                .WithErrorMessage("This species only has {legs} legs");
            var isMammal = Validate.That<Species>(s => s.Genus.Family.Order.Class.Name == "Mammalia")
                .WithErrorMessage("This species is not a mammal.");
            var isNotExtinct = Validate.That<Species>(s => !s.IsExtinct)
                .WithErrorMessage("This species is extinct.");

            return new ValidationPlan<Species>
            {
                isNotNull,
                isMammal.When(isNotNull),
                hasFourLegs.When(isNotNull),
                isNotExtinct.When(isNotNull)
            }
                .WithErrorMessage("Unacceptable as a pet")
                .WithSuccessMessage("A fine pet!");
        }

        private static Species Tyrannosaurus()
        {
            var tyrannosaurus = new Mock<Species>();
            tyrannosaurus.Setup(m => m.Name).Returns("T. Rex");
            tyrannosaurus.Setup(m => m.Genus.Family.Order.Class.Name).Returns("Reptilia");
            tyrannosaurus.Setup(m => m.IsExtinct).Returns(true);
            tyrannosaurus.Setup(m => m.NumberOfLegs).Returns(2);
            return tyrannosaurus.Object;
        }
    }
}