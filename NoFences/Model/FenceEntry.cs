using System.Diagnostics;

namespace NoFences.Model
{
    public sealed class FenceEntry
    {
        private static readonly string[] AlwaysHiddenExtensions = { ".lnk", ".url", ".appref-ms" };

        public string Path { get; }

        public bool IsFolder { get; }

        private FenceEntry(string path, bool isFolder)
        {
            Path = path;
            IsFolder = isFolder;
        }

        public static FenceEntry? FromPath(string path)
        {
            // Network paths aren't touched until opened: every check would log on to that server (and an
            // unreachable one blocks for seconds). Folder or file is guessed from the name.
            if (NetworkPath.IsNetworkPath(path))
                return new FenceEntry(path, isFolder: path.EndsWith('\\') || !System.IO.Path.HasExtension(path));
            if (File.Exists(path))
                return new FenceEntry(path, false);
            if (Directory.Exists(path))
                return new FenceEntry(path, true);
            return null;
        }

        public string GetDisplayName(bool showExtensions)
        {
            if (IsFolder)
            {
                var name = System.IO.Path.GetFileName(Path.TrimEnd('\\'));
                return string.IsNullOrEmpty(name) ? Path : name; // drive roots
            }

            var ext = System.IO.Path.GetExtension(Path);
            if (!showExtensions || AlwaysHiddenExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            return System.IO.Path.GetFileName(Path);
        }

        /// <summary>Folders first, then by the chosen key. Manual keeps the given order.</summary>
        public static List<FenceEntry> Sort(List<FenceEntry> entries, FenceSortMode mode, IReadOnlyDictionary<string, int>? openCounts = null)
        {
            if (mode == FenceSortMode.Manual)
                return entries;

            var byName = StringComparer.CurrentCultureIgnoreCase;
            if (mode == FenceSortMode.MostUsed)
            {
                // Most opened first (folders and files mixed: what you use is what counts)
                return entries.OrderByDescending(e => openCounts?.GetValueOrDefault(e.Path) ?? 0)
                    .ThenBy(e => System.IO.Path.GetFileName(e.Path.TrimEnd('\\')), byName).ToList();
            }
            IOrderedEnumerable<FenceEntry> ordered = entries.OrderBy(e => !e.IsFolder);
            ordered = mode switch
            {
                FenceSortMode.Type => ordered.ThenBy(e => System.IO.Path.GetExtension(e.Path), byName),
                FenceSortMode.Modified => ordered.ThenByDescending(e => SafeInfo(e)?.LastWriteTimeUtc ?? DateTime.MinValue),
                FenceSortMode.Size => ordered.ThenByDescending(e => (SafeInfo(e) as FileInfo)?.Length ?? 0),
                _ => ordered
            };
            return ordered.ThenBy(e => System.IO.Path.GetFileName(e.Path.TrimEnd('\\')), byName).ToList();
        }

        private static FileSystemInfo? SafeInfo(FenceEntry e)
        {
            if (NetworkPath.IsNetworkPath(e.Path))
                return null;
            try
            {
                return e.IsFolder ? new DirectoryInfo(e.Path) : new FileInfo(e.Path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Opens the item without waiting for it (see <see cref="Util.Launcher"/>).</summary>
        public void Open() =>
            // .NET (Core) defaults UseShellExecute to false, which can't open documents.
            Util.Launcher.StartOrWarn(new ProcessStartInfo(Path) { UseShellExecute = true, WorkingDirectory = System.IO.Path.GetDirectoryName(Path) ?? "" });
    }
}
