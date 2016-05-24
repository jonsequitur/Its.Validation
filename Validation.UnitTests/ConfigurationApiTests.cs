// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Its.Validation.Configuration;
using Its.Validation.UnitTests.TestClasses;
using NUnit.Framework;
using Validation.Tests.TestClasses;
using Assert = NUnit.Framework.Assert;
using StringAssert = NUnit.Framework.StringAssert;

namespace Its.Validation.UnitTests
{
    /// <summary>
    ///   The configuration api tests.
    /// </summary>
    [TestFixture]
    public class ConfigurationApiTests
    {
        [SetUp]
        public void Initialize()
        {
            MessageGenerator.Current = null;
        }

        [Test]
        public void Every_Func_overload_evaluates_every_item_without_short_circuiting()
        {
            var rules = new List<IValidationRule<string>>();
            int callCount = 0;

            for (var i = 0; i < 10; i++)
            {
                rules.Add(Validate.That<string>(s =>
                {
                    callCount++;
                    return false;
                }));
            }

            Assert.AreEqual(false, rules.Every(r => r.Check(string.Empty)));

            callCount.Should().Be(10);
        }

        [Test]
        public void Every_rule_overload_evaluates_every_item_without_short_circuiting()
        {
            var hasAName = Validate.That<Individual>(s => !string.IsNullOrWhiteSpace(s.Name));
            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Individuals.Every(hasAName))
            };
            var species = new Species
            {
                Individuals = new List<Individual>
                {
                    new Individual(),
                    new Individual(),
                    new Individual()
                }
            };

            var report = plan.Execute(species);

