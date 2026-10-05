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

        /// <summary>A piece of text with its look. <paramref name="Highlight"/> is a color letter (see <see cref="HighlightColors"/>) or null.</summary>
        public readonly record struct Run(string Text, bool Bold, bool Italic, bool Underline = false, bool Strike = false, char? Highlight = null);

        /// <summary>Marker colors: y(ellow), g(reen), b(lue), p(ink), o(range), r(ed); "==text==" is yellow.</summary>
        public static readonly IReadOnlyList<char> HighlightColors = new[] { 'y', 'g', 'b', 'p', 'o', 'r' };

        // **bold**, __underline__, ~~strike~~, ==marked== / =={g}marked==, *italic*, _italic_ (the earliest match wins; contents are parsed again, so they can be combined)
        [GeneratedRegex(@"\*\*(?=\S)(?<b>.+?)(?<=\S)\*\*|__(?=\S)(?<u>.+?)(?<=\S)__|~~(?=\S)(?<s>.+?)(?<=\S)~~|==(?:\{(?<c>[ygbpor])\})?(?=\S)(?<h>.+?)(?<=\S)==|(?<![\*\w])\*(?=[^\s*])(?<i>.+?)(?<=[^\s*])\*(?![\*\w])|(?<![_\w])_(?=[^\s_])(?<j>.+?)(?<=[^\s_])_(?![_\w])")]
        private static partial Regex InlinePattern();

        /// <summary>Splits the inline formatting into runs; plain text stays one run. Formats can be nested ("**==both==**").</summary>
        public static List<Run> Runs(string text) => Runs(text, new Run("", false, false), 0);

        private static List<Run> Runs(string text, Run style, int depth)
        {
            var runs = new List<Run>();
            var pos = 0;
            foreach (Match m in InlinePattern().Matches(text))
            {
                if (m.Index > pos)
                    runs.Add(style with { Text = text[pos..m.Index] });
                var inner = style;
                string content;
                if (m.Groups["b"].Success) { inner = inner with { Bold = true }; content = m.Groups["b"].Value; }
                else if (m.Groups["u"].Success) { inner = inner with { Underline = true }; content = m.Groups["u"].Value; }
                else if (m.Groups["s"].Success) { inner = inner with { Strike = true }; content = m.Groups["s"].Value; }
                else if (m.Groups["h"].Success) { inner = inner with { Highlight = m.Groups["c"].Success ? m.Groups["c"].Value[0] : 'y' }; content = m.Groups["h"].Value; }
                else { inner = inner with { Italic = true }; content = m.Groups["i"].Success ? m.Groups["i"].Value : m.Groups["j"].Value; }
                if (depth < 4 && InlinePattern().IsMatch(content))
                    runs.AddRange(Runs(content, inner, depth + 1));
                else
                    runs.Add(inner with { Text = content });
                pos = m.Index + m.Length;
            }
            if (pos < text.Length)
                runs.Add(style with { Text = text[pos..] });
            return runs;
        }

        public static bool HasInlineFormatting(string text) => InlinePattern().IsMatch(text);

        #region Priority

        [GeneratedRegex(@"^(!{1,3})\s+")]
        private static partial Regex PriorityPrefix();

        /// <summary>"!!! text" = 3 (high), "!! " = 2, "! " = 1 (low); 0 = none. Returns the line without the marker.</summary>
        public static (int Level, string Text) Priority(string line)
        {
            var m = PriorityPrefix().Match(line);
            return m.Success ? (m.Groups[1].Length, line[m.Length..]) : (0, line);
        }

        #endregion

        #region Editing (formatting bar)

        /// <summary>
        /// Puts <paramref name="marker"/> around the selection, or takes it away if the selection is already
        /// wrapped in it. Without a selection the markers are inserted and the cursor goes between them.
        /// Returns the new text and selection.
        /// </summary>
        public static (string Text, int Start, int Length) ToggleWrap(string text, int start, int length, string marker, string? closing = null)
        {
            closing ??= marker;
            start = Math.Clamp(start, 0, text.Length);
            length = Math.Clamp(length, 0, text.Length - start);
            // Keep spaces outside the markers ("**word** " instead of "**word **")
            while (length > 0 && char.IsWhiteSpace(text[start + length - 1])) length--;
            while (length > 0 && char.IsWhiteSpace(text[start])) { start++; length--; }

            var selected = text.Substring(start, length);
            // Already wrapped: markers inside the selection …
            if (selected.Length >= marker.Length + closing.Length && selected.StartsWith(marker) && selected.EndsWith(closing))
            {
                var inner = selected[marker.Length..^closing.Length];
                return (text[..start] + inner + text[(start + length)..], start, inner.Length);
            }
            // … or right around it
            if (start >= marker.Length && start + length + closing.Length <= text.Length
                && text.Substring(start - marker.Length, marker.Length) == marker && text.Substring(start + length, closing.Length) == closing)
            {
                return (text[..(start - marker.Length)] + selected + text[(start + length + closing.Length)..], start - marker.Length, length);
            }
            return (text[..start] + marker + selected + closing + text[(start + length)..], start + marker.Length, length);
        }

        /// <summary>Marks the selection in a color ('y' … 'r'); null removes any marking around it.</summary>
        public static (string Text, int Start, int Length) SetHighlight(string text, int start, int length, char? color)
        {
            var open = color is null or 'y' ? "==" : "=={" + color + "}";
            // Remove an existing marking of any color first
            foreach (var c in HighlightColors.Select(c => c == 'y' ? "==" : "=={" + c + "}").Prepend("=="))
            {
                var unwrapped = ToggleWrap(text, start, length, c, "==");
                if (unwrapped.Text.Length < text.Length)
                {
                    if (color == null)
                        return unwrapped;
                    (text, start, length) = unwrapped;
                    break;
                }
            }
            return color == null ? (text, start, length) : ToggleWrap(text, start, length, open, "==");
        }

        /// <summary>Sets the priority (0–3) of the line the cursor is in; keeps a checkbox or bullet in front.</summary>
        public static (string Text, int Caret) SetPriority(string text, int caret, int level)
        {
            caret = Math.Clamp(caret, 0, text.Length);
            var lineStart = text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
            if (caret == 0)
                lineStart = 0;
            var lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
                lineEnd = text.Length;
            var line = text[lineStart..lineEnd];

            // Where the priority goes: after "[ ] ", "- " or "* "
            var prefix = CheckboxPrefix().Match(line) is { Success: true } box ? box.Length
                : Regex.Match(line, @"^\s*[-*•]\s+") is { Success: true } bullet ? bullet.Length : 0;
            var (old, rest) = Priority(line[prefix..]);
            var marker = level > 0 ? new string('!', Math.Clamp(level, 1, 3)) + " " : "";
            var newLine = line[..prefix] + marker + rest;
            var removed = old > 0 ? line.Length - prefix - rest.Length : 0;
            var newCaret = caret - lineStart >= prefix ? Math.Max(lineStart + prefix, caret - removed + marker.Length) : caret;
            return (text[..lineStart] + newLine + text[lineEnd..], Math.Min(newCaret, lineStart + newLine.Length));
        }

        #endregion

        /// <summary>The first non-empty line without checkbox markup, e.g. for reminder notifications.</summary>
        public static string Summary(string text, int maxLength = 80)
        {
            var first = Lines(text)
                .Where(l => Media(l) == null)
                .Select(l => string.Concat(Runs(Priority(ParseLine(CheckboxPrefix().Replace(l, "")).Content).Text).Select(r => r.Text)).Trim())
                .FirstOrDefault(l => l.Length > 0) ?? "";
            return first.Length <= maxLength ? first : first[..(maxLength - 1)] + "…";
        }
    }
}
