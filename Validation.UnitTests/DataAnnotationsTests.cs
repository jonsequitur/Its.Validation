// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Its.Validation.Configuration;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestFixture]
    public class DataAnnotationsTests
    {
        [Test]
        public void ConfigureFromAttributes_returns_a_new_instance_of_the_ValidationPlan()
        {
            var plan = new ValidationPlan<Account>();
            var configuredPlan = plan.ConfigureFromAttributes();

            Assert.That(plan != configuredPlan);
        }

        [Test]
        public void FailedEvaluation_Message_is_set_from_data_annotation_ErrorMessage()
        {
            ValidationPlan<Account> plan = new ValidationPlan<Account>()
                .ConfigureFromAttributes();

            ValidationReport report = plan.Execute(new Account());

            Assert.That(report.Failures.Any(f => f.Message == "What's your name?"));
        }

        [Test]
        public void FailedEvaluation_Message_is_templated_per_normal_DataAnnotations_convention()
        {
            ValidationPlan<Account> plan = new ValidationPlan<Account>()
                .ConfigureFromAttributes();

            ValidationReport report = plan.Execute(new Account { UserName = new string('c', 200) });

            Assert.That(report.Failures.First(f => f.MemberPath == "UserName").Message,
                        Is.EqualTo("Keep that UserName between 1 and 100!"));
        }

        [Test]
        public void FailedEvaluation_Rule_is_not_null()
        {
            ValidationPlan<Account> plan = new ValidationPlan<Account>()
                .ConfigureFromAttributes();

            ValidationReport report = plan.Execute(new Account
            {
                Email = "sadjhv"
            });

            Console.WriteLine(report);

            Assert.That(report.Failures.All(f => f.Rule != null));
        }

        [Test]
        public void FailedEvaluation_target_is_set_to_the_instance_that_was_evaluated()
        {
            ValidationPlan<Account> plan = new ValidationPlan<Account>()
                .ConfigureFromAttributes();
            var account = new Account();

            ValidationReport report = plan.Execute(account);

            Assert.That(report.Failures.Any(f => f.Target == account));
        }

        [Test]
        public void Nested_validations_can_be_supported()
        {
            ValidationPlan<Account> accountValidator = new ValidationPlan<Account>()
                .ConfigureFromAttributes();
            ValidationPlan<Order> orderValidator = new ValidationPlan<Order>()
                .ConfigureFromAttributes();
            orderValidator.AddRule(o => accountValidator.Check(o.Account));
            var order = new Order { Account = new Account() };

            ValidationReport report = orderValidator.Execute(order);

            Assert.That(report.Failures.Any(f => f.Message == "What's your name?" && f.Target == order.Account));
            Assert.That(report.Failures.Any(f => f.Message == "You can't place an order for nothing." && f.Target == order));
        }
    }

    public class Account
    {
        [Required(ErrorMessage = "What's your name?")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Keep that {0} between {2} and {1}!")]
        public string UserName { get; set; }

        [EmailAddress]
        public string Email { get; set; }
    }

    public class Order
    {
        [Required]
        public Account Account { get; set; }

        [Required(ErrorMessage = "You can't place an order for nothing.")]
        public IEnumerable<object> OrderLines { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EmailAddressAttribute : DataTypeAttribute
    {
        private static readonly Regex _regex =
            new Regex(
                @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public EmailAddressAttribute()
            : base(DataType.EmailAddress)
        {
            ErrorMessage = "This is an invalid email address.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var valueAsString = value as string;
            return valueAsString != null && _regex.Match(valueAsString).Length > 0;
        }
    }
}