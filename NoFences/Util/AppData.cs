namespace NoFences.Util
{
    /// <summary>
    /// Where NoFences keeps files besides the config (note images, voice notes, pinned clipboard
    /// images). Set once at startup; null in tests and the preview renderer, which then store nothing.
    /// </summary>
    public static class AppData
    {
        public static string? Folder { get; set; }

        /// <summary>Temporary files. Set at startup; found on demand otherwise.</summary>
        public static string? CacheFolder { get; set; }

        public static string Cache => CacheFolder ??= Model.DataFolder.Find().Cache;

        /// <summary>Folder for note images and voice notes.</summary>
        public const string MediaFolderName = "note-media";

        /// <summary>A stored relative path ("note-media/x.png") as a full path; full paths stay as they are.</summary>
        public static string Resolve(string path) =>
            Path.IsPathRooted(path) || Folder == null ? path : Path.Combine(Folder, path.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>A new file in the media folder: (full path, path to store in the note).</summary>
        public static (string Full, string Stored)? NewMediaFile(string prefix, string extension)
        {
            if (Folder == null)
                return null;
            var dir = Path.Combine(Folder, MediaFolderName);
            Directory.CreateDirectory(dir);
            var name = $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}{extension}";
            return (Path.Combine(dir, name), $"{MediaFolderName}/{name}");
        }
    }
}
