// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Its.Validation
{
    internal class AsyncValidationRule<TTarget> : ValidationRule<TTarget>
    {
        private readonly Func<TTarget, ValidationScope, Task<bool>> taskFactory;

        public AsyncValidationRule(Func<TTarget, ValidationScope, Task<bool>> taskFactory)
        {
            if (taskFactory == null)
            {
                throw new ArgumentNullException("taskFactory");
            }
            this.taskFactory = taskFactory;
        }

        protected internal AsyncValidationRule(AsyncValidationRule<TTarget> fromRule)
        {
            taskFactory = fromRule.taskFactory;
            if (fromRule.preconditions != null)
            {
                preconditions = new List<IValidationRule<TTarget>>(fromRule.preconditions);
            }
            if (fromRule.extensions != null)
            {
                extensions = new Dictionary<Type, object>(fromRule.extensions);
            }
        }

        protected internal AsyncValidationRule(
            Func<TTarget, Task<TTarget>> setup,
            Func<TTarget, bool> validate)
        {
            taskFactory = CreateTask(setup, validate);
        }

        private Func<TTarget, ValidationScope, Task<bool>> CreateTask(Func<TTarget, Task<TTarget>> setup, Func<TTarget, bool> validate)
        {
            return (target, parentScope) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                var task = setup(target);
                task.ContinueWith(
                    t => tcs.Complete(t,
                                      () =>
                                      {
                                          using (var scope = new ValidationScope(parentScope))
                                          {
                                              var result = validate(t.Result);
                                              RecordResult(scope, result, target);
                                              return result;
                                          }
                                      }));
                return tcs.Task;
            };
        }

        private void RecordResult(ValidationScope scope, bool result, TTarget target)
        {
            var parameters = scope.FlushParameters();

            var messageGenerator = scope.MessageGenerator;

            if (result)
            {
                scope.AddEvaluation(new SuccessfulEvaluation(target, this)
                {
                    MessageGenerator = messageGenerator,
                    Parameters = parameters
                });
            }
            else
            {
                scope.AddEvaluation(new FailedEvaluation(target, this)
                {
                    MessageGenerator = messageGenerator,
                    Parameters = parameters
                });
            }
        }

        public Task<bool> TaskFor(TTarget target, ValidationScope scope)
        {
            var tcs = new TaskCompletionSource<bool>();
            Task.Factory
                .StartNew(() =>
                {
                    // TODO: (TaskFor) prevent setup from running if a precondition failed. IsPreconditionUnsatisfied may not be appropriate for async since it forces evaluation when it finds an unevaluated precondition.
                    // if (CheckPreconditions(scope, target))
                    // {
                    //     tcs.SetCanceled();
                    //     return;
                    // }

                    taskFactory(target, scope).TryStart();
                });

            return tcs.Task;
        }

        /// <summary>
        ///   Determines (synchronously) whether the specified target is valid.
        /// </summary>
        /// <param name="target"> The object to be validated. </param>
        /// <param name="scope"> The <see cref="ValidationScope" /> in which to perform the validation. </param>
        /// <returns> <c>true</c> if the specified target is valid; otherwise, <c>false</c> . </returns>
        public override bool Check(TTarget target, ValidationScope scope)
        {
            var task = TaskFor(target, scope).TryStart();
            if (task.Status == TaskStatus.WaitingForActivation)
            {
                throw new InvalidOperationException("The rule cannot be evaluated because its task is dependent on another task that has not been started.");
            }
            return task.Result;
        }

        protected internal override ValidationRule<TTarget> Clone()
        {
            var clone = new AsyncValidationRule<TTarget>(this);
            return clone;
        }
    }
}