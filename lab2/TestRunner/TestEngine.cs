using System.Diagnostics; 
using System.Reflection;
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
            _scheduler = new TestScheduler();
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

                foreach (var testClass in testClasses)
                {
                    await RunTestClass(testClass, options);
                }
            }
            catch (Exception ex)
            {
                _reporter.PrintError($"Critical Engine Error: {ex.Message}");
            }
            finally
            {
                _loader.Unload();
            }

            for (int i = 0; i < 10 && _loader.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            globalTimer.Stop();
            _reporter.PrintFinalStats(globalTimer.ElapsedMilliseconds);
        }

        private async Task RunTestClass(Type testClassType, TestRunOptions options)
        {
            var methods = testClassType.GetMethods()
               .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
               .ToList();

            if (!methods.Any()) return;

            bool isE2E = testClassType.GetCustomAttribute<TestE2EAttribute>() != null;
            if (isE2E)
            {
                methods = methods.OrderBy(m => m.GetCustomAttribute<OrderAttribute>()?.Order ?? int.MaxValue).ToList();
            }

            _reporter.PrintClassHeader(testClassType.Name, isE2E);

            var classContext = new GlobalContext();
            RunStaticMethod<ClassInitializeAttribute>(testClassType, classContext);

            bool runMethodsInParallel = options.RunInParallel && !isE2E;
            var classActions = new List<Func<Task>>();

            foreach (var method in methods)
            {
                if (!ShouldRunTest(testClassType, method, options.CategoryFilter)) continue;

                var ignoreAttr = method.GetCustomAttribute<IgnoreAttribute>();
                if (ignoreAttr != null)
                {
                    _reporter.OnTestSkipped(method.Name, ignoreAttr.Reason);
                    continue;
                }

                classActions.AddRange(CreateActionsForMethod(testClassType, method, classContext));
            }

            if (classActions.Any())
            {
                await _scheduler.ExecuteAsync(classActions, runMethodsInParallel, options.MaxDegreeOfParallelism);
            }

            RunStaticMethod<ClassCleanupAttribute>(testClassType, classContext);
        }

        private List<Func<Task>> CreateActionsForMethod(Type type, MethodInfo method, GlobalContext ctx)
        {
            var actions = new List<Func<Task>>();
            var testCaseAttrs = method.GetCustomAttributes<TestCaseAttribute>();
            var dataSourceAttr = method.GetCustomAttribute<DataSourceAttribute>();

            if (testCaseAttrs.Any())
            {
                foreach (var attr in testCaseAttrs)
                    actions.Add(() => _executor.RunSingleTest(type, method, attr.Arguments, ctx));
            }
            else if (dataSourceAttr != null)
            {
                var data = _dataProvider.ReadData(method, dataSourceAttr.FilePath);
                foreach (var args in data)
                    actions.Add(() => _executor.RunSingleTest(type, method, args, ctx));
            }
            else
            {
                actions.Add(() => _executor.RunSingleTest(type, method, null, ctx));
            }
            return actions;
        }

        private void RunStaticMethod<TAttribute>(Type type, GlobalContext ctx) where TAttribute : Attribute
        {
            var method = type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TAttribute>() != null && m.IsStatic);
            if (method == null) return;
            try
            {
                var args = method.GetParameters().Any(p => p.ParameterType == typeof(GlobalContext))
                    ? new object[] { ctx }
                    : null;
                method.Invoke(null, args);
            }
            catch (Exception ex)
            {
                _reporter.PrintError($"Lifecycle Method {typeof(TAttribute).Name} Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private bool ShouldRunTest(Type classType, MethodInfo method, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            var classCats = classType.GetCustomAttributes<CategoryAttribute>().Select(c => c.CategoryName);
            var methodCats = method.GetCustomAttributes<CategoryAttribute>().Select(c => c.CategoryName);
            return classCats.Contains(filter) || methodCats.Contains(filter);
        }
    }
}