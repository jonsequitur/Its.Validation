// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Validation.Configuration;
using Its.Validation.UnitTests.TestClasses;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestFixture]
    public class ValidationPlanTests
    {
        [SetUp]
        public void TestInitialize()
        {
            MessageGenerator.Current = null;
        }

        [Test]
        public void ValidationPlan_executes_multiple_rules_and_ValidationFailure_count_is_correct()
        {
            var plan = new ValidationPlan<Species>();
            var rule1 = Validate.That<Species>(s => s.Name != "cat");
            var rule2 = Validate.That<Species>(s => s.Genus.Name != "felis");
            plan.AddRule(rule1);
            plan.AddRule(rule2);

            var report = plan.Execute(new Species { Name = "cat", Genus = new Genus { Name = "felis" } });

            Assert.AreEqual(2, report.Failures.Count());
        }

        [Test]
        public void ValidationPlan_Task_executes_multiple_rules_and_ValidationFailure_count_is_correct()
        {
            var plan = new ValidationPlan<Species>();
            var rule1 = Validate.That<Species>(s => s.Name != "cat");
            var rule2 = Validate.That<Species>(s => s.Genus.Name != "felis");
            plan.AddRule(rule1);
            plan.AddRule(rule2);
            var cat = new Species { Name = "cat", Genus = new Genus { Name = "felis" } };

            var task = plan.ExecuteAsync(cat);
            var report = task.Result;

            Assert.AreEqual(2, report.Failures.Count());
        }

        [Test]
        public void ValidationPlan_executes_all_rules()
        {
            var rule1WasCalled = false;
            var rule1 = Validate.That<Species>(_ => rule1WasCalled = true);
            var rule2WasCalled = false;
            var rule2 = Validate.That<Species>(_ => rule2WasCalled = true);
            var rule3WasCalled = false;
            var rule3 = Validate.That<Species>(_ => rule3WasCalled = true);

            var plan = new ValidationPlan<Species>
            {
                rule1,
                rule2,
                rule3
            };

            plan.Execute(new Species());

            Assert.That(rule1WasCalled);
            Assert.That(rule2WasCalled);
            Assert.That(rule3WasCalled);
        }

        [Test]
        public async Task ValidationPlan_Task_executes_all_rules()
        {
            var rule1WasCalled = false;
            var rule1 = Validate.That<Species>(_ => rule1WasCalled = true);
            var rule2WasCalled = false;
            var rule2 = Validate.That<Species>(_ => rule2WasCalled = true);
            var rule3WasCalled = false;
            var rule3 = Validate.That<Species>(_ => rule3WasCalled = true);

            var plan = new ValidationPlan<Species>
            {
                rule1,
                rule2,
                rule3
            };

            await plan.ExecuteAsync(new Species());

            Assert.That(rule1WasCalled);
            Assert.That(rule2WasCalled);
            Assert.That(rule3WasCalled);
        }

        [Test]
        public void Iterative_nested_rules_are_all_executed()
        {
            var innerRule = Validate.That<Individual>(i => false);

            var plan = new ValidationPlan<Species>();
            plan.AddRule(s => s.Individuals.Every(innerRule.Check));

            var species = new Species();
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());

            var report = plan.Execute(species);

            Assert.AreEqual(4, report.Failures.Where(f => f.Target is Individual).Count());
        }

        [Test]
        public void Iterative_nested_tasks_are_all_executed()
        {
            var innerRule = Validate.That<Individual>(i => false);
            var plan = new ValidationPlan<Species>();
            plan.AddRule(s => s.Individuals.Every(innerRule.Check));
            var species = new Species();
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());

            var task = plan.ExecuteAsync(species);
            var report = task.Result;

            Assert.AreEqual(4, report.Failures.Where(f => f.Target is Individual).Count());
        }

        [Test]
        public void Rules_within_ValidationPlan_can_be_iterated()
        {
            var plan = new ValidationPlan<string>();
            Enumerable.Range(1, 10).ForEach(_AppDomain =>
                                            plan.AddRule(Validate.That<string>(s => false)));

            Assert.AreEqual(10, plan.Count());
        }

        [Test]
        public void Nested_Rules_within_ValidationPlan_are_not_iterated_when_plan_is_iterated()
        {
            var plan = new ValidationPlan<string>();
            Enumerable.Range(1, 10).ForEach(_ => plan.AddRule(new ValidationPlan<string>
            {
                Validate.That<string>(s => false),
                Validate.That<string>(s => false)
            }));

            Assert.AreEqual(10, plan.Count());
        }

        [Test]
        public void Iterative_nested_rules_can_be_executed_with_short_circuiting()
        {
            var innerRule = Validate.That<Individual>(i => false);

            var plan = new ValidationPlan<Species>();
            plan.AddRule(s => s.Individuals.All(innerRule.Check));

            var species = new Species();
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());

            var report = plan.Execute(species);

            Assert.AreEqual(1, report.Failures.Where(f => f.Target is Individual).Count());
        }

        [Test]
        public void Iterative_nested_tasks_can_be_executed_with_short_circuiting()
        {
            var innerRule = Validate.That<Individual>(i => false);
            var plan = new ValidationPlan<Species>();
            plan.AddRule(s => s.Individuals.All(innerRule.Check));
            var species = new Species();
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());
            species.Individuals.Add(new Individual());

            var task = plan.ExecuteAsync(species);
            var report = task.Result;

            Assert.That(report.Failures.Count(f => f.Target is Individual), Is.EqualTo(1));
        }

        [Test]
        public void ValidationPlans_can_be_combined()
        {
            var plan1 = new ValidationPlan<Species>();
            plan1.AddRule(
                Validate.That<Species>(
                    s =>
                    s.Individuals.Every(
                        i => Validate.That<Individual>(
                            ind => ind.Species == s)
                                 .WithErrorCode("species mismatch")
                                 .Check(i))
                    )
                );

            var plan2 = new ValidationPlan<Species>();
            plan2.AddRule(
                Validate.That<Species>(
                    s =>
                    s.Individuals.Every(
                        Validate.That<Individual>(
                            ind => !string.IsNullOrEmpty(ind.Name))
                            .WithErrorCode("empty name")
                            .Check)
                    )
                );

            var species = new Species { Name = "Felis silvestris" };
            var nameless = new Individual { Name = "", Species = species };
            species.Individuals.Add(nameless);
            species.Individuals.Add(new Individual { Name = "Garfield", Species = species });
            species.Individuals.Add(new Individual { Name = "Morris", Species = species });
            var fido = new Individual { Name = "Fido" };
            species.Individuals.Add(fido);

            var report = ValidationPlan<Species>.Merge(
                new KeyValuePair<string, ValidationPlan<Species>>("one", plan1),
                new KeyValuePair<string, ValidationPlan<Species>>("two", plan2)
                ).Execute(species);

            Assert.That(report.Failures.Count(f => f.ErrorCode == "one"), Is.EqualTo(2));
            Assert.That(report.Failures.Count(f => f.ErrorCode == "two"), Is.EqualTo(2));
            Assert.That(report.Failures.Count(f => f.Target == fido), Is.EqualTo(1));
            Assert.That(report.Failures.Count(f => f.Target == nameless), Is.EqualTo(1));
        }

        [Test]
        public void haltOnFirstFailure_set_to_false_all_rules_are_evaluated()
        {
            var ruleWasCalled = false;
            var nestedPlanRule1WasCalled = false;
            var nestedPlanRule2WasCalled = false;
            var nestedPlanRule3WasCalled = false;
            var nestedPlanRule4WasCalled = false;
            var nestedPlan1 = new ValidationPlan<object>
            {
                Validate.That<object>(p =>
                {
                    nestedPlanRule1WasCalled = true;
                    return false;
                }),
                Validate.That<object>(p =>
                {
                    nestedPlanRule2WasCalled = true;
                    return false;
                }),
            };
            var nestedPlan2 = new ValidationPlan<object>
            {
                Validate.That<object>(p =>
                {
                    nestedPlanRule3WasCalled = true;
                    return false;
                }),
                Validate.That<object>(p =>
                {
                    nestedPlanRule4WasCalled = true;
                    return false;
                }),
            };
            var rule = Validate.That<object>(p =>
            {
                ruleWasCalled = true;
                return false;
            });
            var plan = new ValidationPlan<object>(
                rule,
                nestedPlan1,
                nestedPlan2);

            plan.Execute(new object());

            Assert.IsTrue(ruleWasCalled);
            Assert.IsTrue(nestedPlanRule1WasCalled);
            Assert.IsTrue(nestedPlanRule2WasCalled);
            Assert.IsTrue(nestedPlanRule3WasCalled);
            Assert.IsTrue(nestedPlanRule4WasCalled);
        }

        [Test]
        public void haltOnFirstFailure_set_to_true_does_not_execute_subsequent_rules_after_failure()
        {
            bool called1 = false, called2 = false;
            var plan = new ValidationPlan<bool>(
                Validate.That<bool>(p =>
                {
                    called1 = true;
                    return false;
                }),
                Validate.That<bool>(p =>
                {
                    called2 = true;
                    return true;
                })
                );

            plan.Execute(true, haltOnFirstFailure: true);

            Assert.IsTrue(called1);
            Assert.IsFalse(called2);
        }

        [Test]
        public void haltOnFirstFailure_set_to_true_last_rule_executes_when_no_failures()
        {
            bool called1 = false, called2 = false;
            var plan = new ValidationPlan<bool>(
                Validate.That<bool>(p =>
                {
                    called1 = true;
                    return true;
                }),
                Validate.That<bool>(p =>
                {
                    called2 = true;
                    return true;
                })
                );

            plan.Execute(true, haltOnFirstFailure: true);

            Assert.IsTrue(called1);
            Assert.IsTrue(called2);
        }

        [Test]
        public void haltOnFirstFailure_set_to_true_and_rule_with_failing_func_precondition_then_does_not_stop_evaluation()
        {
            var called = false;
            var plan = new ValidationPlan<bool>(
                Validate.That<bool>(p => true).When(b => false),
                Validate.That<bool>(p =>
                {
                    called = true;
                    return true;
                })
                );

            plan.Execute(true, haltOnFirstFailure: true);

            Assert.IsTrue(called);
        }

        [Test]
        public void haltOnFirstFailure_set_to_true_and_rule_with_failing_rule_precondition_then_does_not_stop_evaluation()
        {
            var precondition = Validate.That<bool>(b => false);
            var called = false;
            var plan = new ValidationPlan<bool>(
                Validate.That<bool>(p => true).When(precondition),
                Validate.That<bool>(p =>
                {
                    called = true;
                    return true;
                })
                );

            plan.Execute(true, haltOnFirstFailure: true);

            Assert.IsTrue(called);
        }

        [Test]
        public void haltOnFirstFailure_set_to_true_and_failing_rule_then_precondition_of_next_rule_does_not_evaluate()
        {
            var called = false;
            var precondition = Validate.That<bool>(p =>
            {
                called = true;
                return true;
            });
            var plan = new ValidationPlan<bool>(
                Validate.That<bool>(p => false),
                Validate.That<bool>(p => true).When(precondition)
                );

            plan.Execute(true, haltOnFirstFailure: true);

            Assert.IsFalse(called);
        }

        [Test]
        public void haltOnFirstFailure_applies_to_nested_plan()
        {
            var plan = new ValidationPlan<bool>
            {
                Validate.That<bool>(b => true),
                new ValidationPlan<bool>
                {
                    Validate.That<bool>(b => false),
                    Validate.That<bool>(b => false),
                    Validate.That<bool>(b => false),
                    Validate.That<bool>(b => false),
                    Validate.That<bool>(b => false)
                }
            };

            var result = plan.Execute(true, haltOnFirstFailure: true);

            Assert.That(result.Failures.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Nested_plan_evaluates_all_rules()
        {
            var rule1Evaluated = false;
            var rule2Evaluated = false;
            var rule3Evaluated = false;
            var plan = new ValidationPlan<string>()
                .AddRule(s => true)
                .AddRule(new ValidationPlan<string>()
                             .AddRule(_ =>
                             {
                                 rule1Evaluated = true;
                                 return false;
                             }, fail => fail.WithErrorMessage("1"))
                             .AddRule(_ =>
                             {
                                 rule2Evaluated = true;
                                 return false;
                             }, fail => fail.WithErrorMessage("2"))
                             .AddRule(_ =>
                             {
                                 rule3Evaluated = true;
                                 return false;
                             }, fail => fail.WithErrorMessage("3")));

            var report = plan.Execute(null);

            Assert.That(rule1Evaluated);
            Assert.That(rule2Evaluated);
            Assert.That(rule3Evaluated);
            Assert.That(report.Evaluations.Count(),
                        Is.EqualTo(5));
        }

        [Test]
        public void Nested_plan_does_not_evaluate_all_when_parent_is_executed_with_haltOnFirstFailure_set_to_true()
        {
            var plan = new ValidationPlan<string>()
                .AddRule(s => true)
                .AddRule(new ValidationPlan<string> { Strategy = EvaluationStrategy<string>.EvaluateAll }
                             .AddRule(_ => false, fail => fail.WithErrorMessage("1"))
                             .AddRule(_ => false, fail => fail.WithErrorMessage("2"))
                             .AddRule(_ => false, fail => fail.WithErrorMessage("3")));

            var report = plan.Execute(null, haltOnFirstFailure: true);

            Console.WriteLine(report);

            Assert.That(report.Evaluations.Count(),
                        Is.EqualTo(3));
        }

        [Test]
        public void Nested_plan_can_evaluate_all_when_parent_uses_HaltOnFirstFailure_strategy()
        {
            var plan = new ValidationPlan<string> { Strategy = EvaluationStrategy<string>.HaltOnFirstFailure }
                .AddRule(s => true)
                .AddRule(new ValidationPlan<string> { Strategy = EvaluationStrategy<string>.EvaluateAll }
                             .AddRule(_ => false, fail => fail.WithErrorMessage("1"))
                             .AddRule(_ => false, fail => fail.WithErrorMessage("2"))
                             .AddRule(_ => false, fail => fail.WithErrorMessage("3")));

            var report = plan.Execute(null);

            Console.WriteLine(report);

            Assert.That(report.Evaluations.Count(),
                        Is.EqualTo(5));
        }

        [Test]
        public void Nested_plan_can_halt_on_first_failure_when_parent_uses_EvaluateAll_strategy()
        {
            var plan = new ValidationPlan<string> { Strategy = EvaluationStrategy<string>.EvaluateAll }
                .AddRule(s => true)
                .AddRule(new ValidationPlan<string> { Strategy = EvaluationStrategy<string>.HaltOnFirstFailure }
                             .AddRule(_ => false, rule => rule.WithErrorMessage("1"))
                             .AddRule(_ => false, rule => rule.WithErrorMessage("2"))
                             .AddRule(_ => false, rule => rule.WithErrorMessage("3")));

            var report = plan.Execute(null);

            Console.WriteLine(report);

            Assert.That(report.Evaluations.Count(),
                        Is.EqualTo(3));
        }

        [Test]
        public void ValidationPlan_evaluates_all_when_strategy_is_EvaluateAll()
        {
            var plan = new ValidationPlan<string>
            {
                Strategy = EvaluationStrategy<string>.EvaluateAll
            };
            for (int i = 0; i < 10; i++)
            {
                plan.AddRule(_ => false);
            }

            Assert.That(plan.Execute(null).Evaluations.Count(),
                        Is.EqualTo(10));
        }

        [Test]
        public void Null_rules_cannot_be_added_to_a_ValidationPlans()
        {
            var plan = new ValidationPlan<string>();

            IValidationRule<string> nullRule = null;

            Action addNullRule = () => plan.Add(nullRule);

            addNullRule.ShouldThrow<ArgumentNullException>();
        }

        [Ignore("Scenario not finished")]
        [Test]
        public void ValidationPlan_halts_on_first_failure_when_strategy_is_HaltOnFirstFailure()
        {
            // TODO: (ValidationPlan_halts_on_first_failure_when_strategy_is_HaltOnFirstFailure) 
            var plan = new ValidationPlan<string>
            {
                Strategy = EvaluationStrategy<string>.HaltOnFirstFailure
            };

            for (int i = 0; i < 10; i++)
            {
                plan.AddRule(_ => false);
            }

            Assert.That(plan.Execute(null).Evaluations.Count(),
                        Is.EqualTo(1));
        }
    }
}