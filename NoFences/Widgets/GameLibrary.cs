using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NoFences.Widgets
{
    public enum GameSource { Steam, Epic, Gog, Xbox }

    /// <summary>An installed game. <see cref="Launch"/> is a URI (steam://…) or an exe; the cover may be missing.</summary>
    public sealed record GameInfo(string Id, string Name, GameSource Source, string Launch, string? CoverPath, string? IconPath, DateTime LastPlayed = default, string? InstallDir = null)
    {
        public void Start()
        {
            try
            {
                ProcessStartInfo info;
                if (Launch.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
                    info = new ProcessStartInfo("explorer.exe", Launch); // Store/Xbox apps
                else if (Launch.Contains("://"))
                    info = new ProcessStartInfo(Launch) { UseShellExecute = true }; // steam://, Epic launcher
                else
                    info = new ProcessStartInfo(Launch) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(Launch) ?? "" };
                Process.Start(info);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>Finds installed games of Steam, Epic, GOG and the Xbox app (read-only, nothing is changed).</summary>
    public static partial class GameLibrary
    {
        private static Task<List<GameInfo>>? cached;
        private static DateTime cachedAt;

        /// <summary>
        /// The library, scanned in the background at most every 30 minutes and shared by the games widget,
        /// the playtime counter and the game news.
        /// </summary>
        public static Task<List<GameInfo>> CachedAsync(bool refresh = false)
        {
            if (refresh || cached == null || cached.IsCompleted && DateTime.UtcNow - cachedAt > TimeSpan.FromMinutes(30))
            {
                cachedAt = DateTime.UtcNow;
                cached = Task.Run(Scan);
            }
            return cached;
        }

        /// <summary>The library if it has been scanned already (never waits).</summary>
        public static List<GameInfo>? CachedNow()
        {
            var task = CachedAsync();
            return task.IsCompletedSuccessfully ? task.Result : null;
        }

        public static List<GameInfo> Scan()
        {
            var games = new List<GameInfo>();
            foreach (var scanner in new Func<IEnumerable<GameInfo>>[] { ScanSteam, ScanEpic, ScanGog, ScanXbox })
            {
                try
                {
                    games.AddRange(scanner());
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Game scan: {e.Message}");
                }
            }
            return games
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        #region Steam

        private static IEnumerable<GameInfo> ScanSteam()
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
                    GameInfo? game = null;
                    try
                    {
                        game = ParseAppManifest(File.ReadAllText(manifest), steam, library);
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

        /// <summary>Tools Steam installs alongside games (runtimes, redistributables), not worth a tile.</summary>
        private static bool IsTool(string appId, string name) =>
            appId is "228980" or "1070560" or "1391110" or "1628350"
            || name.Contains("Redistributable", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Proton", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Steam Linux Runtime", StringComparison.OrdinalIgnoreCase);

        /// <summary>One game from an appmanifest_*.acf; null for tools and unfinished installs.</summary>
        public static GameInfo? ParseAppManifest(string acf, string steamPath, string? library = null)
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
            var installDir = library != null && values.TryGetValue("installdir", out var dir) ? Path.Combine(library, "steamapps", "common", dir) : null;
            return new GameInfo("steam:" + id, name, GameSource.Steam, $"steam://rungameid/{id}", SteamCover(steamPath, id), null, lastPlayed, installDir);
        }

        /// <summary>The 600×900 library image Steam keeps locally (old flat and newer per-app folder layout).</summary>
        private static string? SteamCover(string steam, string appId)
        {
            try
            {
                var cache = Path.Combine(steam, "appcache", "librarycache");
                var flat = Path.Combine(cache, $"{appId}_library_600x900.jpg");
                if (File.Exists(flat))
                    return flat;
                var folder = Path.Combine(cache, appId);
                if (Directory.Exists(folder))
                    return Directory.EnumerateFiles(folder, "library_600x900*.jpg", SearchOption.AllDirectories).FirstOrDefault();
            }
            catch (Exception) { }
            return null;
        }

        #endregion

        #region Epic

        private static IEnumerable<GameInfo> ScanEpic()
        {
            var manifests = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
            if (!Directory.Exists(manifests))
                yield break;
            foreach (var file in Directory.EnumerateFiles(manifests, "*.item"))
            {
                GameInfo? game = null;
                try
                {
                    game = ParseEpicManifest(File.ReadAllText(file));
                }
                catch (Exception) { }
                if (game != null)
                    yield return game;
            }
        }

        public static GameInfo? ParseEpicManifest(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            string S(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            bool B(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
            if (B("bIsIncompleteInstall") || S("DisplayName").Length == 0 || S("AppName").Length == 0)
                return null;
            // Epic's own add-ons and the Unreal Engine aren't games
            if (r.TryGetProperty("AppCategories", out var cats) && cats.ValueKind == JsonValueKind.Array
                && cats.EnumerateArray().Any(c => c.GetString() is "addons" or "plugins" or "engines"))
                return null;
            var uri = $"com.epicgames.launcher://apps/{S("CatalogNamespace")}%3A{S("CatalogItemId")}%3A{S("AppName")}?action=launch&silent=true";
            var exe = Path.Combine(S("InstallLocation"), S("LaunchExecutable"));
            return new GameInfo("epic:" + S("AppName"), S("DisplayName"), GameSource.Epic, uri, null, File.Exists(exe) ? exe : null, default, S("InstallLocation").Length > 0 ? S("InstallLocation") : null);
        }

        #endregion

        #region GOG

        private static IEnumerable<GameInfo> ScanGog()
        {
            using var games = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
            if (games == null)
                yield break;
            foreach (var id in games.GetSubKeyNames())
            {
                using var game = games.OpenSubKey(id);
                if (game?.GetValue("gameName") is not string name || game.GetValue("exe") is not string exe || !File.Exists(exe))
                    continue;
                // DLCs point to their base game
                if (game.GetValue("dependsOn") is string dependsOn && dependsOn.Length > 0)
                    continue;
                yield return new GameInfo("gog:" + id, name, GameSource.Gog, exe, null, exe, default, game.GetValue("path") as string ?? Path.GetDirectoryName(exe));
            }
        }

        #endregion

        #region Xbox

        /// <summary>Xbox app / Game Pass games: packages that carry a MicrosoftGame.config.</summary>
        private static IEnumerable<GameInfo> ScanXbox()
        {
            var manager = new Windows.Management.Deployment.PackageManager();
            foreach (var package in manager.FindPackagesForUser(""))
            {
                GameInfo? game = null;
                try
                {
                    if (package.IsFramework || package.IsResourcePackage)
                        continue;
                    var folder = package.InstalledPath;
                    if (string.IsNullOrEmpty(folder) || !File.Exists(Path.Combine(folder, "MicrosoftGame.config")))
                        continue;
                    var entry = package.GetAppListEntries().FirstOrDefault();
                    if (entry == null)
                        continue;
                    var logo = package.Logo?.LocalPath;
                    game = new GameInfo("xbox:" + package.Id.FamilyName, package.DisplayName, GameSource.Xbox,
                        $"shell:AppsFolder\\{entry.AppUserModelId}", null, logo != null && File.Exists(logo) ? logo : null, default, folder);
                }
                catch (Exception) { }
                if (game != null)
                    yield return game;
            }
        }

        #endregion
    }
}
