namespace NoFences.Model
{
    /// <summary>A search result: an item in a fence, or a note whose text matches.</summary>
    public sealed record SearchItem(string Name, string? Path, string FenceName, FenceInfo Fence);

    /// <summary>Search across all fences: what's in them (links, folder contents, tabs) and note texts.</summary>
    public static class FenceSearch
    {
        /// <summary>Everything searchable; folder fences are read from disk (top level only).</summary>
        public static List<SearchItem> Collect(IEnumerable<FenceInfo> fences, bool showExtensions)
        {
            var items = new List<SearchItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fence in fences)
            {
                switch (fence.Kind)
                {
                    case FenceKind.Note when !string.IsNullOrWhiteSpace(fence.NoteText):
                        items.Add(new SearchItem(FirstLine(fence.NoteText!), null, fence.Name, fence));
                        break;
                    case FenceKind.Links:
                        var paths = fence.Files.Concat(fence.Tabs?.SelectMany(t => t.Files) ?? Enumerable.Empty<string>());
                        foreach (var path in paths)
                            Add(path);
                        break;
                    case FenceKind.Folder when Directory.Exists(fence.FolderPath):
                        try
                        {
                            foreach (var path in Directory.EnumerateFileSystemEntries(fence.FolderPath!).Take(500))
                                Add(path);
                        }
                        catch (Exception) { }
                        break;
                }

                void Add(string path)
                {
                    if (!seen.Add(path) || FenceEntry.FromPath(path) is not { } entry)
                        return;
                    items.Add(new SearchItem(entry.GetDisplayName(showExtensions), path, fence.Name, fence));
                }
            }
            return items;
        }

        private static string FirstLine(string text)
        {
            var line = text.Trim().Split('\n')[0].Trim();
            return line.Length > 80 ? line[..80] + "…" : line;
        }

        /// <summary>
        /// Matches ranked: name starts with the query, a word in it starts with it, contains it, then
        /// "fuzzy" (the letters in order, e.g. "ffx" → "Firefox"). Note texts are searched in full.
        /// </summary>
        public static List<SearchItem> Find(IEnumerable<SearchItem> items, string query, int max = 30)
        {
            query = query.Trim();
            if (query.Length == 0)
                return new();
            return items
                .Select(i => (Item: i, Rank: Rank(i, query)))
                .Where(x => x.Rank >= 0)
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.Item.Name.Length)
                .ThenBy(x => x.Item.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(max)
                .Select(x => x.Item)
                .ToList();
        }

        private static int Rank(SearchItem item, string query)
        {
            var name = item.Name;
            const StringComparison ic = StringComparison.CurrentCultureIgnoreCase;
            if (name.StartsWith(query, ic))
                return 0;
            if (name.Split(' ', '-', '_', '.').Any(w => w.StartsWith(query, ic)))
                return 1;
            if (name.Contains(query, ic))
                return 2;
            if (item.Path == null && item.Fence.NoteText?.Contains(query, ic) == true)
                return 3;
            return IsSubsequence(query, name) ? 4 : -1;
        }

        private static bool IsSubsequence(string query, string text)
        {
            var i = 0;
            foreach (var c in text)
            {
                if (i < query.Length && char.ToLowerInvariant(c) == char.ToLowerInvariant(query[i]))
                    i++;
            }
            return i == query.Length && query.Length >= 2;
        }
    }
}
