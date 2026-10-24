using System.Diagnostics;

namespace NoFences.Util
{
    /// <summary>
    /// Notices when the UI thread is blocked (all fences frozen) and writes how long and what was
    /// running into the log, e.g. "UI blocked 1830 ms during: Clipboard". Places that may take long mark
    /// themselves with <see cref="Activity"/>; a background thread pings the UI thread a few times a second.
    /// </summary>
    public static class UiWatchdog
    {
        /// <summary>Blocks shorter than this aren't noticeable and aren't logged.</summary>
        public const int ThresholdMs = 500;

        private const int PingIntervalMs = 250;

        private static volatile string? current;
        private static volatile bool stopped = true;
        private static SynchronizationContext? ui;

        /// <summary>What the UI thread is doing right now (null = nothing marked).</summary>
        public static string? Current => current;

        /// <summary>Must be called on the UI thread.</summary>
        public static void Start()
        {
            if (!stopped)
                return;
            ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            stopped = false;
            // The thread that runs posted work is the one to measure (in tests that's not the caller)
            ui.Post(_ => uiThread = OpenThread(ThreadQueryLimitedInformation, false, GetCurrentThreadId()), null);
            new Thread(Loop) { IsBackground = true, Name = "UI watchdog", Priority = ThreadPriority.AboveNormal }.Start();
        }

        private static IntPtr uiThread;

        public static void Stop() => stopped = true;

        /// <summary>Marks what the UI thread does until the scope is disposed: <c>using var _ = UiWatchdog.Activity("Clipboard");</c></summary>
        public static Scope Activity(string name)
        {
            var previous = current;
            current = previous == null ? name : previous + " > " + name;
            return new Scope(previous);
        }

        /// <summary>A timer tick handler that marks itself (the timers are where most work happens).</summary>
        public static EventHandler Named(string name, EventHandler handler) => (sender, e) =>
        {
            using var _ = Activity(name);
            handler(sender, e);
        };

        public readonly struct Scope : IDisposable
        {
            private readonly string? previous;

            internal Scope(string? previous) => this.previous = previous;

            public void Dispose() => current = previous;
        }

        private static void Loop()
        {
            using var pong = new ManualResetEventSlim();
            while (!stopped)
            {
                pong.Reset();
                var cpuBefore = UiCpuMs();
                var watch = Stopwatch.StartNew();
                ui!.Post(_ => pong.Set(), null);
                if (pong.Wait(ThresholdMs))
                {
                    Thread.Sleep(PingIntervalMs);
                    continue;
                }

                // Blocked: collect what was running until the UI thread answers again
                var seen = new List<string>();
                var suspended = false;
                var lastCheck = watch.ElapsedMilliseconds;
                // How much later than asked this thread woke up: if it was stuck too, the whole
                // process or PC stood still, not just the UI thread
                var late = Math.Max(0, lastCheck - ThresholdMs);
                while (true)
                {
                    if (current is { } activity && !seen.Contains(activity))
                        seen.Add(activity);
                    if (pong.Wait(100) || stopped)
                        break;
                    // A jump means the PC slept (or this thread wasn't scheduled); that's no hang.
                    var now = watch.ElapsedMilliseconds;
                    if (now - lastCheck > 3000)
                        suspended = true;
                    late = Math.Max(late, now - lastCheck - 100);
                    lastCheck = now;
                }
                var cpuAfter = UiCpuMs();
                var cpu = cpuBefore is { } b && cpuAfter is { } a ? a - b : (long?)null;
                if (!suspended && !stopped)
                    Log.Write("Freeze", Describe(watch.ElapsedMilliseconds, seen, cpu, late));
            }
        }

        public static string Describe(long milliseconds, IReadOnlyCollection<string> activities) =>
            $"UI blocked {milliseconds} ms during: {(activities.Count == 0 ? "(unmarked)" : string.Join(", ", activities))}";

        /// <summary>
        /// With the UI thread's CPU time and how late the watchdog itself woke up, the line also tells
        /// what kind of hang it was: the PC/process stood still, the UI thread was busy, or it waited
        /// (on another program, a lock, the disk …).
        /// </summary>
        public static string Describe(long milliseconds, IReadOnlyCollection<string> activities, long? uiCpuMs, long watchdogLateMs)
        {
            var verdict = watchdogLateMs * 2 >= milliseconds ? "whole process/PC stalled"
                : uiCpuMs is { } cpu && cpu * 2 >= milliseconds ? "UI thread busy"
                : uiCpuMs != null ? "UI thread waiting"
                : "unknown";
            var cpuText = uiCpuMs is { } c ? $"{c} ms" : "?";
            return $"{Describe(milliseconds, activities)}; UI CPU {cpuText}, watchdog late {watchdogLateMs} ms -> {verdict}";
        }

        /// <summary>User + kernel time the UI thread has used so far (null if it can't be read).</summary>
        private static long? UiCpuMs()
        {
            if (uiThread == IntPtr.Zero || !GetThreadTimes(uiThread, out _, out _, out var kernel, out var user))
                return null;
            return (kernel + user) / 10_000; // 100-ns units
        }

        private const uint ThreadQueryLimitedInformation = 0x0800;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(uint access, bool inherit, uint threadId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetThreadTimes(IntPtr thread, out long creation, out long exit, out long kernel, out long user);
    }
}
