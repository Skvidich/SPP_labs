using System.Diagnostics;
using System.Reflection;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Exceptions;

namespace TestRunner
{
    public class TestExecutor
    {
        private readonly ConsoleReporter _reporter;

        public TestExecutor(ConsoleReporter reporter)
        {
            _reporter = reporter;
        }

        public async Task RunSingleTest(Type testClassType, MethodInfo method, object[] parameters, GlobalContext sharedContext)
        {
            string testName = method.Name;
            if (parameters != null && parameters.Length > 0)
                testName += $"({string.Join(", ", parameters)})";

            Stopwatch sw = Stopwatch.StartNew(); 
            object instance = null;
            bool passed = false;
            string failMessage = "";

            try
            {
                instance = Activator.CreateInstance(testClassType);

                if (instance is IUseSharedContext contextUser)
                {
                    contextUser.Context = sharedContext;
                }

                var initMethod = testClassType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);
                initMethod?.Invoke(instance, null);

                var timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
                int timeout = timeoutAttr?.Milliseconds ?? -1;
                var expectedExAttr = method.GetCustomAttribute<ExpectedExceptionAttribute>();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var res = method.Invoke(instance, parameters);
                        if (res is Task t) await t;
                        if (expectedExAttr != null) throw new TestFailedException($"Expected exception {expectedExAttr.ExceptionType.Name} was not thrown.");
                    }
                    catch (Exception ex)
                    {
                        var actual = (ex is TargetInvocationException tex) ? tex.InnerException : ex;
                        if (expectedExAttr != null && expectedExAttr.ExceptionType.IsAssignableFrom(actual.GetType())) return;
                        throw actual;
                    }
                });

                if (timeout > 0)
                {
                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task) throw new TestFailedException($"Timeout {timeout}ms");
                }
                await task;
                passed = true;
            }
            catch (Exception ex)
            {
                var actual = (ex is AggregateException ae) ? ae.InnerException : (ex is TargetInvocationException tie) ? tie.InnerException : ex;
                failMessage = actual is TestFailedException ? actual.Message : $"{actual.GetType().Name}: {actual.Message}";
            }
            finally
            {
                sw.Stop(); 

                try
                {
                    if (instance != null)
                    {
                        var cleanup = testClassType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);
                        cleanup?.Invoke(instance, null);
                    }
                }
                catch { }
            }

            if (passed)
                _reporter.OnTestPassed(testName, sw.ElapsedMilliseconds);
            else
                _reporter.OnTestFailed(testName, failMessage, sw.ElapsedMilliseconds);
        }
    }
}