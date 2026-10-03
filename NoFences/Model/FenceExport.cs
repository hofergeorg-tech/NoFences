using System.Text.Json;

namespace NoFences.Model
{
    /// <summary>
    /// Fences (and the user styles they use) in one file, to move a setup to another PC or share it.
    /// Pure logic, unit-tested; the dialogs live in NoFencesApp.
    /// </summary>
    public sealed class FenceExport
    {
        public string Format { get; set; } = "nofences-export";
        public int Version { get; set; } = 1;
        public List<FenceInfo> Fences { get; set; } = new();

        /// <summary>User style files by file name.</summary>
        public Dictionary<string, string> Themes { get; set; } = new();

        public string ToJson() => JsonSerializer.Serialize(this, FenceStore.JsonOptions);

        public static FenceExport FromJson(string json)
        {
            var export = JsonSerializer.Deserialize<FenceExport>(json, FenceStore.JsonOptions);
            if (export == null || export.Format != "nofences-export")
                throw new FormatException("Not a NoFences export file.");
            return export;
        }

        public static FenceExport Create(IEnumerable<FenceInfo> fences, string? themesFolder)
        {
            var export = new FenceExport { Fences = fences.ToList() };
            if (themesFolder != null && Directory.Exists(themesFolder))
            {
                foreach (var file in Directory.EnumerateFiles(themesFolder, "*.json"))
                    export.Themes[Path.GetFileName(file)] = File.ReadAllText(file);
            }
            return export;
        }

        /// <summary>
        /// Copies of the fences ready to add: new ids (so importing twice doesn't clash) and no
        /// monitor layouts (they belong to the other PC's screens).
        /// </summary>
        public List<FenceInfo> PrepareForImport()
        {
            var json = JsonSerializer.Serialize(Fences, FenceStore.JsonOptions);
            var copies = JsonSerializer.Deserialize<List<FenceInfo>>(json, FenceStore.JsonOptions) ?? new();
            foreach (var f in copies)
            {
                f.Id = Guid.NewGuid();
                f.Layouts.Clear();
                f.VirtualDesktop = null;
            }
            return copies;
        }

        /// <summary>Writes styles that don't exist yet; returns how many were added.</summary>
        public int WriteThemes(string themesFolder)
        {
            Directory.CreateDirectory(themesFolder);
            var added = 0;
            foreach (var (name, content) in Themes)
            {
                var target = Path.Combine(themesFolder, Path.GetFileName(name));
                if (File.Exists(target))
                    continue;
                File.WriteAllText(target, content);
                added++;
            }
            return added;
        }
    }
}
