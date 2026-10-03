using System.Diagnostics;
using NoFences.Model;

namespace NoFences
{
    /// <summary>
    /// Built-in playtime tracking: every few seconds, checks whether the games chosen in playtime widgets
    /// are running and records it – also while the widget is collapsed or hidden.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer playtimeTimer = new() { Interval = 5000 };
        private PlaytimeLog? playtime;
        private DateTime playtimeSaved = DateTime.Now;
        private bool playtimeDirty;

        private string PlaytimePath => Path.Combine(Store.DataDirectory, "playtime.json");

        public PlaytimeLog Playtime => playtime ??= PlaytimeLog.Load(PlaytimePath);

        private void InitPlaytime()
        {
            playtimeTimer.Tick += (_, _) => TrackPlaytime();
            playtimeTimer.Start();
        }

        private void TrackPlaytime()
        {
            var games = Store.Config.Fences
                .Where(f => f.Kind == FenceKind.Widget && f.WidgetType == "playtime" && !string.IsNullOrEmpty(f.WidgetOption))
                .Select(f => f.WidgetOption!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (games.Count == 0)
                return;

            var now = DateTime.Now;
            foreach (var exe in games)
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe));
                try
                {
                    if (processes.Length == 0)
                        continue;
                    playtimeDirty |= Playtime.Running(exe, now, StartTime(processes));
                }
                finally
                {
                    foreach (var p in processes)
                        p.Dispose();
                }
            }

            // Saving every minute is enough; at most a minute is lost if Windows crashes.
            if (playtimeDirty && now - playtimeSaved > TimeSpan.FromMinutes(1))
                SavePlaytime();
        }

        /// <summary>Earliest start time we may read (games with anti-cheat may refuse; then "now" is used).</summary>
        private static DateTime? StartTime(Process[] processes)
        {
            DateTime? earliest = null;
            foreach (var p in processes)
            {
                try
                {
                    if (earliest == null || p.StartTime < earliest)
                        earliest = p.StartTime;
                }
                catch (Exception)
                {
                }
            }
            return earliest;
        }

        private void SavePlaytime()
        {
            if (playtime == null)
                return;
            playtime.Save(PlaytimePath);
            playtimeSaved = DateTime.Now;
            playtimeDirty = false;
        }

        private void DisposePlaytime()
        {
            playtimeTimer.Dispose();
            if (playtimeDirty)
                SavePlaytime();
        }
    }
}
