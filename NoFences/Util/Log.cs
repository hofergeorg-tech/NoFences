namespace NoFences.Util
{
    /// <summary>
    /// A small text log (log.txt next to the config) for things that fail quietly, like a feed that can't be
    /// loaded. Kept under 256 KB; never throws.
    /// </summary>
    public static class Log
    {
        private const long MaxBytes = 256 * 1024;
        private static readonly object Gate = new();

        /// <summary>Set once at startup; null = don't log (tests, preview).</summary>
        public static string? Folder { get; set; }

        public static void Write(string area, string message)
        {
            if (Folder == null)
                return;
            try
            {
                lock (Gate)
                {
                    var path = Path.Combine(Folder, "log.txt");
                    if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                        File.Move(path, path + ".old", overwrite: true);
                    File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {area}: {message}{Environment.NewLine}");
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>The most useful part of an exception for a one-line log entry and a widget hint.</summary>
        public static string Describe(Exception e)
        {
            var inner = e;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner == e ? $"{e.GetType().Name}: {e.Message}" : $"{e.GetType().Name}: {e.Message} ({inner.GetType().Name}: {inner.Message})";
        }
    }
}
