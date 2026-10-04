using System.Diagnostics;
using System.Text.Json;
using NoFences.Widgets;

namespace NoFences.Model
{
    /// <summary>
    /// Play sessions per game, recorded by NoFences itself (playtime.json next to the config).
    /// A game is identified by its exe file name, e.g. "game.exe".
    /// </summary>
    public sealed class PlaytimeLog
    {
        /// <summary>A gap shorter than this continues the previous session (e.g. NoFences restarted mid-game).</summary>
        public static readonly TimeSpan MergeGap = TimeSpan.FromMinutes(2);

        public Dictionary<string, List<PlaySession>> Games { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static string Key(string exePath) => Path.GetFileName(exePath).ToLowerInvariant();

        /// <summary>Key for a game found in a library (Steam/Epic/GOG/Xbox id), tracked by its install folder.</summary>
        public static string GameKey(string gameId) => "game:" + gameId.ToLowerInvariant();

        public IReadOnlyCollection<PlaySession> SessionsOf(string exePath) => SessionsOfKey(Key(exePath));

        public IReadOnlyCollection<PlaySession> SessionsOfKey(string key) =>
            Games.TryGetValue(key, out var list) ? list : Array.Empty<PlaySession>();

        /// <summary>
        /// Called every few seconds while the game runs: extends the current session or starts a new one
        /// (from the process start time if known). Returns true if something changed.
        /// </summary>
        public bool Running(string exePath, DateTime now, DateTime? processStart) => RunningKey(Key(exePath), now, processStart);

        public bool RunningKey(string key, DateTime now, DateTime? processStart)
        {
            if (!Games.TryGetValue(key, out var list))
                Games[key] = list = new List<PlaySession>();

            var nowUnix = new DateTimeOffset(now).ToUnixTimeSeconds();
            if (list.Count > 0 && nowUnix - list[^1].End <= MergeGap.TotalSeconds)
            {
                list[^1] = list[^1] with { End = nowUnix };
                return true;
            }
            var start = processStart is DateTime ps && ps <= now && now - ps < TimeSpan.FromDays(2)
                ? new DateTimeOffset(ps).ToUnixTimeSeconds()
                : nowUnix;
            // Never overlap the previous session
            if (list.Count > 0)
                start = Math.Max(start, (long)list[^1].End);
            list.Add(new PlaySession("", start, nowUnix));
            return true;
        }

        #region Storage

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

        public static PlaytimeLog Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var log = JsonSerializer.Deserialize<PlaytimeLog>(File.ReadAllText(path), Options);
                    if (log != null)
                    {
                        log.Games = new Dictionary<string, List<PlaySession>>(log.Games, StringComparer.OrdinalIgnoreCase);
                        return log;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Playtime log unreadable: {e.Message}");
                try { File.Copy(path, path + $".broken-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true); } catch { }
            }
            return new PlaytimeLog();
        }

        public void Save(string path)
        {
            try
            {
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(this, Options));
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Saving playtime failed: {e.Message}");
            }
        }

        #endregion
    }
}
