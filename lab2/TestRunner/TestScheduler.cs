using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestRunner
{
    public class TestScheduler
    {
        public async Task ExecuteAsync(IEnumerable<Func<Task>> testActions, bool runInParallel, int maxDegreeOfParallelism)
        {
            if (!runInParallel)
            {
                foreach (var action in testActions)
                {
                    await action();
                }
                return;
            }

            var tasks = new List<Task>();
            using (var semaphore = new SemaphoreSlim(maxDegreeOfParallelism))
            {
                foreach (var action in testActions)
                {
                    await semaphore.WaitAsync();

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await action();
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
            }
        }
    }
}