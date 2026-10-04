using System.Text.Json;
using NoFences.Util;

namespace NoFences.Model
{
    /// <summary>
    /// The logic behind fading, the shelf, browser bookmarks, recent folders, moving fences between
    /// monitors and per-fence shortcuts – kept apart from the windows so it can be tested.
    /// </summary>
    public static class FenceExtras
    {
        #region Fading

        /// <summary>Fully visible up to this distance (logical px) from the mouse …</summary>
        public const int FadeNear = 120;

        /// <summary>… and faded to <see cref="FadeMin"/> from this distance on.</summary>
        public const int FadeFar = 600;

        public const double FadeMin = 0.25;

        /// <summary>Opacity for a fence the mouse is <paramref name="distance"/> px away from.</summary>
        public static double FadeOpacity(double distance)
        {
            if (distance <= FadeNear)
                return 1;
            if (distance >= FadeFar)
                return FadeMin;
            var t = (distance - FadeNear) / (FadeFar - FadeNear);
            return Math.Round(1 - t * (1 - FadeMin), 2);
        }

        /// <summary>Shortest distance from a point to a rectangle (0 inside).</summary>
        public static double Distance(Rectangle r, Point p)
        {
            var dx = Math.Max(Math.Max(r.Left - p.X, 0), p.X - r.Right);
            var dy = Math.Max(Math.Max(r.Top - p.Y, 0), p.Y - r.Bottom);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        #region Shelf

        /// <summary>Items in <paramref name="folder"/> that weren't touched for <paramref name="days"/> days.</summary>
        public static List<string> ExpiredItems(string folder, int days, DateTime now)
        {
            if (days <= 0 || !Directory.Exists(folder))
                return new();
            var limit = now.AddDays(-days);
            try
            {
                return new DirectoryInfo(folder).EnumerateFileSystemInfos()
                    .Where(f => (f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    // Put on the shelf = created there (a moved file keeps its old write time)
                    .Where(f => Max(f.CreationTime, f.LastWriteTime) < limit)
                    .Select(f => f.FullName)
                    .ToList();
            }
            catch (IOException)
            {
                return new();
            }
            catch (UnauthorizedAccessException)
            {
                return new();
            }

            static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
        }

        public static readonly int[] ShelfDayChoices = { 0, 1, 3, 7, 14, 30 };

        #endregion

        #region Browser bookmarks

        public sealed record Bookmark(string Name, string Url, string? Folder);

        public static readonly (string Id, string Name, string Path)[] Browsers =
        {
            ("chrome", "Google Chrome", @"Google\Chrome\User Data\Default\Bookmarks"),
            ("edge", "Microsoft Edge", @"Microsoft\Edge\User Data\Default\Bookmarks"),
            ("brave", "Brave", @"BraveSoftware\Brave-Browser\User Data\Default\Bookmarks"),
            ("vivaldi", "Vivaldi", @"Vivaldi\User Data\Default\Bookmarks"),
            ("opera", "Opera", @"..\Roaming\Opera Software\Opera Stable\Bookmarks")
        };

        public static string BookmarksFile(string browserId)
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var entry = Browsers.FirstOrDefault(b => b.Id == browserId);
            return entry.Path == null ? "" : Path.GetFullPath(Path.Combine(local, entry.Path));
        }

        /// <summary>Browsers whose bookmarks file exists on this PC.</summary>
        public static IEnumerable<(string Id, string Name)> InstalledBrowsers() =>
            Browsers.Where(b => File.Exists(BookmarksFile(b.Id))).Select(b => (b.Id, b.Name));

        /// <summary>
        /// The bookmarks bar of a Chromium browser (Chrome, Edge, Brave …). Folders on the bar become
        /// one level of subfolders; anything nested deeper lands in that folder too.
        /// </summary>
        public static List<Bookmark> ParseChromiumBookmarks(string json)
        {
            var result = new List<Bookmark>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("roots", out var roots) || !roots.TryGetProperty("bookmark_bar", out var bar))
                    return result;
                Walk(bar, null);
            }
            catch (JsonException)
            {
            }
            return result;

            void Walk(JsonElement node, string? folder)
            {
                if (!node.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
                    return;
                foreach (var child in children.EnumerateArray())
                {
                    var type = child.TryGetProperty("type", out var t) ? t.GetString() : null;
                    var name = child.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (type == "url" && child.TryGetProperty("url", out var u) && u.GetString() is { Length: > 0 } url)
                        result.Add(new Bookmark(name.Length > 0 ? name : url, url, folder));
                    else if (type == "folder")
                        Walk(child, folder ?? name);
                }
            }
        }

        /// <summary>A name Windows accepts as file name (without extension).</summary>
        public static string SafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
            if (clean.Length > 80)
                clean = clean[..80].Trim();
            return clean.Length == 0 ? "_" : clean;
        }

        public static string UrlFileContent(string url) => $"[InternetShortcut]\r\nURL={url}\r\n";

        /// <summary>
        /// Writes the bookmarks as .url files into <paramref name="folder"/> (subfolders for bookmark
        /// folders) and removes what is no longer bookmarked. Returns whether anything changed.
        /// </summary>
        public static bool WriteBookmarks(string folder, IReadOnlyList<Bookmark> bookmarks)
        {
            Directory.CreateDirectory(folder);
            var wanted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in bookmarks)
            {
                var dir = b.Folder == null ? folder : Path.Combine(folder, SafeFileName(b.Folder));
                var file = Path.Combine(dir, SafeFileName(b.Name) + ".url");
                // Two bookmarks with the same name: number them
                for (var i = 2; wanted.ContainsKey(file); i++)
                    file = Path.Combine(dir, $"{SafeFileName(b.Name)} ({i}).url");
                wanted[file] = UrlFileContent(b.Url);
            }

