using System.Text.Json;
using System.Text.Json.Nodes;

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
            // Check the marker in the file itself; the property's default value would let any JSON through.
            using (var doc = JsonDocument.Parse(json))
            {
                var marker = doc.RootElement.ValueKind == JsonValueKind.Object
                    ? doc.RootElement.EnumerateObject().FirstOrDefault(p => p.Name.Equals("Format", StringComparison.OrdinalIgnoreCase))
                    : default;
                if (marker.Value.ValueKind != JsonValueKind.String || marker.Value.GetString() != "nofences-export")
                    throw new FormatException("Not a NoFences export file.");
            }
            return JsonSerializer.Deserialize<FenceExport>(json, FenceStore.JsonOptions)
                   ?? throw new FormatException("Not a NoFences export file.");
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

        #region Network paths

        /// <summary>
        /// Network paths anywhere in the fences (links, folder, background picture, "open with", notes,
        /// widget settings …) as (fence name, path). Showing such a fence would make Windows log on to that
        /// server with the user's credentials, so an import from someone else asks first.
        /// </summary>
        public List<(string Fence, string Path)> NetworkPaths()
        {
            var found = new List<(string, string)>();
            foreach (var fence in Fences)
            {
                foreach (var text in AllStrings(JsonSerializer.SerializeToNode(fence, FenceStore.JsonOptions)))
                    found.AddRange(NetworkPath.FindIn(text).Select(p => (fence.Name, p)));
            }
            return found.Distinct().ToList();
        }

        /// <summary>
        /// Takes out everything that points to another computer: list entries are dropped, single settings
        /// cleared, lines of longer texts (notes) removed.
        /// </summary>
        public void RemoveNetworkPaths()
        {
            for (var i = 0; i < Fences.Count; i++)
            {
                var node = JsonSerializer.SerializeToNode(Fences[i], FenceStore.JsonOptions);
                Clean(node);
                var fence = node.Deserialize<FenceInfo>(FenceStore.JsonOptions) ?? new FenceInfo();
                fence.Name ??= "";
                fence.NoteText ??= "";
                Fences[i] = fence;
            }
        }

        private static IEnumerable<string> AllStrings(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject o:
                    foreach (var (name, value) in o)
                    {
                        yield return name; // dictionary keys are paths too (marks, item notes)
                        foreach (var s in AllStrings(value))
                            yield return s;
                    }
                    break;
                case JsonArray a:
                    foreach (var s in a.SelectMany(AllStrings))
                        yield return s;
                    break;
                case JsonValue v when v.TryGetValue<string>(out var text):
                    yield return text;
                    break;
            }
        }

        private static void Clean(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject o:
                    foreach (var name in o.Select(p => p.Key).ToList())
                    {
                        if (NetworkPath.FindIn(name).Any())
                            o.Remove(name);
                        else if (o[name] is JsonValue v && v.TryGetValue<string>(out var text))
                        {
                            var cleaned = CleanText(text);
                            if (cleaned != text)
                                o[name] = cleaned;
                        }
                        else
                            Clean(o[name]);
                    }
                    break;
                case JsonArray a:
                    for (var i = a.Count - 1; i >= 0; i--)
                    {
                        if (a[i] is JsonValue v && v.TryGetValue<string>(out var text))
                        {
                            var cleaned = CleanText(text);
                            if (cleaned == null)
                                a.RemoveAt(i);
                            else if (cleaned != text)
                                a[i] = cleaned;
                        }
                        else
                            Clean(a[i]);
                    }
                    break;
            }
        }

        /// <summary>The text without network paths: null for a single value, without those lines for a longer text.</summary>
        private static string? CleanText(string text)
        {
            if (!NetworkPath.FindIn(text).Any())
                return text;
            if (!text.Contains('\n'))
                return null;
            return string.Join('\n', text.Split('\n').Where(line => !NetworkPath.FindIn(line).Any()));
        }

        #endregion

        /// <summary>Writes styles that don't exist yet (only .json files); returns how many were added.</summary>
        public int WriteThemes(string themesFolder)
        {
            Directory.CreateDirectory(themesFolder);
            var added = 0;
            foreach (var (name, content) in Themes)
            {
                var fileName = Path.GetFileName(name);
                // Styles are JSON; anything else in a shared file has no business in the styles folder
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;
                var target = Path.Combine(themesFolder, fileName);
                if (File.Exists(target))
                    continue;
                File.WriteAllText(target, content);
                added++;
            }
            return added;
        }
    }
}