            Assert.AreEqual(4, report.Failures.Count());
        }

        [Test]
        public void Dependencies_can_be_declared_within_expression_using_Validate_That()
        {
            var plan = new ValidationPlan<IEnumerable<Species>>();
            plan
                .AddRule(list =>
                         list.Every(species =>
                                    Validate
                                        .That<Species>(s => s.Name.Length < 20)
                                        .WithErrorCode("name too long")
                                        .When(
                                            Validate
                                                .That<Species>(s => s.Name != null)
                                                .WithErrorCode("name null"))
                                        .Check(species)));

            var listToValidate = new[]
            {
                new Species { Name = null },
                new Species { Name = "this name is way too " }
            };

            var report = plan.Execute(listToValidate);
            Console.WriteLine(report);
            Assert.AreEqual(2, report.Failures.Count());
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "name too long"));
        }

        [Test]
        public void Dependencies_can_be_declared_using_a_func()
        {
            var plan = new ValidationPlan<IEnumerable<Species>>();
            plan
                .AddRule(list =>
                         list.Every(
                             Validate
                                 .That<Species>(s => s.Name.Length < 20)
                                 .WithErrorCode("name too long")
                                 .When(s => s.Name != null)
                                 .Check));

            var listToValidate = new[]
            {
                new Species { Name = null },
                new Species { Name = "this name is way too long" }
            };

            var report = plan.Execute(listToValidate);
            Console.WriteLine(report);
            Assert.AreEqual(2, report.Failures.Count());
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "name too long"));
        }

        [Test]
        public void Dependencies_can_be_declared_using_optional_parameter_on_AddRule()
        {
            var plan = new ValidationPlan<Species>();

            plan.AddRule(s => s.Name.Length < 20,
                         only => only.When(s => s.Name != null));

            var speciesWithNullName = new Species { Name = null };
            var speciesWithLongName = new Species { Name = "this name is way too long" };

            var reportOnSpeciesWithNullName = plan.Execute(speciesWithNullName);
            var reportOnSpeciesWithLongName = plan.Execute(speciesWithLongName);

            Assert.AreEqual(0, reportOnSpeciesWithNullName.Failures.Count());
            Assert.AreEqual(1, reportOnSpeciesWithLongName.Failures.Count());
        }

        [Test]
        public void ErrorCode_can_be_declared_using_optional_parameter_on_AddRule()
        {
            var plan = new ValidationPlan<Species>();

            plan.AddRule(s => s.Name.Length < 20,
                         rule => rule.WithErrorCode("oops"));

            var speciesWithLongName = new Species { Name = "this name is way too long" };

            var reportOnSpeciesWithLongName = plan.Execute(speciesWithLongName);

            Assert.AreEqual("oops", reportOnSpeciesWithLongName.Failures.First().ErrorCode);
        }

        [Test]
        public void RemoveRule_can_be_used_to_remove_a_rule_from_a_ValidatioPlan()
        {
            var notNull = Validate.That<string>(s => s != null);
            var notEmpty = Validate.That<string>(s => s != "").When(notNull);

            var plan = new ValidationPlan<string>
            {
                notNull,
                notEmpty
            };

            Assert.That(plan.Check(""), Is.EqualTo(false));

            plan.Remove(notEmpty);

            Assert.That(plan.Check(""), Is.EqualTo(true));
        }

        [Test]
        public void A_rule_removed_by_RemoveRule_can_still_be_dependend_on_by_another_rule()
        {
            var notNull = Validate.That<string>(s => s != null);
            var notEmpty = Validate.That<string>(s => s != "").When(notNull);

            var plan = new ValidationPlan<string>
            {
                notNull,
                notEmpty
            };

            Assert.That(plan.Check(null), Is.EqualTo(false));

            plan.Remove(notNull);

            Assert.That(plan.Check(null), Is.EqualTo(true));
        }

        [Test]
        public void When_When_is_called_with_null_Func_it_throws()
        {
            var rule = Validate.That<string>(s => false);

            Assert.Throws<ArgumentNullException>(() => rule.When((Func<string, bool>) null));
        }

        [Test]
        public void When_When_is_called_with_null_Rule_it_throws()
        {
            var rule = Validate.That<string>(s => false);
            IValidationRule<string>[] rules = null;

            Assert.Throws<ArgumentNullException>(() => rule.When(rules));
        }

        [Test]
        public void ForMember_using_func_returns_expected_member_name_for_immediate_property()
        {
            var rule = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => false).ForMember(s => s.Individuals)
            };

            var report = rule.Execute(new Species());

            Assert.AreEqual("Individuals", report.Failures.Single().MemberPath);
        }

        [Test]
        public void ForMember_using_func_returns_expected_member_name_for_chained_property()
        {
            var rule = new ValidationPlan<Phylum>
            {
                Validate.That<Phylum>(s => false).ForMember(p => p.Kingdom.Name)
            };

            var report = rule.Execute(new Phylum());

            Assert.AreEqual("Kingdom.Name", report.Failures.Single().MemberPath);
        }

        [Test]
        public void ForMember_using_func_throws_for_extension_method()
        {
            Assert.Throws<NotSupportedException>(
                () => Validate.That<Species>(s => false).ForMember(s => s.Individuals.First()));
        }

        [Test]
        public void ForMember_using_expression_throws_not_supported_for_method()
        {
            Assert.Throws<NotSupportedException>(
                () => Validate.That<Species>(s => false).ForMember(s => s.AddIndividual(null)));
        }

        [Test]
        public void A_rule_can_be_instantiated_and_evaluated_during_another_rules_execution()
        {
            var plan = new ValidationPlan<Species>();

            plan.AddRule(
                s => s.Individuals.Every(
                    Validate.That<Individual>(ind => ind.Species == s).Check));

            Console.WriteLine(plan);

            var cat = new Species("Felis silvestris");
            var fluffy = new Individual { Name = "Fluffy", Species = cat };
            var boots = new Individual { Name = "Boots", Species = cat };
            cat.Individuals.AddRange(new[] { fluffy, boots });

            plan.Execute(cat).HasFailures.Should().BeFalse();

            var fido = new Individual { Name = "Fido", Species = new Species("Canis familiaris") };
            cat.Individuals.Add(fido);

            var report = plan.Execute(cat);

            Assert.IsTrue(report.HasFailures);
            Console.WriteLine(report);
            Assert.AreEqual(2, report.Failures.Count());
            Assert.That(report.Failures.Any(failure => failure.Target == fido));
            Assert.That(report.Failures.Any(failure => failure.Target == cat));
        }

        [Test]
        public void One_rule_can_be_evaluated_inside_another_without_framework_being_aware_of_it()
        {
            var individualNameRule = Validate.That<Individual>(i => !string.IsNullOrEmpty(i.Name))
                .WithErrorCode("Name");
            var speciesNameRule = Validate
                .That<Species>(s => !string.IsNullOrEmpty(s.Name))
                .WithErrorCode("Name");
            var speciesIndividualsRule = Validate.That<Species>(s => s.Individuals.All(individualNameRule.Check));

            var plan = new ValidationPlan<Species>();
            plan.AddRule(speciesNameRule);
            plan.AddRule(speciesIndividualsRule);

            var cat = new Species("Felis silvestris");
            var fluffy = new Individual { Name = "Fluffy", Species = cat };
            var boots = new Individual { Name = "Boots", Species = cat };
            cat.Individuals.AddRange(new[] { fluffy, boots });

            Assert.IsFalse(plan.Execute(cat).HasFailures);

            // adding an individual without a name causes the plan to evaluate to false
            cat.Individuals.Add(new Individual { Species = cat });
            var report = plan.Execute(cat);
            Console.WriteLine(report);
            Assert.IsTrue(report.HasFailures);
        }

        [Test]
        public void Rule_can_validate_children_using_func()
        {
            var cat = new Species("Felis silvestris");
            var mutants = new Species("Os mutantes");

            var fluffy = new Individual { Name = "Fluffy", Species = cat };
            var boots = new Individual { Name = "Boots", Species = cat };
            cat.Individuals.AddRange(new[] { fluffy, boots });
            mutants.Individuals.AddRange(new[] { fluffy, boots });
            mutants.Individuals.Add(new Individual { Name = "Bob" });

            var rule = Validate.That<Species>(s => s.Individuals.All(i => i.Species == s));

            Assert.IsTrue(rule.Check(cat));
            Assert.IsFalse(rule.Check(mutants));
        }

        [Test]
        public void Rules_can_be_aggregated_from_function_calls()
        {
            var plan = new ValidationPlan<Species>();
            plan.AddRule(Validate.That<Species>(IndividualsAreAllOfSpecies().Check));

            var cat = new Species("Felis silvestris");
            var fluffy = new Individual { Name = "Fluffy", Species = cat };
            var boots = new Individual { Name = "Boots", Species = cat };
            cat.Individuals.AddRange(new[] { fluffy, boots });

            Assert.IsFalse(plan.Execute(cat).HasFailures);

            // adding an individual without a name causes the plan to evaluate to false
            var fido = new Individual { Name = "Fido", Species = new Species("Canis familiaris") };
            cat.Individuals.Add(fido);

            var report = plan.Execute(cat);

            Assert.IsTrue(report.HasFailures);
            Console.WriteLine(report);
            Assert.AreEqual(3, report.Failures.Count());
            Assert.That(report.Failures.Any(failure => failure.Target == fido));
            Assert.That(report.Failures.Any(failure => failure.Target == cat));
        }

        [Test]
        public void Rule_validates_using_a_func()
        {
            var rule = Validate.That<Species>(species =>
                                              species.Individuals.All(
                                                  i => i.Parent != null && i.Parent.Species == species));
            var giraffe = new Species("Giraffe");
            var joe = new Individual { Name = "Joe" };
            giraffe.Individuals.Add(joe);

            Assert.IsFalse(rule.Check(giraffe));
        }

        [Test]
        public void Setting_message_generator_to_null_does_not_cause_Current_to_return_null()
        {
            MessageGenerator.Current = null;
            Assert.IsNotNull(MessageGenerator.Current);
        }

        [Test]
        public void ValidationPlan_params_array_ctor_can_be_used_to_combine_plans()
        {
            var plan1 = new ValidationPlan<string>
            {
                Validate.That<string>(s => s != "a").WithErrorCode("RULE 1")
            };
            var plan2 = new ValidationPlan<string>
            {
                Validate.That<string>(s => s.Length > 1).WithErrorCode("RULE 2")
            };

            plan1.WithErrorCode("PLAN 1");
            plan2.WithErrorCode("PLAN 2");

            var combinedPlan = new ValidationPlan<string>(plan1, plan2);

            var report = combinedPlan.Execute("a");
            Console.WriteLine(report);
            Assert.AreEqual(4, report.Failures.Count());
        }

        [Test]
        public void When_ValidationPlan_params_array_ctor_receives_null_it_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ValidationPlan<string>((IValidationRule<string>[]) null));
        }

        [Test]
        public void ValidationPlan_collection_initializer_can_be_used_to_combine_plans()
        {
            var plan1 = new ValidationPlan<string>
            {
                Validate.That<string>(s => s != "a").WithErrorCode("RULE 1")
            };
            var plan2 = new ValidationPlan<string>
            {
                Validate.That<string>(s => s.Length > 1).WithErrorCode("RULE 2")
            };

            plan1.WithErrorCode("PLAN 1");
            plan2.WithErrorCode("PLAN 2");

            var combinedPlan = new ValidationPlan<string>
            {
                plan1,
                plan2
            };

            var report = combinedPlan.Execute("a");
            Console.WriteLine(report);
            Assert.AreEqual(4, report.Failures.Count());
        }

        [Test]
        public void ValidationPlan_collection_initializer_can_be_used_to_combine_plans_with_error_code_assignments()
        {
            var notNull = new ValidationPlan<string> { Validate.That<string>(s => s != null) };
            var notEmpty = new ValidationPlan<string> { Validate.That<string>(s => s.Length > 0) };

            var combinedPlan = new ValidationPlan<string>
            {
                notNull.WithErrorCode("null check"),
                notEmpty.WithErrorCode("length check").When(notNull)
            };

            var report = combinedPlan.Execute(string.Empty);
            Assert.AreEqual(2, report.Failures.Count(f => f.ErrorCode == "length check"));

            report = combinedPlan.Execute(null);
            Assert.AreEqual(2, report.Failures.Count(f => f.ErrorCode == "null check"));
        }

        [Test]
        public void ValidationPlan_collection_initializer_can_be_used_to_combine_rules()
        {
            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Individuals.Count > 1),
                Validate.That<Species>(s => s.ExtinctionDate > DateTime.Now)
            };

            var report = plan.Execute(new Species());

            Assert.AreEqual(2, report.Failures.Count());
        }

        [Test]
        public void
            ValidationPlan_collection_initializer_can_be_used_to_combine_rules_with_error_code_assignments()
        {
            var rule1 = Validate.That<string>(s => s != null);
            var rule2 = Validate.That<string>(s => s.Length > 0);

            var plan = new ValidationPlan<string>
            {
                rule1.WithErrorCode("null check"),
                rule2.When(rule1).WithErrorCode("length check")
            };

            var report = plan.Execute(null);
            Assert.AreEqual(1, report.Failures.Count(), report.ToString());
            Assert.AreEqual("null check", report.Failures.First().ErrorCode);

            report = plan.Execute(string.Empty);
            Assert.AreEqual(1, report.Failures.Count());
            Assert.AreEqual("length check", report.Failures.First().ErrorCode);
        }

        /// <summary>
        ///   Returns a ValidationRule that checks that all individuals are of the parent species
        /// </summary>
        /// <returns> A ValidationRule. </returns>
        protected ValidationRule<Species> IndividualsAreAllOfSpecies()
        {
            return Validate.That<Species>(s => s.Individuals.Every(MatchesSpecies(s).Check));
        }

        /// <summary>
        ///   Returns a ValidationRule that checks that an individual's species matches the specified species.
        /// </summary>
        /// <param name="species"> The species. </param>
        /// <returns> A ValidationRule. </returns>
        protected ValidationRule<Individual> MatchesSpecies(Species species)
        {
            return Validate.That<Individual>(i => i.Species == species);
        }

        [Test]
        public void Rule_extension_value_from_class_is_available_on_failure()
        {
            var plan = new ValidationPlan<string>
            {
                Validate
                    .That<string>(s => !s.Contains("foo"))
                    .With(new ErrorClass { Code = 5, Message = "Red" })
            };

            var report = plan.Execute("food");

            Assert.AreEqual("Red",
                            report.Failures.First().Result<ErrorClass>().Message);
        }

        [Test]
        public void Rule_extension_value_from_struct_is_available_on_failure()
        {
            var plan = new ValidationPlan<string>
            {
                Validate
                    .That<string>(s => !s.Contains("foo"))
                    .With(new ErrorStruct { Code = 5, Message = "Red" })
            };

            var report = plan.Execute("food");

            Assert.AreEqual("Red",
                            report.Failures.First().Result<ErrorStruct>().Message);
        }

        [Test]
        public void Cloned_and_forked_rule_with_different_extension_class()
        {
            var rule = Validate.That<string>(s => s.Contains("some string that s will never contain"));

            var plan = new ValidationPlan<string>
            {
                rule.With(new ErrorClass { Message = "first" }),
                rule.With(new ErrorClass { Message = "second" })
            };

            var report = plan.Execute("some string");

            Assert.AreEqual(2, report.Failures.Count(f => f.Result<ErrorClass>() != null));
            Assert.AreEqual(1, report.Failures.Count(f => f.Result<ErrorClass>().Message == "first"));
            Assert.AreEqual(1, report.Failures.Count(f => f.Result<ErrorClass>().Message == "second"));
        }

        [Test]
        public void When_operator_expression_overload_does_not_mutate_rule()
        {
            var baseRule = Validate.That<IEnumerable<int>>(ints => false);
            var rule1 = new ValidationPlan<IEnumerable<int>> { baseRule.When(ints => ints.Count() < 6) };
            var rule2 = new ValidationPlan<IEnumerable<int>> { baseRule.When(ints => ints.Count() < 4) };

            var results1 = rule1.Execute(new[] { 1, 2, 3, 1, 2 });
            var results2 = rule2.Execute(new[] { 1, 2, 3, 1, 2 });

            Assert.AreEqual(1, results1.Failures.Count());
            Assert.AreEqual(0, results2.Failures.Count());
        }

        [Test]
        public void When_operator_rule_overload_does_not_mutate_rule()
        {
            var baseRule = Validate.That<IEnumerable<int>>(ints => false);

            var precondition1 = Validate.That<IEnumerable<int>>(ints => ints.Count() < 6);
            var precondition2 = Validate.That<IEnumerable<int>>(ints => ints.Count() < 4);

            var rule1 = new ValidationPlan<IEnumerable<int>> { baseRule.When(precondition1) };
            var rule2 = new ValidationPlan<IEnumerable<int>> { baseRule.When(precondition2) };

            var results1 = rule1.Execute(new[] { 1, 2, 3, 1, 2 });
            var results2 = rule2.Execute(new[] { 1, 2, 3, 1, 2 });

            Assert.AreEqual(1, results1.Failures.Count());
            Assert.AreEqual(0, results2.Failures.Count());
        }

        [Test]
        public void With_operator_does_not_mutate_rule()
        {
            var baseRule = Validate.That<IEnumerable<int>>(ints => false);
            var rule1 = new ValidationPlan<IEnumerable<int>> { baseRule.With("hello") };
            var rule2 = new ValidationPlan<IEnumerable<int>> { baseRule.With("goodbye") };

            var results1 = rule1.Execute(null);
            var results2 = rule2.Execute(null);

            Assert.AreEqual(1, results1.Failures.Count(f => f.Result<string>() == "hello"));
            Assert.AreEqual(1, results2.Failures.Count(f => f.Result<string>() == "goodbye"));
        }

        [Test]
        public void With_operator_does_not_mutate_plan()
        {
            var baseRule = Validate.That<IEnumerable<int>>(ints => false);
            var rule1 = new ValidationPlan<IEnumerable<int>> { baseRule }.With("hello");
            var rule2 = new ValidationPlan<IEnumerable<int>> { baseRule }.With("goodbye");

            var results1 = rule1.Execute(null);
            var results2 = rule2.Execute(null);

            Assert.AreEqual(1, results1.Failures.Count(f => f.Result<string>() == "hello"));
            Assert.AreEqual(1, results2.Failures.Count(f => f.Result<string>() == "goodbye"));
        }

        [Test]
        public void Success_and_failure_messages_can_be_configured_separately()
        {
            var plan = new ValidationPlan<string>
            {
                Validate.That<string>(s => s.As("value").Length.As("length") > 3.As("min"))
                    .WithErrorMessage("Fail! Your string {value} is only {length} characters.")
                    .WithSuccessMessage("Win! '{value}' is more than {min} characters.")
            };

            var failure = plan.Execute("hi").Evaluations.Single();
            var success = plan.Execute("hello").Evaluations.Single();

            StringAssert.Contains("Fail!", failure.Message);
            StringAssert.Contains("Win!", success.Message);
        }

        [Test]
        public void Handle_returns_false_when_exception_is_thrown()
        {
            var rule = Validate.That<string>(s => Throw<NotSupportedException>())
                .Handle<NotSupportedException, string>();

            var result = rule.Check("");

            Assert.AreEqual(false, result);
        }

        [Test]
        public void Handle_returns_result_of_rule_when_no_exception_is_thrown()
        {
            var trueRule = Validate.That<string>(s => true)
                .Handle<NotSupportedException, string>();
            var falseRule = Validate.That<string>(s => false)
                .Handle((NotSupportedException ex) => { });

            var t = trueRule.Check("");
            var f = falseRule.Check("");

            Assert.AreEqual(true, t);
            Assert.AreEqual(false, f);
        }

        [Test]
        public void Handle_throws_if_exception_is_not_of_specified_type()
        {
            var rule = Validate
                .That<string>(s => Throw<NullReferenceException>())
                .Handle<IndexOutOfRangeException, string>();

            Assert.Throws<NullReferenceException>(() => rule.Check(""));
        }

        [Test]
        public void Handle_can_be_used_to_add_exception_to_parameters()
        {
            var expected = new NotSupportedException();
            var rule = Validate
                .That<string>(s => Throw(expected))
                .Handle((NotSupportedException ex) => ex.As("exception"));

            var report = rule.Execute("");

            Assert.AreEqual(expected, report.Failures.Single().Parameters["exception"]);
        }

        [Test]
        public void Handle_can_be_used_to_add_a_property_of_the_exception_to_parameters()
        {
            var rule = Validate.That<string>(s => Throw<NotSupportedException>())
                .Handle((NotSupportedException ex) => ex.Message.As("message"));

            var report = rule.Execute("");

            Assert.AreEqual(new NotSupportedException().Message, report.Failures.Single().Parameters["message"]);
        }

        [Ignore("Not supported currently")]
        [Test]
        public void Handle_extends_FailedEvaluation_instance_with_caught_exception()
        {
            var ex = new NotSupportedException();
            var rule = Validate.That<string>(s => Throw(ex))
                .Handle<NotSupportedException, string>();

            var report = rule.Execute("");

            Assert.AreEqual(
                ex,
                report.Failures.Single().Result<NotSupportedException>());
        }

        [Ignore("Not supported currently")]
        [Test]
        public void When_multiple_Handles_are_chained_they_all_take_effect()
        {
            var ex = new IndexOutOfRangeException();
            var rule = Validate.That<string>(s => Throw(ex))
                .Handle((IndexOutOfRangeException iore) => ex.As("one"))
                .Handle((SystemException se) => ex.As("two"));

            var report = rule.Execute("");

            Assert.That(report.Failures.Single().Parameters.Count, Is.EqualTo(2));
            Assert.That(report.Failures.Single().Parameters, Has.Some.InstanceOf<SystemException>());
            Assert.That(report.Failures.Single().Parameters, Has.Some.InstanceOf<IndexOutOfRangeException>());
        }

        private bool Throw<TException>(TException ex = null) where TException : Exception, new()
        {
            throw ex ?? new TException();
        }

        public struct ErrorStruct
        {
            public string Message;
            public int Code;
        }

        public class ErrorClass
        {
            public string Message;
            public int Code;
        }
    }
}