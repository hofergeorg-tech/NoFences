using System.Text.RegularExpressions;

namespace NoFences.Model
{
    /// <summary>Text logic of note fences: checkboxes and links. Pure functions, unit-tested.</summary>
    public static partial class NoteText
    {
        /// <summary>"[ ] ", "[x] ", optionally after "- " or "* ".</summary>
        [GeneratedRegex(@"^(\s*(?:[-*]\s+)?)\[([ xX])\]\s?")]
        public static partial Regex CheckboxPrefix();

        // Web addresses and Windows/UNC paths.
        [GeneratedRegex(@"(https?://[^\s<>""]+|www\.[^\s<>""]+|[A-Za-z]:\\[^\s<>""|?*]+|\\\\[^\s<>""|?*]+)", RegexOptions.IgnoreCase)]
        private static partial Regex LinkPattern();

        public static string[] Lines(string text) => text.Split('\n');

        /// <summary>Flips "[ ]" and "[x]" on the given line; other lines are left alone.</summary>
        public static string ToggleCheckbox(string text, int line)
        {
            var lines = Lines(text);
            if (line < 0 || line >= lines.Length)
                return text;
            var m = CheckboxPrefix().Match(lines[line]);
            if (!m.Success)
                return text;
            var state = m.Groups[2];
            var toggled = state.Value == " " ? "x" : " ";
            lines[line] = lines[line][..state.Index] + toggled + lines[line][(state.Index + state.Length)..];
            return string.Join('\n', lines);
        }

        public readonly record struct Link(int Start, int Length, string Target);

        /// <summary>Links in a line of text, with trailing punctuation removed ("see www.x.com." → "www.x.com").</summary>
        public static IEnumerable<Link> FindLinks(string line)
        {
            foreach (Match m in LinkPattern().Matches(line))
            {
                var value = m.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '\'');
                if (value.Length == 0)
                    continue;
                var target = value.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "https://" + value : value;
                yield return new Link(m.Index, value.Length, target);
            }
        }

        /// <summary>The first non-empty line without checkbox markup, e.g. for reminder notifications.</summary>
        public static string Summary(string text, int maxLength = 80)
        {
            var first = Lines(text).Select(l => CheckboxPrefix().Replace(l, "").Trim()).FirstOrDefault(l => l.Length > 0) ?? "";
            return first.Length <= maxLength ? first : first[..(maxLength - 1)] + "…";
        }
    }
}
