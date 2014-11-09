// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Its.Validation.Configuration;
using Its.Validation.UnitTests.TestClasses;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using StringAssert = NUnit.Framework.StringAssert;

namespace Its.Validation.UnitTests
{
    [TestFixture]
    public class ValidationReportingTests
    {
        [SetUp]
        public void TestInitialize()
        {
            MessageGenerator.Current = null;
        }

        [Test]
        public virtual void Static_default_MessageGenerator_is_not_null()
        {
            Assert.IsNotNull(MessageGenerator.Current);
        }

        [Test]
        public virtual void ValidationFailures_use_MessageGenerator_assigned_to_ValidationPlan()
        {
            var messageGenerator = new Mock<IValidationMessageGenerator>();
            messageGenerator.Setup(g => g.GetMessage(It.IsAny<FailedEvaluation>())).Returns("result");

            var plan = new ValidationPlan<string>(messageGenerator.Object);
            // always fail
            plan.AddRule(s => false);

            var failure = plan.Execute("test").Failures.First();

            Console.WriteLine(failure.Message);

            messageGenerator.VerifyAll();
        }

        [Test]
        public void When_ValidationPlan_ctor_receives_null_message_generator_it_throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ValidationPlan<string>((IValidationMessageGenerator) null));
        }

        [Test]
        public virtual void Can_customize_message_at_the_framework_level()
        {
            var messageGenerator = new Mock<IValidationMessageGenerator>();
            messageGenerator.Setup(g => g.GetMessage(It.IsAny<FailedEvaluation>())).Returns("here i am");
            MessageGenerator.Current = messageGenerator.Object;

            var rule = Validate.That<Individual>(i => i.Species.ExtinctionDate < DateTime.Now);
            var failure = new FailedEvaluation(new Species(), rule);
            var msg = MessageGenerator.Current.GetMessage(failure);
            Assert.IsTrue(msg == "here i am");
        }

        [Test]
        public virtual void Can_customize_message_differently_for_the_same_rule_in_different_plans()
        {
            var generator1 = new Mock<IValidationMessageGenerator>();
            generator1.Setup(g => g.GetMessage(It.IsAny<FailedEvaluation>())).Returns("one");
            var generator2 = new Mock<IValidationMessageGenerator>();
            generator2.Setup(g => g.GetMessage(It.IsAny<FailedEvaluation>())).Returns("two");

            var rule = Validate.That<string>(s => false);

            var plan1 = new ValidationPlan<string>(generator1.Object);
            var plan2 = new ValidationPlan<string>(generator2.Object);

            plan1.AddRule(rule);
            plan2.AddRule(rule);

            var report1 = plan1.Execute("");
            var report2 = plan2.Execute("");

            Assert.AreEqual(1, report1.Failures.Count());
            Assert.AreEqual(1, report2.Failures.Count());

            Assert.AreEqual("one", report1.Failures.First().Message);
            Assert.AreEqual("two", report2.Failures.First().Message);
        }

        [Test]
        public void WithErrorMessage_lambda_overload_can_access_the_FailedEvaluation_to_build_a_message()
        {
            var isCat = Validate.That<Species>(c => c.Name == "Felis silvestris".As("species"))
                .WithErrorMessage(f => "We're looking for a {species}, and you passed a " + f.Target);

            var report = isCat.Execute(new Species { Name = "Iguana iguana" });

            Assert.That(report.Failures.Single().Message, Is.EqualTo("We're looking for a Felis silvestris, and you passed a Iguana iguana"));
        }

        [Test]
        public void WithErrorMessage_lambda_anonymous_message_generator_is_cloned()
        {
            var isCat = Validate.That<Species>(c => c.Name == "Felis silvestris".As("species"))
                .WithErrorMessage(f => "We're looking for a {species}, and you passed a " + f.Target)
                .Clone();

            var report = isCat.Execute(new Species { Name = "Iguana iguana" });

            Assert.That(report.Failures.Single().Message, Is.EqualTo("We're looking for a Felis silvestris, and you passed a Iguana iguana"));
        }

        [Test]
        public void WithErrorMessage_lambda_overload_can_access_the_SuccessfulEvaluation_to_build_a_message()
        {
            var isCat = Validate.That<Species>(c => c.Name == "Felis silvestris")
                .WithSuccessMessage(f => "Thank you for this lovely " + f.Target);

            var report = isCat.Execute(new Species { Name = "Felis silvestris" });

            Assert.That(report.Successes.Single().Message, Is.EqualTo("Thank you for this lovely Felis silvestris"));
        }

        [Test]
        public virtual void Same_rule_in_different_plans_can_have_different_error_codes()
        {
            var rule = Validate.That<string>(
                s => !s.Contains("A") & !s.Contains("B"));

            var planA = new ValidationPlan<string>
            {
                rule.WithErrorCode("string contains A")
            };

            var planB = new ValidationPlan<string>
            {
                rule.WithErrorCode("string contains B")
            };

            var report = planA.Execute("A");
            Console.WriteLine(report);
            Assert.IsTrue(report.Failures.Any(f => f.ErrorCode == "string contains A"),
                          "The error code for the rule changed");

            report = planB.Execute("B");
            Console.WriteLine(report);
            Assert.IsTrue(report.Failures.Any(f => f.ErrorCode == "string contains B"));
        }

        [Test]
        public virtual void MemberPath_can_be_set_differently_for_the_same_rule_in_different_contexts()
        {
            var nameNotEmpty =
                Validate.That<Species>(s => !string.IsNullOrEmpty(s.Name));

            var plan = new ValidationPlan<Species>();

            plan.AddRule(nameNotEmpty.ForMember("species"));
            plan.AddRule(
                s => s.Individuals.Every(
                    i => nameNotEmpty.ForMember("subspecies").Check(i.Species)));

            var cat = new Species();
            var fluffy = new Individual { Species = cat };
            cat.Individuals.Add(fluffy);
            var report = plan.Execute(cat);

            Console.WriteLine(report);
            Assert.AreEqual(3, report.Failures.Count());
            Assert.AreEqual(1, report.Failures.Where(f => f.MemberPath == "species").Count());
            Assert.AreEqual(1, report.Failures.Where(f => f.MemberPath == "subspecies").Count());
        }

        [Test]
        public void Extension_can_be_obtained_from_another_rule_farther_up_the_execution_stack()
        {
            var family = new Family
            {
                Genuses = new List<Genus>
                {
                    new Genus
                    {
                        Species = new List<Species>
                        {
                            new Species
                            {
                                Name = "Canis lupus familiaris",
                                NumberOfLegs = 4
                            }
                        }
                    },
                }
            };
            var hasNoLegs = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.As("species").NumberOfLegs.As("leg-count") == 0)
                    .WithErrorMessage("{species} has {leg-count} legs!")
            };
            var genusIsLegless = Validate.That<Genus>(
                g => g.As("genus").Species.Every(hasNoLegs.Check))
                .ForMember(g => g.Species);
            var familyIsLegless =
                new ValidationPlan<Family> { Validate.That<Family>(f => f.Genuses.Every(genusIsLegless)) };

            var report = familyIsLegless.Execute(family);

            // the idea here is that the member path and message are coming from two different rules, one of which is being executed in the context of the other, so we want to see them both in the outer rule's validation failure
            Assert.That(report.Failures.Any(
                f => f.Message == "Canis lupus familiaris has 4 legs!"
                     && f.MemberPath == "Species"));
        }

        [Test]
        public void Innermost_extension_is_chosen_when_resolving_from_stack_of_ValidationPlans()
        {
            var rule = new ValidationPlan<string>
            {
                new ValidationPlan<string>
                {
                    new ValidationPlan<string>
                    {
                        Validate.That<string>(_ => false)
                    }
                }.WithErrorMessage("three")
            };

            var report = rule.Execute("");

            report.Failures.ForEach(f => Console.WriteLine(f.Message + " (has " + f.RuleStack.Count() + " rules in its stack)"));

            Assert.That(report.Failures.Count(), Is.EqualTo(3));
            Assert.That(report.Failures.ElementAt(0).MessageTemplate == "three");
            Assert.That(report.Failures.ElementAt(1).MessageTemplate == "three");
            Assert.That(report.Failures.ElementAt(2).MessageTemplate == "three");
        }

        [Test]
        public void Innermost_extension_is_chosen_when_resolving_from_stack_of_plans_and_rules()
        {
            var rule = new ValidationPlan<string>
            {
                Validate.That<string>(s =>
                                      new ValidationPlan<string>
                                      {
                                          Validate.That<string>(_ => false)
                                      }.Check(s))
                    .WithErrorMessage("three")
            };

            var report = rule.Execute("");

            report.Failures.ForEach(f => Console.WriteLine(f.Message + " (has " + f.RuleStack.Count() + " rules in its stack)"));

            Assert.That(report.Failures.Count(), Is.EqualTo(3));
            Assert.That(report.Failures.ElementAt(0).MessageTemplate == "three");
            Assert.That(report.Failures.ElementAt(1).MessageTemplate == "three");
            Assert.That(report.Failures.ElementAt(2).MessageTemplate == "three");
        }

        [Test]
        public void Innermost_extension_is_chosen_when_resolving_from_stack_of_plans_and_multiple_levels_of_rules()
        {
            var rule = new ValidationPlan<string>
            {
                Validate.That<string>(s =>
                                      Validate.That<string>(
                                          Validate.That<string>(_ => false).Check)
                                          .Check(s))
                    .WithErrorMessage("three")
            };

            var report = rule.Execute("");

            report.Failures.ForEach(f => Console.WriteLine(f.Message + " (has " + f.RuleStack.Count() + " rules in its stack)"));

            Assert.That(report.Failures.Count(), Is.EqualTo(3));
            Assert.That(report.Failures.ElementAt(0).MessageTemplate == "three");
            Assert.That(report.Failures.ElementAt(1).MessageTemplate == "three");
            Assert.That(report.Failures.ElementAt(2).MessageTemplate == "three");
        }

        [Test]
        public virtual void ErrorCode_can_be_set_differently_for_the_same_rule_in_different_contexts()
        {
            var nameNotEmpty =
                Validate
                    .That<Species>(s => !string.IsNullOrEmpty(s.Name))
                    .WithErrorCode("NameCannotBeEmpty");

            var plan = new ValidationPlan<Species>();

            plan.AddRule(
                s => s.Individuals.Every(
                    i => nameNotEmpty
                             .WithErrorCode("IndividualsSpeciesNameCannotBeEmpty")
                             .Check(i.Species)));
            plan.AddRule(nameNotEmpty.WithErrorCode("SpeciesNameCannotBeEmpty"));

            var cat = new Species();
            var fluffy = new Individual { Species = cat };
            cat.Individuals.Add(fluffy);
            var report = plan.Execute(cat);

            Console.WriteLine(report);
            Assert.AreEqual(3, report.Failures.Count());
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "SpeciesNameCannotBeEmpty"));
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "IndividualsSpeciesNameCannotBeEmpty"));
        }

        [Test]
        public virtual void When_multiple_specifications_of_error_code_are_made_last_one_overrides()
        {
            var individualIsNamed = Validate.That<Individual>(i => i.Name != null).WithErrorCode("not named (1)");

            var plan = new ValidationPlan<Species>
            {
                Validate
                    .That<Species>(
                        s =>
                        s.Individuals.Every(individualIsNamed.WithErrorCode("not named (2)").Check))
            };

            var cat = new Species();
            var nameless = new Individual();
            cat.Individuals.Add(nameless);

            var report = plan.Execute(cat);
            Console.WriteLine(report);

            Assert.AreEqual(0, report.Failures.Count(f => f.ErrorCode == "not named (1)"));
            Assert.AreEqual(1, report.Failures.Count(f => f.ErrorCode == "not named (2)"));
        }

        [Test]
        public virtual void Validation_parameters_can_be_written_to_error_message()
        {
            var rule = Validate
                .That<int>(i => i < 42.As("value"))
                .WithMessage("must be less than {value}");
            var plan = new ValidationPlan<int> { rule };
            var report = plan.Execute(88);

            Assert.AreEqual("must be less than 42", report.Failures.First().Message);
        }

        [Test]
        public virtual void Validation_parameters_can_be_transformed_and_then_written_to_error_message()
        {
            var rule = Validate
                .That<int>(i => i < 42.As("value", j => "forty-two"))
                .WithMessage("must be less than {value}");
            var plan = new ValidationPlan<int> { rule };
            var report = plan.Execute(88);

            Assert.AreEqual("must be less than forty-two", report.Failures.First().Message);
        }

        [Test]
        public virtual void Multiple_validation_parameters_can_be_written_to_error_message_with_params_out_of_order()
        {
            var rule = Validate
                .That<int>(i => i < 365.As("end") & i > 0.As("start"))
                .WithMessage("must be between {start} and {end}");
            var plan = new ValidationPlan<int> { rule };
            var report = plan.Execute(9785);

            var failure = report.Failures.First();
            Assert.AreEqual(2, failure.Parameters.Count);
            Assert.AreEqual("must be between 0 and 365", failure.Message);
        }

        [Test]
        public virtual void Multiple_validation_parameters_can_be_written_to_error_message_with_params_in_order()
        {
            var rule = Validate
                .That<int>(i => i > 0.As("start") & i < 365.As("end"))
                .WithMessage("must be between {start} and {end}");
            var plan = new ValidationPlan<int> { rule };
            var report = plan.Execute(9785);

            Assert.AreEqual("must be between 0 and 365", report.Failures.First().Message);
        }

        [Test]
        public virtual void Parameters_can_be_written_to_message_nested_validations()
        {
            var cat = new Species("cat");
            cat.Individuals.Add(new Individual { Name = "Felix", Species = cat });
            cat.Individuals.Add(new Individual { Name = "Garfield", Species = cat });
            var toto = new Individual { Name = "Toto", Species = new Species("dog") };
            cat.Individuals.Add(toto);
            cat.Individuals.Add(new Individual { Name = "The Cheshire Cat", Species = cat });

            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(
                    s => s.Individuals.Every(
                        Validate.That<Individual>(
                            i => i.Species.As("individualsSpecies") == s.As("expectedSpecies"))
                            .WithMessage(
                                "Expected all {expectedSpecies}s but found a {individualsSpecies}")
                            .Check
                             ))
            };

            var failure = plan.Execute(cat).Failures.FirstOrDefault(f => f.Target == toto);
            Assert.IsNotNull(failure);
            Assert.AreEqual("Expected all cats but found a dog", failure.Message);
        }

        [Test]
        public virtual void Parameters_are_retained_by_failure_after_scope_exits()
        {
            var plan = new ValidationPlan<Species>
            {
                Validate
                    .That<Species>(s => s.Name.As("name") == "Canis lupus")
            };

            var report = plan.Execute(new Species("Felis silvestris"));
            var failure = report.Failures.First();

            Assert.That(failure.Parameters.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public virtual void Parameters_are_retained_by_failure_after_nested_scope_exits()
        {
            var plan = new ValidationPlan<Species>
            {
                new ValidationPlan<Species>
                {
                    Validate
                        .That<Species>(s => s.Name.As("name") == "Canis lupus")
                        .WithErrorCode("error-code")
                }
            };

            var report = plan.Execute(new Species("Felis silvestris"));
            var failure = report.Failures.First(f => f.ErrorCode == "error-code");

            Assert.That(failure.Parameters.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public virtual void Parameters_are_collected_from_nested_scopes()
        {
            var plan = new ValidationPlan<Species>
            {
                new ValidationPlan<Species>
                {
                    Validate
                        .That<Species>(s => s.Name.As("name") == "Canis lupus")
                        .WithErrorCode("error-code")
                }
            };

            var report = plan.Execute(new Species("Felis silvestris"));
            var failure = report.Failures.First(f => f.ErrorCode == "error-code");

            Assert.That(failure.Parameters.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public virtual void Parameters_can_be_written_to_message_nested_validations_multiple_failures()
        {
            var cat = new Species("cat");
            cat.Individuals.Add(new Individual { Name = "Felix", Species = cat });
            cat.Individuals.Add(new Individual { Name = "Garfield", Species = cat });
            var toto = new Individual { Name = "Toto", Species = new Species("dog") };
            cat.Individuals.Add(toto);
            cat.Individuals.Add(new Individual { Name = "The Cheshire Cat", Species = cat });
            var bambi = new Individual { Name = "Bambi", Species = new Species("deer") };
            cat.Individuals.Add(bambi);

            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(
                    s => s.Individuals.Every(
                        Validate.That<Individual>(
                            i => i.Species.As("individualsSpecies") == s.As("expectedSpecies"))
                            .WithMessage("Expected all {expectedSpecies}s but found a {individualsSpecies}")
                            .Check))
            };

            var report = plan.Execute(cat);
            Assert.AreEqual(1, report.Failures.Where(f => f.Message == "Expected all cats but found a dog").Count());
            Assert.AreEqual(1, report.Failures.Where(f => f.Message == "Expected all cats but found a deer").Count());
        }

        [Test]
        public virtual void Parameters_are_flushed_after_rule_evaluation_when_rule_passes()
        {
            var plan = new ValidationPlan<string>
            {
                Validate.That<string>(s => true.As("pass")),
                Validate.That<string>(s => false.As("fail"))
            };

            var report = plan.Execute("");

            Assert.IsFalse(report.Failures.First().Parameters.ContainsKey("pass"));
        }

        [Test]
        public virtual void IValidationRule_when_check_passes_no_failures_are_added_to_report()
        {
            var rule = new Mock<IValidationRule<string>>();
            rule.Setup(r => r.Check(It.IsAny<string>())).Returns(true);

            var plan = new ValidationPlan<string> { rule.Object };

            var report = plan.Execute("");

            Assert.AreEqual(0, report.Failures.Count());
        }

        [Test]
        public virtual void IValidationRule_when_check_fails_failures_are_added_to_report()
        {
            var rule = new Mock<IValidationRule<string>>();
            rule.Setup(r => r.Check(It.IsAny<string>())).Returns(false);

            var plan = new ValidationPlan<string> { rule.Object };

            var report = plan.Execute("");

            Assert.AreEqual(1, report.Failures.Count());
            Assert.IsTrue(report.Failures.First().Rule == rule.Object);
        }

        [Test]
        public void Composability_via_execution_stack()
        {
            var isAGoodNameForADog = Validate.That<string>(
                s => new[] { "Fido", "Rex", "Bowser" }
                         .As("good-names", names => string.Join(", ", names))
                         .Contains(s.As("name")))
                .WithErrorMessage("{name} is not a good name for a dog. Please choose from {good-names}");

            var plan = new ValidationPlan<Individual>
            {
                Validate.That<Individual>(s => isAGoodNameForADog.Check(s.Name))
                    .ForMember(s => s.Name)
            };

            var report = plan.Execute(new Individual { Name = "Fluffy" });

            var failures = report.Failures.Where(f => !string.IsNullOrWhiteSpace(f.Message));
            Assert.That(failures.Count(), Is.EqualTo(1));
            Assert.That(failures.Single().Message == "Fluffy is not a good name for a dog. Please choose from Fido, Rex, Bowser");
            Assert.That(failures.Single().MemberPath, Is.EqualTo("Name"));
        }

        [Test]
        public virtual void Parameter_formatting_can_be_specified_using_colon_notation_for_dates()
        {
            var date = DateTime.Now;
            var dateStr = date.ToString("D");

            var parameters = new Dictionary<string, object>
            {
                { "today", date }
            };

            Assert.AreEqual(
                "Today is " + dateStr,
                MessageGenerator.Detokenize("Today is {today:D}", parameters));
        }

        [Test]
        public virtual void ValidationReport_contains_rules_that_were_called()
        {
            var plan = new ValidationPlan<string>();
            for (var i = 0; i < 10; i++)
            {
                var rule = new Mock<IValidationRule<string>>();
                rule.Setup(r => r.Check(It.IsAny<string>()))
                    .Returns(true);
                plan.AddRule(rule.Object);
            }

            var report = plan.Execute(string.Empty);

            Assert.AreEqual(10, report.RulesExecuted.Count());
            foreach (IValidationRule<string> rule in report.RulesExecuted)
            {
                Mock.Get(rule).VerifyAll();
            }
        }

        [Test]
        public virtual void ValidationReport_contains_nested_rules_that_were_called()
        {
            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Individuals.Every(
                    Validate.That<Individual>(ind => ind.Name != null)))
            };

            var species = new Species
            {
                Individuals =
                    Enumerable.Range(1, 10).Select(_ => new Individual()).ToList()
            };
            var report = plan.Execute(species);

            Assert.AreEqual(1 + species.Individuals.Count(), report.RulesExecuted.Count());
        }

        [Test]
        public virtual void ValidationReport_contains_referenced_rules_that_were_called()
        {
            var hasName = Validate.That<Individual>(ind => ind.Name != null);
            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Individuals.Every(hasName))
            };

            var report = plan.Execute(new Species
            {
                Individuals =
                    Enumerable.Range(1, 10).Select(_ => new Individual()).ToList()
            });

            Assert.AreEqual(11, report.RulesExecuted.Count());
        }

        [Test]
        public void ValidationReport_contains_all_rule_evaluations()
        {
            var hasName = Validate.That<Individual>(ind => ind.Name != null);
            var plan = new ValidationPlan<Species>
            {
                Validate.That<Species>(s => s.Individuals.Every(hasName))
            };

            var report = plan.Execute(new Species
            {
                Individuals =
                    Enumerable.Range(1, 10).Select(_ => new Individual()).ToList()
            });

            Assert.AreEqual(11, report.Evaluations.Count());
        }

        [Test]
        public void Successful_rule_evaluations_have_informative_messages_containing_templated_values()
        {
            var hasIndividuals = Validate.That<Species>(s => s.Individuals.Count().As("count") > 0)
                .WithMessage("Species has {count} individuals.");
            var bunnies = new Species
            { Individuals = Enumerable.Range(1, 9000).Select(_ => new Individual()).ToList() };

            var report = hasIndividuals.Execute(bunnies);

            var successfulEvaluation = report.Evaluations.Single();
            StringAssert.Contains("9000", successfulEvaluation.Message);
        }

        [Test]
        public void Successful_rule_evaluations_have_accessible_parameters()
        {
            var hasIndividuals = Validate
                .That<Species>(s => s.As("name", ss => ss.Name).Individuals.Count().As("count") > 0)
                .WithMessage("Species has {count} individuals.");
            var bunnies = new Species
            {
                Name = "bunny",
                Individuals = Enumerable.Range(1, 9000).Select(_ => new Individual()).ToList()
            };

            var bunniesReport = hasIndividuals.Execute(bunnies);

            var evaluation = bunniesReport.Evaluations.Single();

            Assert.AreEqual("bunny", evaluation.Parameters["name"]);
            Assert.AreEqual(9000, evaluation.Parameters["count"]);
        }

        [Test]
        public virtual void New_ValidationFailure_parameters_property_is_not_null()
        {
            var failure = new FailedEvaluation();
            Assert.IsNotNull(failure.Parameters);
        }

        [Test]
        public void ValidationReport_Failures_does_not_report_internal_failures()
        {
            var report = new ValidationReport(new[] { new FailedEvaluation { IsInternal = true } });

            Assert.That(report.Evaluations.Count(), Is.EqualTo(0));
        }

        [Test]
        public void ValidationReport_Evaluations_does_not_report_internal_failures()
        {
            var report = new ValidationReport(new[] { new FailedEvaluation { IsInternal = true } });

            Assert.That(report.Evaluations.Count(), Is.EqualTo(0));
        }

        [Test]
        public void ValidationReport_SuccessfulEvaluations_does_not_report_failures()
        {
            var report = new ValidationReport(new RuleEvaluation[]
            {
                new SuccessfulEvaluation(),
                new FailedEvaluation()
            });

            Assert.AreEqual(1, report.Successes.Count());
        }

        [NUnit.Framework.Ignore("Scenario is under development")]
        [Test]
        public void Validation_results_can_be_displayed_hierarchically_based_on_rule_execution_ordering_and_nesting()
        {
            // TODO (Validation_results_can_be_displayed_hierarchically_based_on_rule_ex/ecution_ordering_and_nesting) write test
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public virtual void ValidationReport_Evaluations_does_not_report_internal_successes()
        {
            var report = new ValidationReport(new[] { new SuccessfulEvaluation { IsInternal = true } });

            Assert.That(report.Evaluations.Count(), Is.EqualTo(0));
        }

        [NUnit.Framework.Ignore("Fails, but may be a valid design change")]
        [Test]
        public void Evaluations_having_an_undetokenized_message_template_do_not_have_a_message()
        {
            var notExtinct = Validate.That<Species>(s => !s.As("species", sp => sp.Name).IsExtinct);
            var makesAGoodPet = Validate.That<IEnumerable<Species>>(ss => ss.Every(notExtinct))
                .WithErrorMessage("A {species} would make a terrible pet!");

            var mammoth = new Species { Name = "Mammuthua primigenius", IsExtinct = true };

            var report = new ValidationPlan<IEnumerable<Species>>
            {
                makesAGoodPet
            }.Execute(new[] { mammoth });

            Console.WriteLine(report);

            Assert.That(!report.Failures.Any(f => f.Message.Contains("{") || f.Message.Contains("}")));
        }
    }
}