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
