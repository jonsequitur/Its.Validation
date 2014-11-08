// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Linq;
using Its.Validation.Configuration;
using Its.Validation.UnitTests.TestClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestClass, TestFixture]
    public class DiagnosticsTests
    {
        [Test, TestMethod]
        public virtual void When_using_DebugMessageGenerator_extension_values_are_written_to_message()
        {
            var plan = new ValidationPlan<Species>(new DebugMessageGenerator())
            {
                Validate.That<Species>(s => false)
                    .With(new ErrorCode<string>("forty-two"))
                    .With(new ErrorCode<int>(42))
            };

            var report = plan.Execute(null);

            var msg = report.ToString();
            Console.WriteLine(msg);

            Assert.That(msg.Contains("forty-two"));
            Assert.That(msg.Contains("42"));
        }

        [Test, TestMethod]
        public virtual void When_using_DebugMessageGenerator_extension_types_are_written_to_message()
        {
            var plan = new ValidationPlan<Species>(new DebugMessageGenerator())
            {
                Validate.That<Species>(s => false)
                    .With(new ErrorCode<string>("one"))
                    .With(new ErrorCode<int>(1))
            };

            var report = plan.Execute(null);

            var msg = report.ToString();
            Console.WriteLine(msg);

            Assert.That(msg.Contains("ErrorCode"));
        }

        [Test, TestMethod]
        public virtual void DebugMessageGenerator_accepts_validation_failure_having_null_rule()
        {
            var generator = new DebugMessageGenerator();
            var failure = new FailedEvaluation(null, null, generator);
            Console.WriteLine(generator.GetMessage(failure));
        }
    }
}