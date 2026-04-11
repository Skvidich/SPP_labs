using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TestFramework.Attributes;
using TestFramework.Context;

namespace TestRunner
{
    public class TestEngine
    {
        private readonly ConsoleReporter _reporter;
        private readonly TestScheduler _scheduler;
        private readonly TestExecutor _executor;
        private readonly CsvDataProvider _dataProvider;
        private readonly TestLoader _loader;

        public TestEngine()
        {
            _reporter = new ConsoleReporter();
            _scheduler = new TestScheduler(_reporter);
            _executor = new TestExecutor(_reporter);
            _dataProvider = new CsvDataProvider();
            _loader = new TestLoader();
        }

        public async Task RunTestsInAssembly(TestRunOptions options)
        {
            var globalTimer = Stopwatch.StartNew();

            try
            {
                var assembly = _loader.LoadAssembly(options.AssemblyPath);
                Directory.SetCurrentDirectory(Path.GetDirectoryName(options.AssemblyPath));

                var testClasses = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                    .ToList();

                var globalContext = new GlobalContext();

                foreach (var testClass in testClasses)
                {
                    await RunTestClass(testClass, options, globalContext);
                }
            }
            catch (Exception ex)
            {
                _reporter.PrintError($"Critical Engine Error: {ex.Message}");
            }
            finally
            {
                globalTimer.Stop();
                _reporter.PrintFinalStats(globalTimer.ElapsedMilliseconds);
            }
        }

        private async Task RunTestClass(Type type, TestRunOptions options, GlobalContext ctx)
        {
            var filterDelegate = TestFilterFactory.CreateFilter(options);

            if (type.GetCustomAttribute<IgnoreAttribute>() != null)
            {
                _reporter.OnTestSkipped(type.Name, type.GetCustomAttribute<IgnoreAttribute>().Reason);
                return;
            }

            RunStaticMethod<ClassInitializeAttribute>(type, ctx);

            var methods = type.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .OrderBy(m => m.GetCustomAttribute<OrderAttribute>()?.Order ?? int.MaxValue)
                .ToList();

            var allActions = new List<Func<Task>>();

            foreach (var method in methods)
            {
                if (!TestFilterFactory.ExecuteAll(filterDelegate, type, method))
                {
                    continue;
                }

                if (method.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    _reporter.OnTestSkipped(method.Name, method.GetCustomAttribute<IgnoreAttribute>().Reason);
                    continue;
                }

                var testActions = PrepareTestActions(type, method, ctx);
                allActions.AddRange(testActions);
            }

            await _scheduler.ExecuteAsync(allActions, options.RunInParallel, options.MaxDegreeOfParallelism);

            RunStaticMethod<ClassCleanupAttribute>(type, ctx);
        }

        private List<Func<Task>> PrepareTestActions(Type type, MethodInfo method, GlobalContext ctx)
        {
            var actions = new List<Func<Task>>();

            var methodSourceAttr = method.GetCustomAttribute<MethodDataSourceAttribute>();
            if (methodSourceAttr != null)
            {
                var sourceMethod = type.GetMethod(methodSourceAttr.MethodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

                if (sourceMethod != null)
                {
                    object instance = sourceMethod.IsStatic ? null : Activator.CreateInstance(type);
                    var data = sourceMethod.Invoke(instance, null) as IEnumerable<object[]>;

                    if (data != null)
                    {
                        foreach (var args in data)
                        {
                            actions.Add(() => _executor.RunSingleTest(type, method, args, ctx));
                        }
                        return actions;
                    }
                }
            }

            var testCases = method.GetCustomAttributes<TestCaseAttribute>();
            if (testCases.Any())
            {
                foreach (var tc in testCases)
                    actions.Add(() => _executor.RunSingleTest(type, method, tc.Arguments, ctx));
                return actions;
            }

            var dataSource = method.GetCustomAttribute<DataSourceAttribute>();
            if (dataSource != null)
            {
                var data = _dataProvider.ReadData(method, dataSource.FilePath);
                foreach (var args in data)
                    actions.Add(() => _executor.RunSingleTest(type, method, args, ctx));
                return actions;
            }

            actions.Add(() => _executor.RunSingleTest(type, method, null, ctx));

            return actions;
        }

        private void RunStaticMethod<TAttribute>(Type type, GlobalContext ctx) where TAttribute : Attribute
        {
            var method = type.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<TAttribute>() != null && m.IsStatic);

            if (method == null) return;

            try
            {
                var parameters = method.GetParameters();
                object[] args = parameters.Any(p => p.ParameterType == typeof(GlobalContext))
                    ? new object[] { ctx }
                    : null;

                method.Invoke(null, args);
            }
            catch (Exception ex)
            {
                _reporter.PrintError($"Lifecycle Method {typeof(TAttribute).Name} Failed in {type.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

    }
}