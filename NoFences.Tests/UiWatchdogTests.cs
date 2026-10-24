using System.Collections.Concurrent;
using NoFences.Util;

namespace NoFences.Tests
{
    public class UiWatchdogTests
    {
        [Fact]
        public void Activity_NestsAndRestores()
        {
            Assert.Null(UiWatchdog.Current);
            using (UiWatchdog.Activity("Widget clipboard"))
            {
                Assert.Equal("Widget clipboard", UiWatchdog.Current);
                using (UiWatchdog.Activity("Paint"))
                    Assert.Equal("Widget clipboard > Paint", UiWatchdog.Current);
                Assert.Equal("Widget clipboard", UiWatchdog.Current);
            }
            Assert.Null(UiWatchdog.Current);
        }

        [Fact]
        public void Named_MarksTheHandlerAndRestoresOnException()
        {
            string? seen = null;
            UiWatchdog.Named("Automation", (_, _) => seen = UiWatchdog.Current)(null, EventArgs.Empty);
            Assert.Equal("Automation", seen);
            Assert.Throws<InvalidOperationException>(() => UiWatchdog.Named("Boom", (_, _) => throw new InvalidOperationException())(null, EventArgs.Empty));
            Assert.Null(UiWatchdog.Current);
        }

        [Fact]
        public void Describe_NamesWhatRan()
        {
            Assert.Equal("UI blocked 1830 ms during: Clipboard, Paint \"Notes\"", UiWatchdog.Describe(1830, new[] { "Clipboard", "Paint \"Notes\"" }));
            Assert.Equal("UI blocked 700 ms during: (unmarked)", UiWatchdog.Describe(700, Array.Empty<string>()));
        }

        [Theory]
        [InlineData(900L, 20L, 850L, "whole process/PC stalled")] // watchdog was stuck too
        [InlineData(900L, 800L, 30L, "UI thread busy")]
        [InlineData(900L, 15L, 30L, "UI thread waiting")]
        [InlineData(900L, null, 30L, "unknown")]
        public void Describe_TellsTheKindOfHang(long ms, long? cpu, long late, string verdict)
        {
            var line = UiWatchdog.Describe(ms, Array.Empty<string>(), cpu, late);
            Assert.StartsWith($"UI blocked {ms} ms during: (unmarked); UI CPU ", line);
            Assert.EndsWith($"watchdog late {late} ms -> {verdict}", line);
        }

        [Fact]
        public void BlockedUiThread_IsLoggedWithItsActivity()
        {
            using var temp = new TempFolder();
            using var ui = new FakeUiThread();
            var previous = SynchronizationContext.Current;
            Log.Folder = temp.Path;
            try
            {
                SynchronizationContext.SetSynchronizationContext(ui);
                UiWatchdog.Start();
                Thread.Sleep(300); // a few quick pings: no freeze
                ui.Post(state =>
                {
                    using var _ = UiWatchdog.Activity("Slow test work");
                    Thread.Sleep(1200);
                }, null);
                // Wait until the block is over and has been written
                var log = Path.Combine(temp.Path, "log.txt");
                for (var i = 0; i < 40 && !File.Exists(log); i++)
                    Thread.Sleep(100);
                var text = File.ReadAllText(log);
                Assert.Contains("Freeze: UI blocked", text);
                Assert.Contains("Slow test work", text);
                // The fake UI thread slept: hardly any CPU -> it waited
                Assert.Contains("-> UI thread waiting", text);
                Assert.Single(text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            }
            finally
            {
                UiWatchdog.Stop();
                Log.Folder = null;
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Fact]
        public void DesktopHook_RunsOnItsOwnThreadAndStopsCleanly()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var hook = new NoFences.Win32.DesktopDoubleClickHook();
            hook.Dispose();
            hook.Dispose(); // twice is fine
            // The hook thread answers WM_QUIT at once (Join would wait a full second otherwise)
            Assert.True(watch.ElapsedMilliseconds < 1000, $"took {watch.ElapsedMilliseconds} ms");
        }

        /// <summary>Runs posted callbacks one after another on its own thread, like a UI thread.</summary>
        private sealed class FakeUiThread : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<(SendOrPostCallback, object?)> queue = new();

            public FakeUiThread()
            {
                new Thread(() =>
                {
                    foreach (var (callback, state) in queue.GetConsumingEnumerable())
                        callback(state);
                }) { IsBackground = true }.Start();
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                if (!queue.IsAddingCompleted)
                    queue.Add((d, state));
            }

            public void Dispose() => queue.CompleteAdding();
        }
    }
}
