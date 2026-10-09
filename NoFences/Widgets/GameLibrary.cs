using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NoFences.Widgets
{
    /// <summary>An installed Steam game.</summary>
    public sealed record SteamGame(string AppId, string Name, DateTime LastPlayed = default);

    /// <summary>Finds installed Steam games for the game news (read-only, nothing is changed).</summary>
    public static partial class GameLibrary
    {
        private static Task<List<SteamGame>>? cached;
        private static DateTime cachedAt;

        /// <summary>The installed games, scanned in the background at most every 30 minutes.</summary>
        public static Task<List<SteamGame>> CachedAsync(bool refresh = false)
        {
            if (refresh || cached == null || cached.IsCompleted && DateTime.UtcNow - cachedAt > TimeSpan.FromMinutes(30))
            {
                cachedAt = DateTime.UtcNow;
                cached = Task.Run(Scan);
            }
            return cached;
        }

        public static List<SteamGame> Scan()
        {
            try
            {
                return ScanSteam().GroupBy(g => g.AppId).Select(g => g.First()).ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Game scan: {e.Message}");
                return new List<SteamGame>();
            }
        }

        private static IEnumerable<SteamGame> ScanSteam()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is not string steam || !Directory.Exists(steam))
                yield break;
            steam = steam.Replace('/', '\\');
            var libraries = new List<string> { steam };
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
                libraries.AddRange(ParseLibraryFolders(File.ReadAllText(vdf)));

            foreach (var library in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var apps = Path.Combine(library, "steamapps");
                if (!Directory.Exists(apps))
                    continue;
                foreach (var manifest in Directory.EnumerateFiles(apps, "appmanifest_*.acf"))
                {
                    SteamGame? game = null;
                    try
                    {
                        game = ParseAppManifest(File.ReadAllText(manifest));
                    }
                    catch (IOException) { }
                    if (game != null)
                        yield return game;
                }
            }
        }

        [GeneratedRegex("\"path\"\\s+\"([^\"]+)\"")]
        private static partial Regex VdfPath();

        [GeneratedRegex("\"(\\w+)\"\\s+\"([^\"]*)\"")]
        private static partial Regex VdfPair();

        /// <summary>Library folders from steamapps\libraryfolders.vdf.</summary>
        public static IEnumerable<string> ParseLibraryFolders(string vdf) =>
            VdfPath().Matches(vdf).Select(m => m.Groups[1].Value.Replace(@"\\", @"\"));

        /// <summary>Tools Steam installs alongside games (runtimes, redistributables), not games.</summary>
        private static bool IsTool(string appId, string name) =>
            appId is "228980" or "1070560" or "1391110" or "1628350"
            || name.Contains("Redistributable", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Proton", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Steam Linux Runtime", StringComparison.OrdinalIgnoreCase);

        /// <summary>One game from an appmanifest_*.acf; null for tools and unfinished installs.</summary>
        public static SteamGame? ParseAppManifest(string acf)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in VdfPair().Matches(acf))
                values.TryAdd(m.Groups[1].Value, m.Groups[2].Value);
            if (!values.TryGetValue("appid", out var id) || !values.TryGetValue("name", out var name) || IsTool(id, name))
                return null;
            // StateFlags 4 = fully installed
            if (values.TryGetValue("StateFlags", out var flags) && int.TryParse(flags, out var f) && (f & 4) == 0)
                return null;
            var lastPlayed = values.TryGetValue("LastPlayed", out var lp) && long.TryParse(lp, out var unix) && unix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime
                : default;
            return new SteamGame(id, name, lastPlayed);
        }
    }
}
