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
