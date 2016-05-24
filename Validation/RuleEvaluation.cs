// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Its.Validation
{
    /// <summary>
    ///   The result of a single evaluation of an object against a rule.
    /// </summary>
    public abstract class RuleEvaluation
    {
        private IDictionary<string, object> parameters;
        private IValidationMessageGenerator messageGenerator;
        private string message;
        private string memberPath;
        private readonly IEnumerable<IValidationRule> ruleStack;

        /// <summary>
        ///   Initializes a new instance of the <see cref="FailedEvaluation" /> class.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <param name="rule"> The rule. </param>
        /// <param name="messageGenerator"> </param>
        protected RuleEvaluation(object target = null, IValidationRule rule = null, IValidationMessageGenerator messageGenerator = null)
        {
            Rule = rule;
            Target = target;
            this.messageGenerator = messageGenerator;

            var scope = ValidationScope.Current;
            if (scope != null)
            {
                ruleStack = scope.Rules;
            }
        }

        /// <summary>
        ///   Gets the parameters relevant to the rule evaluation.
        /// </summary>
        public IDictionary<string, object> Parameters
        {
            get
            {
                return parameters ?? (parameters = new Dictionary<string, object>());
            }
            internal set
            {
                parameters = value;
            }
        }

        /// <summary>
        ///   Gets or sets the rule against which the validation check failed.
        /// </summary>
        /// <value> The rule against which the validation check failed. </value>
        public IValidationRule Rule { get; }

        /// <summary>
        ///   Gets or sets the target of the validation check.
        /// </summary>
        /// <value> The target of the validation check. </value>
        public object Target { get; private set; }

        /// <summary>
        ///   Gets a message describing the validation failure.
        /// </summary>
        /// <value> A message describing the validation failure. </value>
        public string Message
        {
            get
            {
                return message ?? (message = MessageGenerator.GetMessage(this));
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        ///   Gets or sets the message generator.
        /// </summary>
        /// <value> The message generator. </value>
        public IValidationMessageGenerator MessageGenerator
        {
            get
            {
                return messageGenerator ??
                       Result<IValidationMessageGenerator>() ??
                       Validation.MessageGenerator.Current;
            }
            set
            {
                messageGenerator = value;
            }
        }

        /// <summary>
        ///   Gets the message template for the rule evaluation.
        /// </summary>
        /// <remarks>
        ///   This template may contain tokens intended to be filled in with parameters collected during the rule evaluation.
        /// </remarks>
        public virtual string MessageTemplate => Result<MessageTemplate, string>(template => template.GetMessage(this),
                                                                                 orElse: () => string.Empty);

        /// <summary>
        ///   Gets the member path.
        /// </summary>
        /// <value> The member path. </value>
        public string MemberPath
        {
            get
            {
                return memberPath ??
                       (memberPath = Result<MemberPath, string>(path => path.Value, orElse: () => string.Empty));
            }
            set
            {
                memberPath = value;
            }
        }

        /// <summary>
        ///   Allows access to rule metadata.
        /// </summary>
        /// <typeparam name="T"> The type of the metadata </typeparam>
        /// <returns> An instance of the specified type <typeparamref name="T" /> , or null if it has not been defined on this rule or in this rules <see
        ///    cref="ValidationScope" /> . </returns>
        public T Result<T>()
        {
            T value = default(T);

            if (Rule != null)
            {
                value = Rule.Result<T>();
            }

            return value != null
                       ? value
                       : ResultFromRuleStack<T>();
        }

        internal IEnumerable<IValidationRule> RuleStack => ruleStack;

        internal bool IsInternal { get; set; }

        protected TReturn Result<TExtension, TReturn>(Func<TExtension, TReturn> ifWasSet, Func<TReturn> orElse)
            where TExtension : class
        {
            TExtension value;
            if (Rule != null)
            {
                value = Result<TExtension>();
                if (value != null)
                {
                    return ifWasSet(value);
                }
            }

            value = ResultFromRuleStack<TExtension>();

            return value != null
                       ? ifWasSet(value)
                       : orElse();
        }

        private TExtension ResultFromRuleStack<TExtension>()
        {
            if (ruleStack == null)
            {
                return default(TExtension);
            }

            return ruleStack
                .Select(r => r.Result<TExtension>())
                .FirstOrDefault(e => e != null);
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns> A <see cref="System.String" /> that represents this instance. </returns>
        public override string ToString() => Message;
    }
}