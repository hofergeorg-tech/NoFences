using System.Globalization;
using System.Text.RegularExpressions;

namespace NoFences.Model
{
    /// <summary>How finished checklist items are shown in a note.</summary>
    public enum NoteDoneMode { Normal, Bottom, Hidden }

    /// <summary>An earlier state of a note (taken whenever an edit changed it).</summary>
    public sealed class NoteVersion
    {
        public DateTime Time { get; set; }

        /// <summary>The text, or for password-protected notes the encrypted text.</summary>
        public string Text { get; set; } = "";

        public bool Encrypted { get; set; }
    }

    /// <summary>A note template: a name and the text a new note starts with.</summary>
    public sealed class NoteTemplate
    {
        public string Name { get; set; } = "";
        public string Text { get; set; } = "";
    }

    /// <summary>
    /// Checklists, sub-items, counters, calculations, tags and tables in notes. Pure functions, unit-tested.
    /// </summary>
    public static partial class NoteLists
    {
        #region Indentation and sub-items

        /// <summary>Indentation level of a line: a tab or two spaces per level.</summary>
        public static int Indent(string line)
        {
            var level = 0;
            var spaces = 0;
            foreach (var c in line)
            {
                if (c == '\t') { level++; spaces = 0; }
                else if (c == ' ') { if (++spaces == 2) { level++; spaces = 0; } }
                else break;
            }
            return level;
        }

        /// <summary>Whether the next non-empty line is indented deeper (this line has sub-items).</summary>
        public static bool HasChildren(IReadOnlyList<string> lines, int index)
        {
            var own = Indent(lines[index]);
            for (var i = index + 1; i < lines.Count; i++)
            {
                if (lines[i].Trim().Length == 0)
                    continue;
                return Indent(lines[i]) > own;
            }
            return false;
        }

        /// <summary>The line and the following deeper-indented lines (its sub-items): end index (exclusive).</summary>
        public static int BlockEnd(IReadOnlyList<string> lines, int index)
        {
            var own = Indent(lines[index]);
            var end = index + 1;
            while (end < lines.Count && (lines[end].Trim().Length == 0 ? NextIsDeeper(lines, end, own) : Indent(lines[end]) > own))
                end++;
            return end;
        }

        private static bool NextIsDeeper(IReadOnlyList<string> lines, int from, int own)
        {
            for (var i = from + 1; i < lines.Count; i++)
                if (lines[i].Trim().Length > 0)
                    return Indent(lines[i]) > own;
            return false;
        }

        /// <summary>Key of a line for remembering that it is folded (its text without indentation and check state).</summary>
        public static string FoldKey(string line) => NoteText.CheckboxPrefix().Replace(line.Trim(), "").Trim();

        public static bool IsDone(string line) => NoteText.CheckboxPrefix().Match(line) is { Success: true } m && m.Groups[2].Value != " ";

        public static bool IsCheckbox(string line) => NoteText.CheckboxPrefix().IsMatch(line);

        /// <summary>
        /// Which lines to show, in which order: sub-items of folded lines are left out; finished items
        /// (with their sub-items) go to the end or are hidden.
        /// </summary>
        public static List<int> DisplayOrder(IReadOnlyList<string> lines, NoteDoneMode mode, IReadOnlyCollection<string>? folded = null)
        {
            var open = new List<int>();
            var done = new List<int>();
            var i = 0;
            while (i < lines.Count)
            {
                var end = BlockEnd(lines, i);
                var isDone = IsDone(lines[i]);
                if (isDone && mode == NoteDoneMode.Hidden)
                {
                    i = end;
                    continue;
                }
                var target = isDone && mode == NoteDoneMode.Bottom && Indent(lines[i]) == 0 ? done : open;
                var foldedHere = folded != null && end > i + 1 && folded.Contains(FoldKey(lines[i]));
                target.Add(i);
                if (foldedHere)
                {
                    i = end;
                    continue;
                }
                i++;
                // Sub-items of a finished top-level item move with it
                if (target == done)
                {
                    for (; i < end; i++)
                        if (!(mode == NoteDoneMode.Hidden && IsDone(lines[i])))
                            done.Add(i);
                }
            }
            open.AddRange(done);
            return open;
        }

        #endregion

        #region Progress and resetting

        /// <summary>Ticked and all checkboxes of the note.</summary>
        public static (int Done, int Total) Progress(string text)
        {
            int done = 0, total = 0;
            foreach (var line in NoteText.Lines(text))
            {
                if (!IsCheckbox(line))
                    continue;
                total++;
                if (IsDone(line))
                    done++;
            }
            return (done, total);
        }

        /// <summary>All checkboxes back to empty and all counters back to 0 (recurring checklists).</summary>
        public static string Reset(string text)
        {
            var lines = NoteText.Lines(text);
            for (var i = 0; i < lines.Length; i++)
            {
                var m = NoteText.CheckboxPrefix().Match(lines[i]);
                if (m.Success && m.Groups[2].Value != " ")
                    lines[i] = lines[i][..m.Groups[2].Index] + " " + lines[i][(m.Groups[2].Index + 1)..];
                lines[i] = CounterPattern().Replace(lines[i], c => $"[0/{c.Groups[2].Value}]");
            }
            return string.Join('\n', lines);
        }

