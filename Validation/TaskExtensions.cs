// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Its.Validation
{
    /// <summary>
    ///   Supports asynchronous rule evaluation.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        ///   Starts a validation asynchronously and returns a Task for the operation.
        /// </summary>
        /// <typeparam name="TTarget"> The type of the object to validate. </typeparam>
        /// <param name="plan"> The validation plan to execute asynchronously. </param>
        /// <param name="target"> The type of the object to validate. </param>
        /// <returns> </returns>
        public static Task<ValidationReport> ExecuteAsync<TTarget>(
            this ValidationPlan<TTarget> plan,
            TTarget target)
        {
            var tcs = new TaskCompletionSource<ValidationReport>(target);
            var scope = new ValidationScope(enter: false);
            Task.Factory.StartNew(() =>
            {
                var taskDictionary =
                    new ConcurrentDictionary<IValidationRule<TTarget>, Task<bool>>();

                // create and start a task for each rule
                plan.Select(
                    rule => taskDictionary.GetOrAdd(rule,
                                                    rule.ToTask(target,
                                                                taskDictionary,
                                                                scope,
                                                                TaskCreationOptions.AttachedToParent)))
                    .ForEach(t => t.TryStart());
            })
                .ContinueWith(t =>
                {
                    try
                    {
                        tcs.Complete(t, () => ValidationReport.FromScope(scope));
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                });
            return tcs.Task;
        }

        private static Task<bool> ToTask<T>(
            this IValidationRule<T> forRule,
            T target,
            ConcurrentDictionary<IValidationRule<T>, Task<bool>> tasks,
            ValidationScope scope,
            TaskCreationOptions options = TaskCreationOptions.PreferFairness)
        {
            // if the rule is inherently capable of async, let it build the task
            var asyncRule = forRule as AsyncValidationRule<T>;
            if (asyncRule != null)
            {
                return asyncRule.TaskFor(target, scope);
            }

            // otherwise, build a task from its tree
            var task = new Task<bool>(
                () =>
                {
                    var rule = forRule as ValidationRule<T>;

                    // there are preconditions, so each will need its own task, and main task must wait on them all
                    rule?.preconditions
                         .ForEach(pre => tasks.GetOrAdd(rule,
                                                        _ => pre.ToTask(target,
                                                                        tasks,
                                                                        scope,
                                                                        TaskCreationOptions.AttachedToParent)));

                    using (var innerScope = new ValidationScope(scope))
                    {
                        return forRule.Check(target, innerScope);
                    }
                }, options);

            return task;
        }

        internal static TTask TryStart<TTask>(this TTask task)
            where TTask : Task
        {
            if (task.Status == TaskStatus.Created)
            {
                task.Start();
            }
            return task;
        }

        internal static void Complete<TAntecedent, TResult>(this TaskCompletionSource<TResult> tcs, Task<TAntecedent> t, Func<TAntecedent, TResult> validate)
        {
            if (t.IsFaulted)
            {
                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions);
                }
            }
            else if (t.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            {
                tcs.TrySetResult(validate(t.Result));
            }
        }

        internal static void Complete<TResult>(this TaskCompletionSource<TResult> tcs, Task t, Func<TResult> result)
        {
            if (t.IsFaulted)
            {
                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions);
                }
            }
            else if (t.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            {
                tcs.TrySetResult(result());
            }
        }
    }
}