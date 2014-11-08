// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Its.Validation.UnitTests
{
    public static class TestExtensions
    {
        public static bool IsSameSequenceAs<T>(
            this IEnumerable<IValidationRule<T>> actual,
            IEnumerable<IValidationRule<T>> expected)
        {
            var actualArray = actual.ToArray();
            var expectedArray = expected.ToArray();

            try
            {
                Assert.That(actualArray.Length, Is.EqualTo(expectedArray.Length));

                for (var i = 0; i < actualArray.Length; i++)
                {
                    Console.Write("Evaluating item at index " + i + ": ");

                    if (!ValidationRule<T>.ValidationRuleComparer.Instance.Equals(
                        actualArray[i],
                        expectedArray[i]))
                    {
                        Assert.Fail(string.Format("Expected: {0}\nbut was: {1}", expectedArray[i], actualArray[i]));
                    }

                    Console.WriteLine("good (" + actualArray[i] + ")");
                }
            }
            finally
            {
                WriteToConsole(actualArray);
            }

            return true;
        }

        public static void WriteToConsole<T>(IValidationRule<T>[] rules, string actualSequence = "Actual sequence:")
        {
            Console.WriteLine(actualSequence);
            foreach (var rule in rules)
            {
                Console.WriteLine(rule);
            }
        }
    }
}