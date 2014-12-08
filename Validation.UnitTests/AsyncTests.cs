// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Its.Validation.Configuration;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Its.Validation.UnitTests
{
    [TestFixture]
    public class AsyncTests
    {
        // TODO: (AsyncTests) simulate web access rather than make real calls

        private static ValidationPlan<string> isReachableUrl;
        private static ValidationPlan<string> isTwitterFreeUrl;
        private static ValidationRule<string> isValidHttpUrl;

        static AsyncTests()
        {
            InitializeReachableUrlPlan();
            InitializeTwitterPlan();
        }

        [Test]
        public void When_items_are_validated_in_parallel_the_rule_evaluations_are_reported_correctly()
        {
            DescribeThread();
            var plan = new ValidationPlan<IEnumerable<string>>();
            plan.AddRule(urls => urls.Parallel(url =>
            {
                if (string.IsNullOrWhiteSpace(Thread.CurrentThread.Name))
                {
                    Thread.CurrentThread.Name = url;
                }
                Console.WriteLine(
                    "checking {0} in scope {2} on thread {1} ", url,
                    Thread.CurrentThread.ManagedThreadId,
                    ValidationScope.Current);
                return isReachableUrl.Check(url);
            }));

            var report = plan.Execute(Urls());

            Console.WriteLine(report);

            Assert.That(report.Evaluations.Count(),
                        Is.GreaterThan(Urls().Count()));
        }

        private static void DescribeThread()
        {
            Console.WriteLine(string.Format("On thread {0} (name: {1})", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name));
        }

        [Test]
        public void
            When_the_same_plan_is_run_on_two_threads_simultaneously_they_each_report_success_and_failure_correctly()
        {
            var barrier = new Barrier(2);
            var plan = new ValidationPlan<string>();
            plan.AddRule(s =>
            {
                barrier.SignalAndWait();
                return s != "fail";
            });

            var succeed = Task<ValidationReport>.Factory.StartNew(() => plan.Execute("succeed"));
            var fail = Task<ValidationReport>.Factory.StartNew(() => plan.Execute("fail"));

            succeed.Wait();
            fail.Wait();

            Assert.That(succeed.Result.HasFailures, Is.False);
            Assert.That(fail.Result.HasFailures, Is.True);
        }

        [Test]
        public void When_the_same_plan_is_run_on_multiple_threads_simultaneously_they_each_report_parameters_correctly()
        {
            var barrier = new Barrier(20);
            var plan = new ValidationPlan<string>(new DebugMessageGenerator())
            {
                Validate.That<string>(s =>
                {
                    DescribeThread();

                    // report input parameter
                    s.As("s");
                    barrier.SignalAndWait();
                    return false;
                }).WithErrorMessage("the parameter was {s}")
            };

            var tasks =
                new ConcurrentBag<Task<ValidationReport>>(
                    Enumerable.Range(1, 20).Select(
                        i => Task<ValidationReport>.Factory.StartNew(() =>
                        {
                            var report = plan.Execute(i.ToString());
                            Console.WriteLine(report);
                            return report;
                        })));
            Task.WaitAll(tasks.ToArray());

            Enumerable.Range(1, 20).ForEach(i =>
                                            tasks.Any(
                                                t => t.Result.Failures.Any(f => f.Parameters["s"].Equals(i.ToString()))));
        }

        [Test]
        public void ToTask_converts_ValidationPlan_without_rule_preconditions_to_Task_that_reports_correct_result()
        {
            var task = isTwitterFreeUrl.ExecuteAsync("http://twitter.com");
            var report = task.Result;

            Assert.That(report.HasFailures, Is.True);
        }

        [Test]
        public void ExecuteAsync_converts_ValidationPlan_without_rule_preconditions_to_Task_that_reports_correct_result()
        {
            var task = isTwitterFreeUrl.ExecuteAsync("http://twitter.com");
            var report = task.Result;
            Console.WriteLine(report);

            Assert.That(report.HasFailures, Is.True);
        }

        [Test]
        public void ToTask_converts_ValidationPlan_without_rule_preconditions_to_Task_that_reports_correct_rule_executions()
        {
            var task = isTwitterFreeUrl.ExecuteAsync("http://twitter.com");
            var report = task.Result;

            Assert.That(report.Successes.Any(e => e.Rule == isValidHttpUrl));
            Assert.That(report.Failures.Any());
        }

        [Test]
        public void ToTask_converts_ValidationPlan_without_rule_preconditions_to_Task_that_reports_correctly_parameterized_message()
        {
            var task = isTwitterFreeUrl.ExecuteAsync("http://twitter.com");
            var report = task.Result;

            Assert.That(report.Failures.Any(f => f.Message.Contains("http://twitter.com")));
        }

        [Test]
        public void ToTask_converts_ValidationPlan_with_rule_preconditions_to_Task_that_reports_correct_rule_executions()
        {
            var task = isReachableUrl.ExecuteAsync("http://google.com/ugiuagkjbsfksjdf");
            var report = task.Result;

            Assert.That(report.Successes.Any(e => e.Rule == isValidHttpUrl));
            Assert.That(report.Failures.Any());
        }

        [Test]
        public void ToTask_converts_ValidationPlan_with_rule_preconditions_to_Task_that_reports_correctly_parameterized_message()
        {
            var task = isReachableUrl.ExecuteAsync("http://google.com/ugiuagkjbsfksjdf");
            var report = task.Result;

            Assert.That(report.Failures.Any(e => e.Message.Contains("http://google.com/ugiuagkjbsfksjdf")));
        }

        [NUnit.Framework.Ignore("In development")]
        [Test]
        public void Rules_within_a_ValidationPlan_can_use_APM_signatures_and_run_asynchronously()
        {
            var hasAwesomeTag = Validate.Async<string>(
                setup: url =>
                {
                    var request = new ApmOperation<string, string>(s =>
                                                                   Task<string>.Factory.StartNew(() => "<div>" + s + "</div>"));
                    var tcs = new TaskCompletionSource<string>();
                    Task<string>.Factory
                        .FromAsync(request.BeginInvoke,
                                   request.EndInvoke,
                                   url,
                                   state: null,
                                   creationOptions: TaskCreationOptions.AttachedToParent)
                        .ContinueWith(
                            t => tcs.Complete(t, () => t.Result));
                    return tcs.Task;
                },
                validate: html => html.As("page").Contains("<awesome>"))
                .WithErrorMessage("{page} is missing the <awesome> tag!");

            var plan = new ValidationPlan<string>
            {
                isValidHttpUrl,
                hasAwesomeTag.When(isValidHttpUrl)
            };

            var task = plan.ExecuteAsync("http://google.com");
            task.Wait();
            var result = task.Result;

            Console.WriteLine(result);
            // TODO: (Rules_within_a_ValidationPlan_can_use_APM_signatures_and_run_asynchronously) 

            Assert.That(result.Failures.Any(f => f.Message == "http://google.com is missing the <awesome> tag!"));
        }

        [Ignore("In development")]
        [Test]
        public void Continuation_rules_do_not_start_setup_tasks_when_precondition_fails()
        {
            // TODO: (Continuation_rules_do_not_start_setup_tasks_when_precondition_fails) 
            var setupStarted = false;
            var hasAwesomeTag = Validate.Async<string>(
                setup: url =>
                {
                    setupStarted = true;
                    return Task<string>.Factory.StartNew(() => "<div>not awesome</div>");
                },
                validate: html => html.As("page").Contains("<awesome>"))
                .WithErrorMessage("{page} is missing the <awesome> tag!");

            var plan = new ValidationPlan<string>
            {
                isValidHttpUrl,
                hasAwesomeTag.When(isValidHttpUrl)
            };

            var task = plan.ExecuteAsync("hhttp://bing.com");
            task.Wait();
            Console.WriteLine(task.Result);
            Assert.That(setupStarted, Is.False);
        }

        [NUnit.Framework.Ignore("In development")]
        [Test]
        public void Rules_within_a_ValidationPlan_can_use_APM_signatures_and_run_synchronously()
        {
            var hasAwesomeTag = Validate.Async<string>(
                setup: url =>
                {
                    var request = new ApmOperation<string, string>(s =>
                                                                   Task<string>.Factory.StartNew(() => "<div>" + s + "</div>"));
                    var tcs = new TaskCompletionSource<string>();
                    Task<string>.Factory
                        .FromAsync(request.BeginInvoke,
                                   request.EndInvoke,
                                   url,
                                   state: null)
                        .ContinueWith(
                            t => tcs.Complete(t, () => t.Result));
                    return tcs.Task;
                },
                validate: html => html.As("page").Contains("<awesome>"))
                .WithErrorMessage("{page} is missing the <awesome> tag!");

            var plan = new ValidationPlan<string>
            {
                isValidHttpUrl,
                hasAwesomeTag.When(isValidHttpUrl)
            };

            var result = plan.Execute("http://microsoft.com");

            Console.WriteLine(result);

            // TODO: (Rules_within_a_ValidationPlan_can_use_APM_signatures_and_run_synchronously) 
            Assert.Fail("Test not written");
        }

        private static void InitializeTwitterPlan()
        {
            isTwitterFreeUrl = new ValidationPlan<string>();
            isTwitterFreeUrl.AddRule(
                s =>
                {
                    var client = new WebClient();

                    try
                    {
                        var response = client.DownloadString(s.As("url"));
                        return !response.ToLower().Contains("twitter");
                    }
                    catch (WebException ex)
                    {
                        ex.Message.As("error");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        ex.As("error");
                        return false;
                    }
                },
                only => only
                            .When(isValidHttpUrl)
                            .WithErrorMessage("Oh no! The page at {url} contains the word 'twitter'")
                            .WithSuccessMessage("The page at {url} doesn't contain the word 'twitter'")
                );
        }

        private static void InitializeReachableUrlPlan()
        {
            var isNotNull = Validate.That<string>(
                s => s != null)
                .WithErrorMessage("URL is null.");
            var isNotEmpty = Validate.That<string>(
                s => !string.IsNullOrWhiteSpace(s))
                .WithErrorMessage("URL is empty.");
            var isWellFormed = Validate.That<string>(
                s => Uri.IsWellFormedUriString(s.As("url"), UriKind.Absolute))
                .WithErrorMessage("URL '{url}' is not well-formed.");
            isValidHttpUrl = Validate.That<string>(
                s => s.As("url").StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .WithErrorMessage("URL {url} does not begin with 'http' or 'https'.");
            var isReachable = Validate.That<string>(
                s =>
                {
                    var request = WebRequest.Create(s.As("url"));
                    request.Timeout = 2000;

                    try
                    {
                        var response = request.GetResponse();
                        return true;
                    }
                    catch (WebException ex)
                    {
                        ex.Message.As("error");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        ex.As("error");
                        return false;
                    }
                })
                .WithErrorMessage("An exception occurred while verifying {url}: {error}")
                .WithSuccessMessage("{url} is OK.");

            isReachableUrl = new ValidationPlan<string>
            {
                isNotNull,
                isNotEmpty.When(isNotNull),
                isWellFormed.When(isNotNull),
                isReachable.When(isWellFormed, isValidHttpUrl)
            };
        }

        public static IEnumerable<string> Urls()
        {
            yield return @"https://bing.com";
            yield return @"http://microsoft.com";
            yield return @"ftp://ftp.com";
            yield return @"\\someserver\share";
            yield return @"c:\temp\file.txt";
            yield return @"http://google.com/ugdi3g4wfkdsbkfjsd";
            yield return @"http://yahoo.com";
        }

        public class ApmOperation<TArg, TResult>
        {
            private readonly Func<TArg, Task<TResult>> operation;

            public ApmOperation(Func<TArg, Task<TResult>> operation)
            {
                this.operation = operation;
            }

            public IAsyncResult BeginInvoke(TArg arg, AsyncCallback callback, object state)
            {
                var task = operation(arg);
                task.ContinueWith(_ => callback(task));
                return task;
            }

            public TResult EndInvoke(IAsyncResult result)
            {
                return ((Task<TResult>) result).Result;
            }
        }
    }
}