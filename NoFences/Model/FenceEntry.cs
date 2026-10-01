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
        public static List<FenceEntry> Sort(List<FenceEntry> entries, FenceSortMode mode)
        {
            if (mode == FenceSortMode.Manual)
                return entries;

            var byName = StringComparer.CurrentCultureIgnoreCase;
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
            try
            {
                return e.IsFolder ? new DirectoryInfo(e.Path) : new FileInfo(e.Path);
            }
            catch
            {
                return null;
            }
        }

        public void Open()
        {
            try
            {
                // .NET (Core) defaults UseShellExecute to false, which can't open documents.
                Process.Start(new ProcessStartInfo(Path) { UseShellExecute = true, WorkingDirectory = System.IO.Path.GetDirectoryName(Path) ?? "" });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
