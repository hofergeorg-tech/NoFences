using System.Text.RegularExpressions;

namespace NoFences.Model
{
    /// <summary>
    /// Paths on another computer (\\server\share, file://server/…). Just touching one – an icon, a
    /// thumbnail, "does it exist?" – makes Windows log on to that server with the user's credentials
    /// (NTLM), so paths that came from someone else must not be touched without asking.
    /// </summary>
    public static partial class NetworkPath
    {
        public static bool IsNetworkPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            var p = path.Trim().Trim('"');
            if (p.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                return true;
            // \\?\C:\… is a long local path, \\.\ a local device
            if (p.StartsWith(@"\\?\", StringComparison.Ordinal) || p.StartsWith(@"\\.\", StringComparison.Ordinal))
                return false;
            if (p.StartsWith(@"\\", StringComparison.Ordinal) || p.StartsWith("//", StringComparison.Ordinal))
                return p.Length > 2 && p[2] is not ('\\' or '/');
            return p.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                   && Uri.TryCreate(p, UriKind.Absolute, out var uri) && uri.IsUnc;
        }

        // Where a path may start inside a text: not right after a letter, ':' or a slash (that's "https://", "C:\")
        [GeneratedRegex(@"(?<![\w:/\\])(?:\\\\|//|file:)[^\s""'<>|()]+", RegexOptions.IgnoreCase)]
        private static partial Regex Candidates();

        /// <summary>All network paths inside a text (a note, a widget's settings).</summary>
        public static IEnumerable<string> FindIn(string? text) =>
            string.IsNullOrEmpty(text)
                ? Enumerable.Empty<string>()
                : Candidates().Matches(text).Select(m => m.Value).Where(IsNetworkPath);
    }
}
