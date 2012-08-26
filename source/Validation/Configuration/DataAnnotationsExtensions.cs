// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Its.Validation.Configuration
{
    /// <summary>
    /// Provides DataAnnotations support.
    /// </summary>
    public static class DataAnnotationsExtensions
    {
        /// <summary>
        ///   Adds validatiom rules for any DataAnnotations attributes on the validated type.
        /// </summary>
        /// <typeparam name="T"> The type to be validated. </typeparam>
        /// <param name="plan"> The validation plan to which to add DataAnnotations validation rules. </param>
        /// <returns> </returns>
        public static ValidationPlan<T> ConfigureFromAttributes<T>(this ValidationPlan<T> plan)
        {
            ValidationRule<T> rule = null;
            rule = Validate.That<T>(t =>
            {
                var context = new ValidationContext(t, null, null);
                var results = new List<ValidationResult>();
                var isValid = Validator.TryValidateObject(t, context, results, true);

                // add results to the validation scope
                results.ForEach(result =>
                {
                    result
                        .MemberNames
                        .ForEach(member =>
                                 ValidationScope.Current.AddEvaluation(
                                     new FailedEvaluation(
                                         target: t,
                                         rule: rule)
                                     {
                                         Message = result.ErrorMessage,
                                         MemberPath = member
                                     }));
                });

                return isValid;
            });

            var clone = (ValidationPlan<T>) plan.Clone();
            clone.AddRule(rule);
            return clone;
        }
    }
}