            var changed = false;
            foreach (var (file, content) in wanted)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                if (File.Exists(file) && File.ReadAllText(file) == content)
                    continue;
                File.WriteAllText(file, content);
                changed = true;
            }
            foreach (var file in Directory.EnumerateFiles(folder, "*.url", SearchOption.AllDirectories).ToList())
            {
                if (!wanted.ContainsKey(file))
                {
                    File.Delete(file);
                    changed = true;
                }
            }
            foreach (var dir in Directory.EnumerateDirectories(folder).ToList())
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    changed = true;
                }
            }
            return changed;
        }

        #endregion

        #region Recent folders

        /// <summary>
        /// Folders from Windows' recent items, newest first: opened folders themselves and the folders
        /// of opened files. <paramref name="resolve"/> turns a .lnk into its target.
        /// </summary>
        public static List<string> RecentFolders(IEnumerable<(string Lnk, DateTime Time)> links, Func<string, string?> resolve, Func<string, bool> folderExists, int count)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (lnk, _) in links.OrderByDescending(l => l.Time))
            {
                var target = resolve(lnk);
                if (string.IsNullOrWhiteSpace(target))
                    continue;
                var folder = folderExists(target) ? target : Path.GetDirectoryName(target);
                if (folder == null || !folderExists(folder) || !seen.Add(folder.TrimEnd('\\')))
                    continue;
                result.Add(folder);
                if (result.Count >= count)
                    break;
            }
            return result;
        }

        #endregion

        #region Monitors

        /// <summary>Same place relative to the working area, on another monitor (kept inside it).</summary>
        public static Rectangle MoveToScreen(Rectangle bounds, Rectangle from, Rectangle to)
        {
            var fx = from.Width > bounds.Width ? (double)(bounds.Left - from.Left) / (from.Width - bounds.Width) : 0;
            var fy = from.Height > bounds.Height ? (double)(bounds.Top - from.Top) / (from.Height - bounds.Height) : 0;
            fx = Math.Clamp(fx, 0, 1);
            fy = Math.Clamp(fy, 0, 1);
            var width = Math.Min(bounds.Width, to.Width);
            var height = Math.Min(bounds.Height, to.Height);
            var x = to.Left + (int)Math.Round(fx * (to.Width - width));
            var y = to.Top + (int)Math.Round(fy * (to.Height - height));
            return new Rectangle(x, y, width, height);
        }

        #endregion

        #region Templates

        public static readonly string[] TemplateIds = { "gaming", "office", "minimal" };

        /// <summary>The fences of a ready-made setup, placed in columns from the top left of <paramref name="area"/>.</summary>
        public static List<FenceInfo> Template(string id, Rectangle area)
        {
            var fences = id switch
            {
                "gaming" => new List<FenceInfo>
                {
                    new() { Name = Strings.TemplateGamesFence, Kind = FenceKind.Links, Compact = true, IconSize = 48, Theme = "gaming", Width = 440, Height = 120 },
                    Widget("games", "gaming"),
                    Widget("system", "gaming"),
                    Widget("steamdeals", "gaming"),
                },
                "office" => new List<FenceInfo>
                {
                    new() { Name = Strings.TemplateWorkFence, Kind = FenceKind.Links, Theme = "work", Width = 340, Height = 300 },
                    Widget("clock", "work"),
                    Widget("todo", "work"),
                    new() { Name = Strings.NoteName, Kind = FenceKind.Note, Theme = "postit", Width = 260, Height = 240, TitleHeight = 30 },
                },
                _ => new List<FenceInfo>
                {
                    Widget("clock", null),
                    new() { Name = Strings.QuickLaunchName, Kind = FenceKind.Links, Compact = true, IconSize = 48, Width = 420, Height = 110 },
                }
            };

            // Top to bottom, then the next column
            const int gap = 16;
            int x = area.Left + 24, y = area.Top + 24, columnWidth = 0;
            foreach (var f in fences)
            {
                if (y + f.Height > area.Bottom && y > area.Top + 24)
                {
                    x += columnWidth + gap;
                    y = area.Top + 24;
                    columnWidth = 0;
                }
                f.PosX = x;
                f.PosY = y;
                y += f.Height + gap;
                columnWidth = Math.Max(columnWidth, f.Width);
            }
            return fences;

            static FenceInfo Widget(string type, string? theme)
            {
                var (_, name, size) = Widgets.WidgetRegistry.Types.First(t => t.Type == type);
                return new FenceInfo { Name = name(), Kind = FenceKind.Widget, WidgetType = type, Theme = theme, Width = size.Width, Height = size.Height };
            }
        }

        #endregion

        #region Per-fence shortcuts

        /// <summary>Ctrl+Shift+F1 … F12 (free in most programs, and AltGr can't trigger them).</summary>
        public static readonly IReadOnlyList<string> FenceHotkeys =
            Enumerable.Range(1, 12).Select(i => $"Ctrl+Shift+F{i}").ToList();

        /// <summary>The function key of "Ctrl+Shift+F5", or null.</summary>
        public static Keys? FenceHotkeyKey(string? hotkey)
        {
            if (hotkey == null || !hotkey.StartsWith("Ctrl+Shift+F", StringComparison.Ordinal))
                return null;
            return int.TryParse(hotkey["Ctrl+Shift+F".Length..], out var n) && n is >= 1 and <= 12 ? Keys.F1 + (n - 1) : null;
        }

        #endregion
    }
}