        /// <summary>Whether a recurring checklist is due to be reset (a new day, week or month since the last reset).</summary>
        public static bool ResetDue(DateTime? last, Repeat repeat, DateTime now)
        {
            if (repeat == Repeat.None)
                return false;
            if (last == null)
                return true;
            var l = last.Value;
            return repeat switch
            {
                Repeat.Weekly => StartOfWeek(now) > StartOfWeek(l),
                Repeat.Monthly => now.Year != l.Year || now.Month != l.Month,
                _ => now.Date > l.Date // daily, weekdays
            };
        }

        private static DateTime StartOfWeek(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

        #endregion

        #region Counters

        [GeneratedRegex(@"\[(\d{1,4})/(\d{1,4})\]")]
        public static partial Regex CounterPattern();

        public static bool HasCounter(string line) => CounterPattern().IsMatch(line);

        /// <summary>Counts the <paramref name="occurrence"/>-th counter on the line up or down (between 0 and its goal).</summary>
        public static string StepCounter(string text, int line, int occurrence, int delta)
        {
            var lines = NoteText.Lines(text);
            if (line < 0 || line >= lines.Length)
                return text;
            var n = 0;
            lines[line] = CounterPattern().Replace(lines[line], m =>
            {
                if (n++ != occurrence)
                    return m.Value;
                var goal = int.Parse(m.Groups[2].Value);
                var value = Math.Clamp(int.Parse(m.Groups[1].Value) + delta, 0, Math.Max(goal, 0));
                return $"[{value}/{goal}]";
            });
            return string.Join('\n', lines);
        }

        #endregion

        #region Calculations

        [GeneratedRegex(@"(?<=\d)\s*[xX×·]\s*(?=\d)")]
        private static partial Regex TimesPattern();

        /// <summary>
        /// A line ending in "=" whose numbers and operators make a calculation ("Miete 650 + Strom 80 =")
        /// gives its result; words and currency signs are ignored.
        /// </summary>
        public static bool TryCalculate(string line, out string result)
        {
            result = "";
            var t = line.TrimEnd();
            if (!t.EndsWith('=') || t.EndsWith("=="))
                return false;
            var expression = TimesPattern().Replace(t[..^1], "*");
            // Thousands separators ("1.250,50" or "1,250.50") are rare in notes; a comma is a decimal comma
            var kept = new string(expression.Where(c => char.IsDigit(c) || "+-*/÷:^%().,".Contains(c)).ToArray()).Trim('+', '*', '/', ':');
            if (!Calculator.TryEvaluate(kept, out var value))
                return false;
            result = Calculator.Format(value);
            return true;
        }

        #endregion

        #region Tags

        [GeneratedRegex(@"(?<![\w#&])#(\p{L}[\p{L}\p{N}_-]*)")]
        public static partial Regex TagPattern();

        public static bool HasTag(string line) => TagPattern().IsMatch(line);

        public static IEnumerable<string> Tags(string text) =>
            TagPattern().Matches(text).Select(m => "#" + m.Groups[1].Value).Distinct(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>A stable color per tag (the same tag always looks the same).</summary>
        public static int TagHue(string tag)
        {
            var hash = 17;
            foreach (var c in tag.ToLowerInvariant())
                hash = hash * 31 + c;
            return Math.Abs(hash % 360);
        }

        #endregion

        #region Tables

        public static bool IsTableLine(string line)
        {
            var t = line.Trim();
            return t.Length >= 2 && t.StartsWith('|');
        }

        /// <summary>The cells of a table line ("| a | b |" → a, b); null for the separator line "|---|---|".</summary>
        public static List<string>? Cells(string line)
        {
            var t = line.Trim();
            if (t.StartsWith('|')) t = t[1..];
            if (t.EndsWith('|')) t = t[..^1];
            var cells = t.Split('|').Select(c => c.Trim()).ToList();
            return cells.All(c => Regex.IsMatch(c, @"^:?-{2,}:?$")) ? null : cells;
        }

        #endregion

        /// <summary>The note as plain Markdown (for export): the same text; NoFences already writes Markdown.</summary>
        public static string ToMarkdown(string name, string text) =>
            (name.Trim().Length > 0 && !text.TrimStart().StartsWith("# ") ? $"# {name.Trim()}\n\n" : "") + text.TrimEnd() + "\n";

        /// <summary>Keeps the newest versions; equal neighbours are stored once.</summary>
        public static void AddVersion(List<NoteVersion> versions, NoteVersion version, int max = 20)
        {
            if (versions.Count > 0 && versions[^1].Text == version.Text)
                return;
            versions.Add(version);
            while (versions.Count > max)
                versions.RemoveAt(0);
        }

        internal static CultureInfo Culture => new(Util.Strings.Effective);
    }
}
