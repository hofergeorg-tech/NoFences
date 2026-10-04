using System.Security.Cryptography;

namespace NoFences.Model
{
    /// <summary>
    /// Finds files with identical content: same size first (cheap), then the same SHA-256. In each group
    /// the oldest file counts as the original; the others are the copies offered for removal.
    /// </summary>
    public static class DuplicateFinder
    {
        public sealed record FileItem(string Path, long Size, DateTime Modified);

        /// <summary>Groups of identical files (original first), the most wasted space first.</summary>
        public static List<List<FileItem>> Find(IEnumerable<FileItem> files, Func<string, string?> hash, CancellationToken cancel = default)
        {
            var groups = new List<List<FileItem>>();
            foreach (var sameSize in files.Where(f => f.Size > 0).GroupBy(f => f.Size).Where(g => g.Count() > 1))
            {
                cancel.ThrowIfCancellationRequested();
                var byHash = sameSize
                    .Select(f => (File: f, Hash: hash(f.Path)))
                    .Where(x => x.Hash != null)
                    .GroupBy(x => x.Hash)
                    .Where(g => g.Count() > 1);
                foreach (var same in byHash)
                    groups.Add(same.Select(x => x.File).OrderBy(f => f.Modified).ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList());
            }
            return groups.OrderByDescending(g => g[0].Size * (g.Count - 1)).ToList();
        }

        /// <summary>All files below the folders (no hidden/system files), at most <paramref name="max"/>.</summary>
        public static List<FileItem> Scan(IEnumerable<string> folders, int max = 50_000)
        {
            var result = new List<FileItem>();
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint };
            foreach (var folder in folders.Where(Directory.Exists))
            {
                foreach (var f in new DirectoryInfo(folder).EnumerateFiles("*", options))
                {
                    result.Add(new FileItem(f.FullName, f.Length, f.LastWriteTime));
                    if (result.Count >= max)
                        return result;
                }
            }
            // The same folder listed twice (or nested) must not report a file as its own copy
            return result.DistinctBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string? Sha256(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16);
                return Convert.ToHexString(SHA256.HashData(stream));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
