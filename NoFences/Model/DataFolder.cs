namespace NoFences.Model
{
    /// <summary>
    /// Where NoFences keeps its files, sorted into subfolders:
    /// <c>config</c> (fences.json, playtime, screen time), <c>backups</c>, <c>themes</c>,
    /// <c>media</c> (note pictures, voice notes, pinned clipboard pictures, shelf, bookmarks),
    /// <c>cache</c> (web page widget), <c>logs</c> and <c>lang</c> (own translations).
    /// Normally next to NoFences.exe; in %LocalAppData%\NoFences when the program folder can't be
    /// written to (Program Files, a WinGet package folder). Older versions kept everything flat in
    /// %LocalAppData%\NoFences (or next to the exe in portable mode); that is moved once.
    /// </summary>
    public sealed class DataFolder
    {
        public const string ConfigName = "config";
        public const string BackupsName = "backups";
        public const string ThemesName = "themes";
        public const string MediaName = "media";
        public const string CacheName = "cache";
        public const string LogsName = "logs";
        public const string LangName = "lang";

        /// <summary>Left in the old folder after its data was copied next to the exe.</summary>
        public const string MovedMarker = "moved.txt";

        public DataFolder(string root, bool besideExe)
        {
            Root = root;
            BesideExe = besideExe;
        }

        public string Root { get; }

        /// <summary>Next to NoFences.exe (otherwise in %LocalAppData%\NoFences).</summary>
        public bool BesideExe { get; }

        public string Config => Path.Combine(Root, ConfigName);
        public string Backups => Path.Combine(Root, BackupsName);
        public string Themes => Path.Combine(Root, ThemesName);
        public string Media => Path.Combine(Root, MediaName);
        public string Cache => Path.Combine(Root, CacheName);
        public string Logs => Path.Combine(Root, LogsName);
        public string Lang => Path.Combine(Root, LangName);

        /// <summary>Old folder paths stored in fences (shelf, bookmarks) and where they are now; set by a move.</summary>
        public List<(string From, string To)> MovedPaths { get; } = new();

        public static string LegacyFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoFences");

        public static string ExeFolder => AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        /// <summary>The folder in use, without moving anything.</summary>
        public static DataFolder Find() => Find(ExeFolder, LegacyFolder);

        public static DataFolder Find(string exeFolder, string legacyFolder) =>
            Directory.Exists(Path.Combine(exeFolder, ConfigName)) ? new DataFolder(exeFolder, true) : new DataFolder(legacyFolder, false);

        /// <summary>Chooses the folder and moves data of older versions into it (once).</summary>
        public static DataFolder Prepare() => Prepare(ExeFolder, LegacyFolder, CanWrite(ExeFolder) && !IsProtected(ExeFolder));

        public static DataFolder Prepare(string exeFolder, string legacyFolder, bool exeFolderUsable)
        {
            var besideExe = Directory.Exists(Path.Combine(exeFolder, ConfigName))
                || HasFlatData(exeFolder)
                || exeFolderUsable;
            var folder = new DataFolder(besideExe ? exeFolder : legacyFolder, besideExe);
            if (!Directory.Exists(folder.Config))
            {
                if (HasFlatData(folder.Root))
                    folder.SortFlat(folder.Root, move: true);
                else if (besideExe && HasData(legacyFolder) && !File.Exists(Path.Combine(legacyFolder, MovedMarker)))
                    folder.CopyFrom(legacyFolder);
            }
            Directory.CreateDirectory(folder.Config);
            return folder;
        }

        /// <summary>Folders where a program should not keep its data (they are replaced on updates or need admin rights).</summary>
        public static bool IsProtected(string folder)
        {
            var full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Path.Combine(local, "Microsoft", "WinGet"),
                Path.Combine(local, "Microsoft", "WindowsApps"),
                Path.GetTempPath()
            };
            return roots.Where(r => r.Length > 0)
                .Any(r => full.StartsWith(r.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        }

        private static bool CanWrite(string folder)
        {
            try
            {
                var probe = Path.Combine(folder, $".nofences-write-test-{Environment.ProcessId}");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Data in the layout of older versions (fences.json directly in the folder).</summary>
        private static bool HasFlatData(string folder) =>
            File.Exists(Path.Combine(folder, "fences.json")) || File.Exists(Path.Combine(folder, "sync-folder.txt"));

        private static bool HasData(string folder) =>
            HasFlatData(folder) || Directory.Exists(Path.Combine(folder, ConfigName));

        /// <summary>Files of the flat layout and their place in the sorted one; null = left behind (temporary).</summary>
        private static string? SortedPlace(string name, bool isDirectory)
        {
            if (isDirectory)
            {
                return name.ToLowerInvariant() switch
                {
                    "backups" => BackupsName,
                    "themes" => ThemesName,
                    "note-media" or "clipboard" or "shelf" or "bookmarks" => Path.Combine(MediaName, name),
                    "webview2" => Path.Combine(CacheName, name),
                    _ => null
                };
            }
            var lower = name.ToLowerInvariant();
            if (lower is "fences.json" or "playtime.json" or "usage.json" or "sync-folder.txt" || lower.StartsWith("fences.json.broken-"))
                return Path.Combine(ConfigName, name);
            if (lower.StartsWith("log.txt"))
                return Path.Combine(LogsName, name);
            return null;
        }

        /// <summary>Sorts the flat layout of <paramref name="source"/> into this folder.</summary>
        private void SortFlat(string source, bool move)
        {
            foreach (var dir in Directory.EnumerateDirectories(source))
            {
                var name = Path.GetFileName(dir);
                if (SortedPlace(name, true) is not { } place || (!move && place.StartsWith(CacheName)))
                    continue;
                var target = Path.Combine(Root, place);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    if (move)
                        Directory.Move(dir, target);
                    else
                        CopyDirectory(dir, target);
                    MovedPaths.Add((dir, target));
                }
                catch (Exception)
                {
                    // In use (e.g. the web page cache): stays where it is, nothing depends on it
                }
            }
            foreach (var file in Directory.EnumerateFiles(source))
            {
                if (SortedPlace(Path.GetFileName(file), false) is not { } place)
                    continue;
                var target = Path.Combine(Root, place);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    if (move)
                        File.Move(file, target, overwrite: true);
                    else
                        File.Copy(file, target, overwrite: true);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>Copies the data of an older version (flat or already sorted) and marks the old folder.</summary>
        private void CopyFrom(string legacy)
        {
            if (HasFlatData(legacy))
            {
                SortFlat(legacy, move: false);
            }
            else
            {
                foreach (var name in new[] { ConfigName, BackupsName, ThemesName, MediaName, LogsName, LangName })
                {
                    var dir = Path.Combine(legacy, name);
                    if (Directory.Exists(dir))
                        CopyDirectory(dir, Path.Combine(Root, name));
                }
                MovedPaths.Add((Path.Combine(legacy, MediaName), Media));
            }
            if (!File.Exists(Path.Combine(Config, "fences.json")) && !File.Exists(Path.Combine(Config, "sync-folder.txt")))
                return; // copying failed: keep using nothing from here, the old folder stays untouched
            File.WriteAllText(Path.Combine(legacy, MovedMarker),
                $"The NoFences data now lives next to NoFences.exe:{Environment.NewLine}{Root}{Environment.NewLine}This folder is a copy and can be deleted.{Environment.NewLine}");
        }

        private static void CopyDirectory(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var file in Directory.EnumerateFiles(from))
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
            foreach (var dir in Directory.EnumerateDirectories(from))
                CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
        }

        /// <summary>A stored folder path after the move (shelf and bookmark fences point into the data folder).</summary>
        public string? MapMovedPath(string? path)
        {
            if (path == null)
                return null;
            foreach (var (from, to) in MovedPaths)
            {
                if (path.Equals(from, StringComparison.OrdinalIgnoreCase))
                    return to;
                if (path.StartsWith(from + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return to + path[from.Length..];
            }
            return path;
        }
    }
}
