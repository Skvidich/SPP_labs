using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestThreadPool; 

namespace TestRunner
{
    public class TestScheduler
    {
        private readonly ConsoleReporter _reporter;

        public TestScheduler(ConsoleReporter reporter)
        {
            _reporter = reporter;
        }

        public async Task ExecuteAsync(IEnumerable<Func<Task>> testActions, bool runInParallel, int maxDegreeOfParallelism)
        {
            var actionsList = testActions.ToList();

            if (!runInParallel || actionsList.Count == 0)
            {
                foreach (var action in actionsList) await action();
                return;
            }


            using var pool = new CustomThreadPool(
                minThreads: 2,
                maxThreads: maxDegreeOfParallelism,
                idleTimeoutMs: 3000,
                hangTimeoutMs: 5000);

            _reporter.SubscribeToPoolEvents(pool);
            _reporter.StartPoolMonitoring(pool, actionsList.Count, maxDegreeOfParallelism);

            var tasks = new List<Task>();

            foreach (var action in actionsList)
            {
                var poolTask = pool.Enqueue(() =>
                {
                    action().GetAwaiter().GetResult();
                });
                tasks.Add(poolTask);
            }

            try
            {

                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {

            }


            pool.WaitForIdle();


            _reporter.StopPoolMonitoring(pool);
        }
    }
}