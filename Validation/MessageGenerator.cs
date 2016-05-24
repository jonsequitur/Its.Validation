// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Its.Validation
{
    public static class MessageGenerator
    {
        private static IValidationMessageGenerator current = new DefaultMessageGenerator();

        private static readonly Regex ParamsRegex = new Regex(
            @"{(?<key>[^{}:]*)(?:\:(?<format>.+))?}",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.CultureInvariant
#if !SILVERLIGHT
            // Silverlight does not support this option
            | RegexOptions.Compiled
#endif
            );

        /// <summary>
        ///   Gets or sets the message generator to be used for all validations that do not otherwise specify a message generator.
        /// </summary>
        /// <value> The current. </value>
        public static IValidationMessageGenerator Current
        {
            get
            {
                return current;
            }
            set
            {
                current = value ?? new DefaultMessageGenerator();
            }
        }

        /// <summary>
        ///   Detokenizes the specified message template, filling in bracketed strings with parameters.
        /// </summary>
        /// <param name="messageTemplate"> The message template. </param>
        /// <param name="parameters"> The parameters to fill into the template. </param>
        /// <returns> A string with the tokens replaced with values from the supplied dictionary. </returns>
        /// <remarks>
        ///   The tokens should be surrouned by single curly braces, e.g. "The password must contain only the characters {allowed-characters}."
        /// </remarks>
        public static string Detokenize(string messageTemplate, IDictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(messageTemplate))
            {
                return string.Empty;
            }
            if (parameters == null)
            {
                return messageTemplate;
            }

            var formattedMsg = new StringBuilder(messageTemplate);

            var matches = ParamsRegex.Matches(messageTemplate);
            foreach (Match match in matches)
            {
                var tokenName = match.Groups["key"].Captures[0].Value;
                var replacementTarget = match.Value;
                object paramValue;
                if (parameters.TryGetValue(tokenName, out paramValue))
                {
                    var formatStr = match.Groups["format"].Success ? match.Groups["format"].Captures[0].Value : null;
                    string formattedParam = null;
                    if (!string.IsNullOrEmpty(formatStr))
                    {
                        var formattableParamValue = paramValue as IFormattable;
                        if (formattableParamValue != null)
                        {
                            formattedParam = formattableParamValue.ToString(formatStr, CultureInfo.CurrentCulture);
                        }
                    }

                    if (formattedParam == null)
                    {
                        formattedParam = paramValue.Format();
                    }

                    formattedMsg.Replace(replacementTarget, formattedParam);
                }
            }

            return formattedMsg.ToString();
        }

        public static string Format<T>(this T objectToFormat) => objectToFormat?.ToString() ?? "";
    }
}