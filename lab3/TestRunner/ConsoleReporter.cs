using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestThreadPool;

namespace TestRunner
{
    public class ConsoleReporter
    {
        private int _passed = 0;
        private int _failed = 0;
        private int _skipped = 0;
        private static readonly object _consoleLock = new object();

        private CancellationTokenSource _monitorCts;
        private Task _monitorTask;
        private string _originalTitle;

        public ConsoleReporter()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        // --- МЕТОДЫ МОНИТОРИНГА ПУЛА (ЛР 3) ---

        public void StartPoolMonitoring(CustomThreadPool pool, int totalTasks, int maxThreads)
        {
            _originalTitle = Console.Title;
            _monitorCts = new CancellationTokenSource();
            pool.OnLogMessage += HandlePoolLogMessage;
            _monitorTask = Task.Run(() => MonitorLoop(pool, totalTasks, maxThreads, _monitorCts.Token));
        }

        public void StopPoolMonitoring(CustomThreadPool pool)
        {
            if (_monitorCts != null)
            {
                _monitorCts.Cancel();
                pool.OnLogMessage -= HandlePoolLogMessage;
                try { _monitorTask?.Wait(1000); } catch { }
                _monitorCts.Dispose();
                _monitorCts = null;
                Console.Title = _originalTitle ?? "TestRunner CLI Pro";
            }
        }

        private void HandlePoolLogMessage(string msg)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  [Pool Event] ");
                if (msg.Contains("UP") || msg.Contains("Added")) Console.ForegroundColor = ConsoleColor.Cyan;
                else if (msg.Contains("DOWN") || msg.Contains("removed")) Console.ForegroundColor = ConsoleColor.Magenta;
                else if (msg.Contains("Watchdog") || msg.Contains("hung")) Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        private async Task MonitorLoop(CustomThreadPool pool, int totalTasks, int maxThreads, CancellationToken token)
        {
            DateTime lastPrint = DateTime.Now;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var stats = pool.GetStats();
                    Console.Title = $"📊 TR Monitor | Threads: {stats.TotalThreads}/{maxThreads} | Busy: {stats.BusyThreads} | Queue: {stats.QueueLength} | Done: {stats.CompletedTasks}/{totalTasks}";

                    if (stats.CompletedTasks >= totalTasks) break;

                    if ((DateTime.Now - lastPrint).TotalSeconds >= 1)
                    {
                        lock (_consoleLock)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"\n  === 📈 Pool Status: {stats.BusyThreads} of {stats.TotalThreads} threads working | In Queue: {stats.QueueLength} ===\n");
                            Console.ResetColor();
                        }
                        lastPrint = DateTime.Now;
                    }
                    await Task.Delay(250, token);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void PrintClassHeader(string className, bool isE2E)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"\nClass: {className} {(isE2E ? "[E2E Sequence]" : "")}");
            }
        }

        public void OnTestPassed(string name, long durationMs)
        {
            Interlocked.Increment(ref _passed);
            PrintLine(name, "PASS", ConsoleColor.Green, null, durationMs);
        }

        public void OnTestFailed(string name, string message, long durationMs)
        {
            Interlocked.Increment(ref _failed);
            PrintLine(name, "FAIL", ConsoleColor.Red, message, durationMs);
        }

        public void OnTestSkipped(string name, string reason)
        {
            Interlocked.Increment(ref _skipped);
            PrintLine(name, "SKIPPED", ConsoleColor.Yellow, reason, -1);
        }

        public void PrintError(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {message}");
                Console.ResetColor();
            }
        }

        public void PrintFinalStats(long totalDurationMs)
        {
            lock (_consoleLock)
            {
                Console.WriteLine("--------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"PASSED: {_passed}    ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"FAILED: {_failed}    ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"SKIPPED: {_skipped}");
                Console.ResetColor();
                Console.WriteLine($"\nTotal Duration: {totalDurationMs} ms");
                Console.WriteLine("--------------------------------------------------");
            }
        }

        private void PrintLine(string name, string status, ConsoleColor color, string message, long durationMs)
        {
            lock (_consoleLock)
            {
                Console.Write($"  [{name}] ");
                Console.ForegroundColor = color;
                Console.Write(status);
                Console.ResetColor();

                if (durationMs >= 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($" ({durationMs}ms)");
                    Console.ResetColor();
                }

                if (!string.IsNullOrEmpty(message))
                    Console.Write($" - {message}");

                Console.WriteLine();
            }
        }
    }
}