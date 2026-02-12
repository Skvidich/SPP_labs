using System;
using System.Text;
using System.Threading;

namespace TestRunner
{
    public class ConsoleReporter
    {
        private int _passed = 0;
        private int _failed = 0;
        private int _skipped = 0;
        private static readonly object _consoleLock = new object();

        public ConsoleReporter()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public void OnTestPassed(string name, long durationMs)
        {
            Interlocked.Increment(ref _passed);
            PrintLine(name, "PASS", ConsoleColor.Green, message: null, durationMs);
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

        public void PrintClassHeader(string className, bool isE2E)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"\nClass: {className} {(isE2E ? "[E2E Sequence]" : "")}");
            }
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
                Console.WriteLine();
                Console.WriteLine($"Total Duration: {totalDurationMs} ms");
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