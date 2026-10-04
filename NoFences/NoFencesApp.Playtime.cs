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

        private int playtimeTicks;

        /// <summary>
        /// Games widget: every 15 s, a game counts as running while any process runs from its install
        /// folder (that also covers games whose exe has a different name than the folder).
        /// </summary>
        private void TrackLibraryGames(DateTime now)
        {
            if (playtimeTicks++ % 3 != 0 || !Store.Config.Fences.Any(f => f.Kind == FenceKind.Widget && f.WidgetType == "games"))
                return;
            var library = Widgets.GameLibrary.CachedNow()?.Where(g => !string.IsNullOrEmpty(g.InstallDir)).ToList();
            if (library == null || library.Count == 0)
                return;
            var running = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var path = ProcessImagePath(process.Id);
                    if (path == null)
                        continue;
                    var game = library.FirstOrDefault(g => path.StartsWith(g.InstallDir!.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
                    if (game == null)
                        continue;
                    DateTime? start = null;
                    try { start = process.StartTime; } catch (Exception) { }
                    if (!running.TryGetValue(game.Id, out var earliest) || start < earliest)
                        running[game.Id] = start;
                }
                finally
                {
                    process.Dispose();
                }
            }
            foreach (var (id, start) in running)
                playtimeDirty |= Playtime.RunningKey(PlaytimeLog.GameKey(id), now, start);
        }

        private void TrackPlaytime()
        {
            TrackLibraryGames(DateTime.Now);
            var games = Store.Config.Fences
                .Where(f => f.Kind == FenceKind.Widget && f.WidgetType == "playtime" && !string.IsNullOrEmpty(f.WidgetOption))
                .Select(f => f.WidgetOption!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (games.Count == 0)
            {
                // The games widget may have recorded something; save it on the same rhythm
                if (playtimeDirty && DateTime.Now - playtimeSaved > TimeSpan.FromMinutes(1))
                    SavePlaytime();
                return;
            }

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
