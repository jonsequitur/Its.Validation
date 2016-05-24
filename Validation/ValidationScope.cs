// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Its.Validation
{
    /// <summary>
    ///   Defines a unit of work for a validation or set of validations.
    /// </summary>
    public class ValidationScope : IDisposable
    {
        /// <summary>
        ///   Occurs when a rule evaluated on the scope or in one of its child scopes.
        /// </summary>
        public event RuleEvaluatedEventHandler RuleEvaluated;

        /// <summary>
        ///   The parent scope of the current scope.
        /// </summary>
        private readonly ValidationScope parent;

        /// <summary>
        ///   The rules executed.
        /// </summary>
        private readonly ConcurrentBag<RuleEvaluation> evaluations;

        private bool? haltsOnFirstFailure;

        /// <summary>
        ///   The parameters.
        /// </summary>
        private Lazy<ConcurrentDictionary<string, object>> parameters = new Lazy<ConcurrentDictionary<string, object>>(() => new ConcurrentDictionary<string, object>());

        private readonly object lockObj = new object();
        private bool disposed = false;
        private IValidationRule rule;
        private IValidationRule[] ruleStack;

        /// <summary>
        ///   A stack containing the active scopes in the order they were opened
        /// </summary>
        // TODO: (ValidationScope) use CallContext
        [ThreadStatic]
        private static Stack<ValidationScope> activeScopes;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationScope" /> class.
        /// </summary>
        public ValidationScope(bool enter = true)
        {
            if (activeScopes != null && activeScopes.Count > 0)
            {
                parent = ActiveScopes.Peek();
                evaluations = parent.evaluations;
            }
            else
            {
                evaluations = new ConcurrentBag<RuleEvaluation>();
            }

            if (enter)
            {
                EnterScope(this);
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ValidationScope" /> class.
        /// </summary>
        /// <param name="parent"> The parent. </param>
        public ValidationScope(ValidationScope parent)
        {
            this.parent = parent;
            evaluations = parent.evaluations;
            EnterScope(this);
        }

        internal IValidationRule Rule
        {
            get
            {
                return rule;
            }
            set
            {
                rule = value;
                ruleStack = Scopes().Select(scope => scope.Rule).Where(r => r != null).Distinct().ToArray();
            }
        }

        internal ValidationScope Parent => parent;

        internal IValidationRule[] Rules => ruleStack;

        private IEnumerable<ValidationScope> Scopes()
        {
            yield return this;
            if (parent != null)
            {
                foreach (var scope in parent.Scopes())
                {
                    yield return scope;
                }
            }
        }

        /// <summary>
        ///   Gets or setsthe MessageGenerator to be used during the current validation execution.
        /// </summary>
        internal IValidationMessageGenerator MessageGenerator { get; set; }

        /// <summary>
        ///   Gets the active scopes.
        /// </summary>
        /// <value> The active scopes. </value>
        private static Stack<ValidationScope> ActiveScopes
        {
            get
            {
                if (activeScopes == null)
                {
                    activeScopes = new Stack<ValidationScope>();

                    // TODO: (ActiveScopes) something like this:
                    // activeScopes = (Stack<ValidationScope>) CallContext.LogicalGetData("__Its_Validation_ValidationScopes__");
                }

                return activeScopes;
            }
        }

        /// <summary>
        ///   Gets Current.
        /// </summary>
        public static ValidationScope Current
        {
            get
            {
                if (activeScopes == null || ActiveScopes.Count == 0)
                {
                    return null;
                }

                return ActiveScopes.Peek();
            }
        }

        /// <summary>
        ///   Enters the specified <paramref name="scope" /> .
        /// </summary>
        /// <param name="scope"> The scope. </param>
        protected static void EnterScope(ValidationScope scope)
        {
            ActiveScopes.Push(scope);
        }

        /// <summary>
        ///   Exit the specified <paramref name="scope" /> .
        /// </summary>
        /// <remarks>
        ///   If there are nested scopes within the specified <paramref name="scope" /> , those will be exited as well.
        /// </remarks>
        /// <param name="scope"> The scope to exit </param>
        protected static void ExitScope(ValidationScope scope)
        {
            if (!ActiveScopes.Contains(scope))
            {
                return;
            }

            ValidationScope popped;
            do
            {
                popped = ActiveScopes.Pop();
            } while (scope != popped);
        }

        /// <summary>
        ///   Gets the evaluations that have taken place in the current scope, including its child scopes.
        /// </summary>
        public IEnumerable<RuleEvaluation> Evaluations => evaluations.NonInternal();

        /// <summary>
        ///   Gets the <see cref="FailedEvaluation" /> s collected within the current scope and all child scopes.
        /// </summary>
        /// <value> The <see cref="FailedEvaluation" /> s resulting from the execution of all rules within the current validation operation. </value>
        public IEnumerable<FailedEvaluation> Failures => AllFailures.Where(f => !f.IsInternal);

        /// <summary>
        ///   Gets all <see cref="FailedEvaluation" /> s collected within the current scope and all child scopes, including those marked as internal.
        /// </summary>
        internal IEnumerable<FailedEvaluation> AllFailures => evaluations.OfType<FailedEvaluation>();

        /// <summary>
        ///   Gets the <see cref="SuccessfulEvaluation" /> s collected within the current scope and all child scopes.
        /// </summary>
        public IEnumerable<SuccessfulEvaluation> Successes => evaluations.OfType<SuccessfulEvaluation>();

        internal bool HaltsOnFirstFailure
        {
            get
            {
                return haltsOnFirstFailure == true || Scopes().Any(s => s.haltsOnFirstFailure == true);
            }
            set
            {
                haltsOnFirstFailure = value;
            }
        }

        internal bool ShouldHalt() => HaltsOnFirstFailure && Failures.Any();

        /// <summary>
        ///   The dispose.
        /// </summary>
        public void Dispose()
        {
            disposed = true;
            ExitScope(this);
        }

        internal virtual void AddParameter(string key, object value)
        {
            if (disposed)
            {
                return;
            }

            parameters.Value[key] = value;
        }

        internal virtual void AddEvaluation(RuleEvaluation evaluation)
        {
            if (disposed)
            {
                return;
            }

            evaluations.Add(evaluation);

            NotifyRuleEvaluated(evaluation);
        }

        private void NotifyRuleEvaluated(RuleEvaluation evaluation)
        {
            RuleEvaluated?.Invoke(this, new RuleEvaluatedEventArgs(evaluation));

            parent?.NotifyRuleEvaluated(evaluation);
        }

        internal bool HasFailed(object target, IValidationRule ruleTemp) => 
            AllFailures.Any(f => !f.IsInternal && Equals(ruleTemp, f.Rule) && Equals(target, f.Target));

        /// <summary>
        ///   The flush parameters.
        /// </summary>
        internal virtual IDictionary<string, object> FlushParameters()
        {
            if (!parameters.IsValueCreated)
            {
                return null;
            }

            // TODO: (FlushParameters) do this without a lock
            lock (lockObj)
            {
                var parametersCopy = parameters.Value;
                parameters = new Lazy<ConcurrentDictionary<string, object>>(() => new ConcurrentDictionary<string, object>());
                return parametersCopy;
            }
        }

#if DEBUG
        private static readonly DebugMessageGenerator debugMessageGenerator = new DebugMessageGenerator();

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            // parent scopes
            if (parent != null)
            {
                sb.AppendLine($"[parent: {parent}]");
            }

            // self
            var messages = evaluations.Select(e => debugMessageGenerator.GetMessage(e));
            sb.Append(string.Join(" / ", messages));
            sb.Append(" (" + GetHashCode());
            if (disposed)
            {
                sb.Append(", disposed");
            }
            sb.Append(")");
            return sb.ToString();
        }
#endif
    }
}