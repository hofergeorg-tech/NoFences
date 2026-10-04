using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace NoFences.Model
{
    /// <summary>
    /// Loads and saves the configuration as a single JSON file. Writes are debounced and
    /// atomic (temp file + replace), so a crash mid-save can't corrupt the config.
    /// </summary>
    public class FenceStore
    {
        private const string ConfigFileName = "fences.json";
        private const string LegacyMetaFileName = "__fence_metadata.xml";

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly System.Windows.Forms.Timer saveTimer = new() { Interval = 750 };

        /// <summary>Where fences.json, playtime.json and usage.json are: the sync folder or the local config folder.</summary>
        public string DataDirectory { get; }

        /// <summary>The local data folder with its subfolders.</summary>
        public DataFolder Folder { get; }

        /// <summary>Custom styles (shared when syncing).</summary>
        public string ThemesDirectory => SyncFolder != null ? Path.Combine(SyncFolder, DataFolder.ThemesName) : Folder.Themes;

        /// <summary>Note pictures, voice notes, pinned clipboard pictures (shared when syncing).</summary>
        public string MediaDirectory => SyncFolder ?? Folder.Media;

        public AppConfig Config { get; private set; } = new();

        private string ConfigPath => Path.Combine(DataDirectory, ConfigFileName);

        public FenceStore() : this(DataFolder.Prepare())
        {
        }

        public FenceStore(DataFolder folder)
        {
            Folder = folder;
            Directory.CreateDirectory(LocalDirectory);
            SyncFolder = ReadSyncPointer(LocalDirectory);
            DataDirectory = SyncFolder ?? LocalDirectory;
            saveTimer.Tick += (_, _) => SaveNow();
        }

        /// <summary>Store in a given folder (tests).</summary>
        public FenceStore(string root) : this(new DataFolder(root, true))
        {
        }

        #region Sync folder

        private const string SyncPointerFile = "sync-folder.txt";

        /// <summary>Where the config lives without sync (and where the sync pointer is kept).</summary>
        public string LocalDirectory => Folder.Config;

        /// <summary>A folder shared with other PCs (e.g. in OneDrive), or null.</summary>
        public string? SyncFolder { get; }

        /// <summary>The files that move into a sync folder.</summary>
        public static readonly string[] SyncedFiles = { ConfigFileName, "playtime.json" };

        private static string? ReadSyncPointer(string localDirectory)
        {
            try
            {
                var pointer = Path.Combine(localDirectory, SyncPointerFile);
                if (!File.Exists(pointer))
                    return null;
                var folder = File.ReadAllText(pointer).Trim();
                // An unavailable folder (OneDrive not signed in yet) falls back to the local data
                return folder.Length > 0 && Directory.Exists(folder) ? folder : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Points this PC at <paramref name="folder"/>. Unless <paramref name="useExisting"/>, this PC's
        /// files are copied there first. Takes effect after a restart.
        /// </summary>
        public void StartSync(string folder, bool useExisting)
        {
            SaveNow();
            Directory.CreateDirectory(folder);
            if (!useExisting)
                CopyData(DataDirectory, ThemesDirectory, folder, Path.Combine(folder, DataFolder.ThemesName));
            File.WriteAllText(Path.Combine(LocalDirectory, SyncPointerFile), folder);
        }

        /// <summary>Copies the shared data back to this PC and stops syncing (after a restart).</summary>
        public void StopSync()
        {
            SaveNow();
            if (SyncFolder != null)
                CopyData(SyncFolder, ThemesDirectory, LocalDirectory, Folder.Themes);
            File.Delete(Path.Combine(LocalDirectory, SyncPointerFile));
        }

        private static void CopyData(string from, string fromThemes, string to, string toThemes)
        {
            if (string.Equals(Path.GetFullPath(from), Path.GetFullPath(to), StringComparison.OrdinalIgnoreCase))
                return;
            foreach (var name in SyncedFiles)
                TryCopy(Path.Combine(from, name), Path.Combine(to, name));
            if (Directory.Exists(fromThemes))
            {
                Directory.CreateDirectory(toThemes);
                foreach (var file in Directory.EnumerateFiles(fromThemes, "*.json"))
                    TryCopy(file, Path.Combine(toThemes, Path.GetFileName(file)));
            }
        }

        public static bool HasConfig(string folder) => File.Exists(Path.Combine(folder, ConfigFileName));

        private string? lastWritten;

        /// <summary>Whether fences.json on disk is something else than what this PC wrote last (another PC saved).</summary>
        public bool ChangedOnDisk()
        {
            try
            {
                return File.Exists(ConfigPath) && lastWritten != null && File.ReadAllText(ConfigPath) != lastWritten;
            }
            catch (IOException)
            {
                return false; // being written right now; the next check sees it
            }
        }

        #endregion


        public void Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                    lastWritten = json;
                    if (Folder.MovedPaths.Count > 0)
                        ApplyMovedPaths();
                    return;
                }
                catch (Exception e)
                {
                    // Keep the broken file for inspection instead of silently overwriting it.
                    Debug.WriteLine($"Config unreadable: {e}");
                    TryCopy(ConfigPath, ConfigPath + $".broken-{DateTime.Now:yyyyMMdd-HHmmss}");
                }
            }

            Config = new AppConfig { Fences = LoadLegacyFences() };
            if (Config.Fences.Count > 0)
                SaveNow();
        }

        /// <summary>Schedules a save; repeated calls within the interval are coalesced.</summary>
        public void RequestSave()
        {
            saveTimer.Stop();
            saveTimer.Start();
        }

        public void SaveNow()
        {
            saveTimer.Stop();
            try
            {
                var tmp = ConfigPath + ".tmp";
                var json = JsonSerializer.Serialize(Config, JsonOptions);
                File.WriteAllText(tmp, json);
                File.Move(tmp, ConfigPath, overwrite: true);
                lastWritten = json;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Saving config failed: {e}");
            }
        }

        /// <summary>Shelf and bookmark fences whose folder moved with the data now point to the new place.</summary>
        private void ApplyMovedPaths()
        {
            var changed = false;
            foreach (var fence in Config.Fences)
            {
                var path = Folder.MapMovedPath(fence.FolderPath);
                if (path != fence.FolderPath)
                {
                    fence.FolderPath = path;
                    changed = true;
                }
            }
            if (changed)
                SaveNow();
        }

        private string BackupDirectory => SyncFolder != null ? Path.Combine(SyncFolder, DataFolder.BackupsName) : Folder.Backups;

        private const int KeepBackups = 10;

        /// <summary>Copies the config into backups/ if the newest backup is older than 12 hours; keeps the last 10.</summary>
        public void BackupIfDue()
        {
            if (!File.Exists(ConfigPath))
                return;
            var newest = ListBackups().FirstOrDefault();
            if (newest.Path != null && DateTime.Now - newest.Time < TimeSpan.FromHours(12))
                return;
            try
            {
                Directory.CreateDirectory(BackupDirectory);
                File.Copy(ConfigPath, Path.Combine(BackupDirectory, $"fences-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"), overwrite: true);
                foreach (var old in ListBackups().Skip(KeepBackups))
                    File.Delete(old.Path);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Backup failed: {e.Message}");
            }
        }

        /// <summary>Backups, newest first.</summary>
        public IEnumerable<(string Path, DateTime Time)> ListBackups()
        {
            if (!Directory.Exists(BackupDirectory))
                return Enumerable.Empty<(string, DateTime)>();
            return new DirectoryInfo(BackupDirectory).GetFiles("fences-*.json")
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => (f.FullName, f.LastWriteTime))
                .ToList();
        }

        /// <summary>Replaces the config with a backup (the current config is backed up first). Takes effect after a restart.</summary>
        public void RestoreBackup(string backupPath)
        {
            // Validate before touching anything.
            JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(backupPath), JsonOptions);
            SaveNow();
            Directory.CreateDirectory(BackupDirectory);
            File.Copy(ConfigPath, Path.Combine(BackupDirectory, $"fences-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-before-restore.json"), overwrite: true);
            File.Copy(backupPath, ConfigPath, overwrite: true);
        }

        /// <summary>Reads fences written by NoFences 1.x (one folder per fence with an XML file).</summary>
        private List<FenceInfo> LoadLegacyFences()
        {
            var result = new List<FenceInfo>();
            var serializer = new XmlSerializer(typeof(LegacyFenceInfo), new XmlRootAttribute("FenceInfo"));
            var folders = new[] { Folder.Root, DataFolder.LegacyFolder }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var dir in folders.SelectMany(Directory.EnumerateDirectories))
            {
                var metaFile = Path.Combine(dir, LegacyMetaFileName);
                if (!File.Exists(metaFile))
                    continue;
                try
                {
                    using var reader = new StreamReader(metaFile);
                    if (serializer.Deserialize(reader) is LegacyFenceInfo old)
                        result.Add(old.ToFenceInfo());
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Skipping legacy fence {metaFile}: {e}");
                }
            }
            return result;
        }

        /// <summary>
        /// Exactly the fields NoFences 1.x wrote. Kept separate from <see cref="FenceInfo"/>, which has
        /// grown types XmlSerializer can't handle (dictionaries) — using it here crashed every first start.
        /// </summary>
        public class LegacyFenceInfo
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = "";
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int Width { get; set; } = 300;
            public int Height { get; set; } = 300;
            public bool Locked { get; set; }
            public bool CanMinify { get; set; }
            public int TitleHeight { get; set; } = 35;
            public List<string> Files { get; set; } = new();

            public FenceInfo ToFenceInfo() => new()
            {
                Id = Id, Name = Name, PosX = PosX, PosY = PosY, Width = Width, Height = Height,
                Locked = Locked, CanMinify = CanMinify, TitleHeight = TitleHeight, Files = Files
            };
        }

        private static void TryCopy(string from, string to)
        {
            try { File.Copy(from, to, overwrite: true); } catch { }
        }
    }
}
