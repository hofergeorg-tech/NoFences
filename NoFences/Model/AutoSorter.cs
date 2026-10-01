using System.Diagnostics;
using System.IO.Enumeration;
using NoFences.Util;

namespace NoFences.Model
{
    /// <summary>
    /// Watches the user's desktop folder and sorts new files into the first fence whose
    /// <see cref="FenceInfo.AutoSortPatterns"/> match: folder fences get the file moved in,
    /// links fences get a link.
    /// </summary>
    public sealed class AutoSorter : IDisposable
    {
        public static readonly (Func<string> Name, string Patterns)[] Presets =
        {
            (() => Strings.PresetImages, "*.jpg; *.jpeg; *.png; *.gif; *.webp; *.bmp; *.heic; *.svg"),
            (() => Strings.PresetDocuments, "*.pdf; *.doc; *.docx; *.xls; *.xlsx; *.ppt; *.pptx; *.odt; *.ods; *.txt; *.md; *.csv"),
            (() => Strings.PresetArchives, "*.zip; *.rar; *.7z; *.tar; *.gz"),
            (() => Strings.PresetInstallers, "*.exe; *.msi; *.msix; *.appx"),
            (() => Strings.PresetVideos, "*.mp4; *.mkv; *.mov; *.avi; *.webm"),
            (() => Strings.PresetMusic, "*.mp3; *.flac; *.wav; *.m4a; *.ogg"),
            (() => Strings.PresetShortcuts, "*.lnk; *.url"),
        };

        // Files that are still being written or belong to the system.
        private static readonly string[] Ignored = { "*.tmp", "*.crdownload", "*.part", "*.partial", "*.download", "*.opdownload", "~$*", "desktop.ini", "thumbs.db" };

        private const int MaxAttempts = 60; // × interval ≈ 1.5 minutes for locked files

        private readonly Func<AppConfig> config;
        private readonly Action<FenceInfo> sorted;
        private readonly SynchronizationContext ui;
        private readonly FileSystemWatcher? watcher;
        private readonly Dictionary<string, int> pending = new(StringComparer.OrdinalIgnoreCase);
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 1500 };

        public string DesktopPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        /// <param name="config">Current config (read on every event, so changes apply immediately).</param>
        /// <param name="sorted">Called on the UI thread after a file went into a fence.</param>
        public AutoSorter(Func<AppConfig> config, Action<FenceInfo> sorted)
        {
            this.config = config;
            this.sorted = sorted;
            ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            timer.Tick += (_, _) => ProcessPending();

            if (!Directory.Exists(DesktopPath))
                return;
            watcher = new FileSystemWatcher(DesktopPath) { NotifyFilter = NotifyFilters.FileName, IncludeSubdirectories = false };
            watcher.Created += (_, e) => ui.Post(_ => Enqueue(e.FullPath), null);
            // Browsers download to *.crdownload etc. and rename when done.
            watcher.Renamed += (_, e) => ui.Post(_ => Enqueue(e.FullPath), null);
            watcher.EnableRaisingEvents = true;
        }

        public static bool Matches(string fileName, string? patterns)
        {
            if (string.IsNullOrWhiteSpace(patterns))
                return false;
            if (Ignored.Any(p => FileSystemName.MatchesSimpleExpression(p, fileName, ignoreCase: true)))
                return false;
            return patterns.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(p => FileSystemName.MatchesSimpleExpression(p, fileName, ignoreCase: true));
        }

        /// <summary>Sorts all files currently on the desktop. Returns how many were sorted.</summary>
        public int SortDesktopNow()
        {
            if (!Directory.Exists(DesktopPath))
                return 0;
            var count = 0;
            foreach (var file in new DirectoryInfo(DesktopPath).EnumerateFiles())
            {
                if ((file.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;
                if (!IsLocked(file.FullName) && TrySort(file.FullName))
                    count++;
            }
            return count;
        }

        private void Enqueue(string path)
        {
            if (!config().AutoSortEnabled || FindTarget(Path.GetFileName(path)) == null)
                return;
            pending[path] = 0;
            timer.Stop();
            timer.Start();
        }

        private void ProcessPending()
        {
            timer.Stop();
            foreach (var path in pending.Keys.ToList())
            {
                if (!File.Exists(path) || !config().AutoSortEnabled)
                {
                    pending.Remove(path);
                    continue;
                }
                if (IsLocked(path) && ++pending[path] < MaxAttempts)
                    continue;

                pending.Remove(path);
                TrySort(path);
            }
            if (pending.Count > 0)
                timer.Start();
        }

        private FenceInfo? FindTarget(string fileName)
        {
            foreach (var fence in config().Fences)
            {
                if (!Matches(fileName, fence.AutoSortPatterns))
                    continue;
                if (fence.Kind == FenceKind.Links)
                    return fence;
                if (Directory.Exists(fence.FolderPath) && !SamePath(fence.FolderPath!, DesktopPath))
                    return fence;
            }
            return null;
        }

        private bool TrySort(string path)
        {
            var fence = FindTarget(Path.GetFileName(path));
            if (fence == null)
                return false;

            try
            {
                if (fence.Kind == FenceKind.Folder)
                {
                    var target = UniquePath(fence.FolderPath!, Path.GetFileName(path));
                    File.Move(path, target);
                    fence.Files.Add(target);
                }
                else
                {
                    if (fence.Files.Contains(path, StringComparer.OrdinalIgnoreCase))
                        return false;
                    fence.Files.Add(path);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Auto-sort of {path} failed: {e.Message}");
                return false;
            }

            sorted(fence);
            return true;
        }

        /// <summary>"name.ext" → "name (2).ext" if taken, like Explorer does.</summary>
        private static string UniquePath(string dir, string fileName)
        {
            var target = Path.Combine(dir, fileName);
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            for (var i = 2; File.Exists(target) || Directory.Exists(target); i++)
                target = Path.Combine(dir, $"{stem} ({i}){ext}");
            return target;
        }

        private static bool IsLocked(string path)
        {
            try
            {
                using var _ = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        private static bool SamePath(string a, string b) =>
            string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

        public void Dispose()
        {
            watcher?.Dispose();
            timer.Dispose();
        }
    }
}
