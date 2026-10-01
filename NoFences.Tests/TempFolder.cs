namespace NoFences.Tests
{
    /// <summary>A fresh temporary folder per test, deleted afterwards.</summary>
    public sealed class TempFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NoFencesTests", Guid.NewGuid().ToString("N"));

        public TempFolder() => Directory.CreateDirectory(Path);

        public string File(string name, string content = "x", DateTime? modified = null)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(path, content);
            if (modified != null)
                System.IO.File.SetLastWriteTime(path, modified.Value);
            return path;
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }
}
