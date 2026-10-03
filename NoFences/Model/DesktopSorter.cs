namespace NoFences.Model
{
    public enum DesktopCategory { Games, Programs, Documents, Images, Media, Archives, Folders, Other }

    /// <summary>
    /// Sorts desktop items into categories for the desktop assistant. Shortcuts count as games when they
    /// point into a game library (Steam, Epic, GOG, Xbox, Riot, Battle.net, Ubisoft, EA, RSI …).
    /// </summary>
    public static class DesktopSorter
    {
        private static readonly string[] DocumentTypes = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp", ".txt", ".rtf", ".md", ".csv", ".one", ".epub" };
        private static readonly string[] ImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".svg", ".psd", ".tif", ".tiff", ".raw", ".cr2", ".nef" };
        private static readonly string[] MediaTypes = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".aac" };
        private static readonly string[] ArchiveTypes = { ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".cab" };
        private static readonly string[] ProgramTypes = { ".lnk", ".exe", ".appref-ms", ".url", ".bat", ".cmd", ".msi" };

        private static readonly string[] GameMarkers =
        {
            "steam://", "steamapps", "com.epicgames.launcher", "epic games", "gog galaxy", "goggalaxy://", "xboxgames",
            "riot games", "riotclient", "battle.net", "battlenet://", "ubisoft", "uplay://", "ea games", "origin2://",
            "roberts space industries", "rsi launcher", "rockstar games", "minecraft"
        };

        /// <summary>The category of a desktop item. <paramref name="target"/> is what a shortcut points to (or null).</summary>
        public static DesktopCategory Categorize(string path, bool isFolder, string? target)
        {
            if (isFolder)
                return DesktopCategory.Folders;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ProgramTypes.Contains(ext))
            {
                var haystack = $"{target} {path}".ToLowerInvariant();
                return GameMarkers.Any(haystack.Contains) ? DesktopCategory.Games : DesktopCategory.Programs;
            }
            if (DocumentTypes.Contains(ext))
                return DesktopCategory.Documents;
            if (ImageTypes.Contains(ext))
                return DesktopCategory.Images;
            if (MediaTypes.Contains(ext))
                return DesktopCategory.Media;
            if (ArchiveTypes.Contains(ext))
                return DesktopCategory.Archives;
            return DesktopCategory.Other;
        }

        /// <summary>A fitting built-in style per category.</summary>
        public static string ThemeFor(DesktopCategory category) => category switch
        {
            DesktopCategory.Games => "gaming",
            DesktopCategory.Documents => "documents",
            DesktopCategory.Images => "photos",
            DesktopCategory.Media => "multimedia",
            DesktopCategory.Programs => "windows",
            _ => "default"
        };

        /// <summary>The URL of an internet shortcut (.url), read from its [InternetShortcut] section.</summary>
        public static string? ReadUrlShortcut(string path)
        {
            try
            {
                return File.ReadLines(path).FirstOrDefault(l => l.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))?[4..];
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
