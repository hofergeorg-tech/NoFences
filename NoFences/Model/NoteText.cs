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

        public enum LineKind { Text, Heading1, Heading2, Heading3, Bullet, Quote, Rule }

        [GeneratedRegex(@"^\s*!\[([^\]]*)\]\(([^)]+)\)\s*$")]
        private static partial Regex MediaPattern();

        private static readonly string[] AudioExtensions = { ".wav", ".mp3", ".m4a", ".ogg" };

        public readonly record struct MediaLine(string Alt, string Path, bool IsAudio);

        /// <summary>A line that is only "![alt](path)": an image, or a voice note for audio files.</summary>
        public static MediaLine? Media(string line)
        {
            var m = MediaPattern().Match(line);
            if (!m.Success)
                return null;
            var path = m.Groups[2].Value.Trim();
            var isAudio = AudioExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
            return new MediaLine(m.Groups[1].Value, path, isAudio);
        }

        public static string MediaMarkup(string alt, string path) => $"![{alt.Replace("]", ")")}]({path})";

        /// <summary>"0:42", "12:05"</summary>
        public static string Duration(TimeSpan t) => $"{(int)t.TotalMinutes}:{t.Seconds:00}";

        /// <summary>Markdown-style line types: "# " headings, "- " / "* " bullets, "> " quotes, "---" rules.</summary>
        public static (LineKind Kind, string Content) ParseLine(string line)
        {
            var t = line.TrimStart();
            if (t.Length >= 3 && t.All(c => c == '-' || c == '_' || c == '*') && t.Distinct().Count() == 1)
                return (LineKind.Rule, "");
            if (t.StartsWith("### "))
                return (LineKind.Heading3, t[4..]);
            if (t.StartsWith("## "))
                return (LineKind.Heading2, t[3..]);
            if (t.StartsWith("# "))
                return (LineKind.Heading1, t[2..]);
            if (t.StartsWith("- ") || t.StartsWith("* ") || t.StartsWith("• "))
                return (LineKind.Bullet, t[2..]);
            if (t.StartsWith("> "))
                return (LineKind.Quote, t[2..]);
            return (LineKind.Text, line);
        }

        public readonly record struct Run(string Text, bool Bold, bool Italic);

        [GeneratedRegex(@"\*\*(?=\S)(.+?)(?<=\S)\*\*|(?<![\*\w])\*(?=\S)(.+?)(?<=\S)\*(?![\*\w])|(?<!\w)_(?=\S)(.+?)(?<=\S)_(?!\w)")]
        private static partial Regex InlinePattern();

        /// <summary>Splits **bold**, *italic* and _italic_ into runs; plain text stays one run.</summary>
        public static List<Run> Runs(string text)
        {
            var runs = new List<Run>();
            var pos = 0;
            foreach (Match m in InlinePattern().Matches(text))
            {
                if (m.Index > pos)
                    runs.Add(new Run(text[pos..m.Index], false, false));
                if (m.Groups[1].Success)
                    runs.Add(new Run(m.Groups[1].Value, true, false));
                else
                    runs.Add(new Run(m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value, false, true));
                pos = m.Index + m.Length;
            }
            if (pos < text.Length)
                runs.Add(new Run(text[pos..], false, false));
            return runs;
        }

        public static bool HasInlineFormatting(string text) => InlinePattern().IsMatch(text);

        /// <summary>The first non-empty line without checkbox markup, e.g. for reminder notifications.</summary>
        public static string Summary(string text, int maxLength = 80)
        {
            var first = Lines(text)
                .Where(l => Media(l) == null)
                .Select(l => string.Concat(Runs(ParseLine(CheckboxPrefix().Replace(l, "")).Content).Select(r => r.Text)).Trim())
                .FirstOrDefault(l => l.Length > 0) ?? "";
            return first.Length <= maxLength ? first : first[..(maxLength - 1)] + "…";
        }
    }
}
