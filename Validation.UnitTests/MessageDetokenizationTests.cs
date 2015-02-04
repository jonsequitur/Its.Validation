// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestFixture]
    public class MessageDetokenizationTests
    {
        [Test]
        public virtual void Detokenize_does_not_throw_on_null_parameters_dictionary()
        {
            Assert.DoesNotThrow(() => MessageGenerator.Detokenize(string.Empty, null));
        }

        [Test]
        public virtual void Detokenize_returns_empty_string_on_null_message_template()
        {
            Assert.AreEqual(string.Empty, MessageGenerator.Detokenize(null, new Dictionary<string, object>()));
        }

        [Test]
        public virtual void Detokenize_returns_empty_string_on_empty_string_message_template()
        {
            Assert.AreEqual(string.Empty,
                            MessageGenerator.Detokenize(string.Empty, new Dictionary<string, object>()));
        }

        [NUnit.Framework.Ignore("Fails, but may be a valid design change")]
        [Test]
        public virtual void When_parameters_are_missing_then_Detokenize_returns_empty_string()
        {
            const string messageTemplate = "This {parameter} is not available";
            Assert.That(MessageGenerator.Detokenize(messageTemplate, new Dictionary<string, object>()), Is.EqualTo(""));
        }

        [Test]
        public virtual void Detokenize_can_tokenize_null_parameter_values()
        {
            var parameters = new Dictionary<string, object>
            {
                { "null", null },
                { "notnull", "not null" }
            };

            Assert.AreEqual(
                "This is  and that is not null",
                MessageGenerator.Detokenize("This is {null} and that is {notnull}", parameters));
        }

        [Test]
        public void MessageTemplate_returns_template_directly_if_passed_a_null_RuleEvaluation()
        {
            var templateString = "{this} and {that}";
            var messageTemplate = new MessageTemplate(templateString);

            var message = messageTemplate.GetMessage(null);

            Assert.That(message, Is.EqualTo(templateString));
        }

        [Test]
        public void FailureMessageTemplate_returns_template_directly_if_passed_a_null_RuleEvaluation()
        {
            var templateString = "{this} and {that}";
            var messageTemplate = new FailureMessageTemplate(templateString);

            var message = messageTemplate.GetMessage(null);

            Assert.That(message, Is.EqualTo(templateString));
        }

        [Test]
        public void SuccessMessageTemplate_returns_template_directly_if_passed_a_null_RuleEvaluation()
        {
            var templateString = "{this} and {that}";
            var messageTemplate = new SuccessMessageTemplate(templateString);

            var message = messageTemplate.GetMessage(null);

            Assert.That(message, Is.EqualTo(templateString));
        }

        [Test]
        public void FailureMessageTemplate_cannot_receive_null_RuleEvaluation()
        {
            var messageTemplate = new FailureMessageTemplate(e =>
            {
                Assert.That(e, Is.Not.Null);
                return "";
            });

            messageTemplate.GetMessage(null);
        }

        [Test]
        public void SuccessMessageTemplate_cannot_receive_null_RuleEvaluation()
        {
            var messageTemplate = new SuccessMessageTemplate(e =>
            {
                Assert.That(e, Is.Not.Null);
                return "";
            });

            messageTemplate.GetMessage(null);
        }
    }
}