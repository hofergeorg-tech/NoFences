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
        private const string PortableMarker = "portable.txt";
        private const string LegacyMetaFileName = "__fence_metadata.xml";

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly System.Windows.Forms.Timer saveTimer = new() { Interval = 750 };

        public string DataDirectory { get; }

        public bool IsPortable { get; }

        public AppConfig Config { get; private set; } = new();

        private string ConfigPath => Path.Combine(DataDirectory, ConfigFileName);

        public FenceStore()
        {
            var exeDir = AppContext.BaseDirectory;
            IsPortable = File.Exists(Path.Combine(exeDir, PortableMarker)) || File.Exists(Path.Combine(exeDir, ConfigFileName));
            DataDirectory = IsPortable
                ? exeDir
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoFences");
            Directory.CreateDirectory(DataDirectory);

            saveTimer.Tick += (_, _) => SaveNow();
        }

        /// <summary>Store in a given folder (tests).</summary>
        public FenceStore(string dataDirectory)
        {
            DataDirectory = dataDirectory;
            IsPortable = true;
            Directory.CreateDirectory(DataDirectory);
            saveTimer.Tick += (_, _) => SaveNow();
        }

        public void Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    Config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions) ?? new AppConfig();
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
                File.WriteAllText(tmp, JsonSerializer.Serialize(Config, JsonOptions));
                File.Move(tmp, ConfigPath, overwrite: true);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Saving config failed: {e}");
            }
        }

        private string BackupDirectory => Path.Combine(DataDirectory, "backups");

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
            foreach (var dir in Directory.EnumerateDirectories(DataDirectory))
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
