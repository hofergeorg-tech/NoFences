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

        private static readonly JsonSerializerOptions JsonOptions = new()
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

        /// <summary>Reads fences written by NoFences 1.x (one folder per fence with an XML file).</summary>
        private List<FenceInfo> LoadLegacyFences()
        {
            var result = new List<FenceInfo>();
            var serializer = new XmlSerializer(typeof(FenceInfo));
            foreach (var dir in Directory.EnumerateDirectories(DataDirectory))
            {
                var metaFile = Path.Combine(dir, LegacyMetaFileName);
                if (!File.Exists(metaFile))
                    continue;
                try
                {
                    using var reader = new StreamReader(metaFile);
                    if (serializer.Deserialize(reader) is FenceInfo fence)
                        result.Add(fence);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Skipping legacy fence {metaFile}: {e}");
                }
            }
            return result;
        }

        private static void TryCopy(string from, string to)
        {
            try { File.Copy(from, to, overwrite: true); } catch { }
        }
    }
}
