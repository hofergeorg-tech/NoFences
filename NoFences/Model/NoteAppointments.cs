using System.Globalization;
using System.Text.RegularExpressions;

namespace NoFences.Model
{
    /// <summary>
    /// Finds appointments in note lines – "Mo 14:00 Zahnarzt", "morgen 9:30 Meeting", "12.10. 15:00 Friseur",
    /// "2026-10-20 8:15 Flug" or "15:00 Call" at the start of a line – so the note can offer a reminder.
    /// A time is needed; the day is a weekday (next one), today/tomorrow, a date, or none (today, or
    /// tomorrow when the time has passed). Weekday names and words come from the note language and English.
    /// </summary>
    public static class NoteAppointments
    {
        public sealed record Appointment(DateTime When, string Title, string Line);

        private static readonly Regex Time = new(
            @"(?<![\d:.])(?:(?<h12>1[0-2]|0?[1-9])(?::(?<m>[0-5]\d))?\s*(?<ampm>[ap])\.?m\.?(?!\w)|(?<h>[01]?\d|2[0-3]):(?<m>[0-5]\d)(?:\s*(?:Uhr|h)\b)?|(?<h>[01]?\d|2[0-3])\s*(?:Uhr\b|h(?<m>[0-5]\d)?\b))",
            RegexOptions.IgnoreCase);

        private static readonly Regex DottedDate = new(@"(?<![\d.])(?<d>[0-3]?\d)\.(?<mo>[01]?\d)\.(?<y>\d{4}|\d{2})?(?![\d:])");
        private static readonly Regex IsoDate = new(@"(?<!\d)(?<y>\d{4})-(?<mo>[01]\d)-(?<d>[0-3]\d)(?!\d)");
        private static readonly Regex SlashDate = new(@"(?<![\d/])(?<d>[0-3]?\d)/(?<mo>[01]?\d)(?:/(?<y>\d{4}|\d{2}))?(?![\d/])");

        private static readonly Dictionary<string, (string[] Today, string[] Tomorrow, string[] AfterTomorrow)> Words = new()
        {
            ["en"] = (new[] { "today", "tonight" }, new[] { "tomorrow" }, Array.Empty<string>()),
            ["de"] = (new[] { "heute" }, new[] { "morgen" }, new[] { "übermorgen" }),
            ["it"] = (new[] { "oggi", "stasera" }, new[] { "domani" }, new[] { "dopodomani" }),
            ["fr"] = (new[] { "aujourd'hui", "ce soir" }, new[] { "demain" }, new[] { "après-demain" }),
            ["es"] = (new[] { "hoy", "esta noche" }, new[] { "mañana" }, new[] { "pasado mañana" })
        };

        public static List<Appointment> Find(string text, DateTime now, string language)
        {
            var result = new List<Appointment>();
            foreach (var raw in text.Split('\n'))
            {
                var line = Regex.Replace(raw, @"^\s*(?:\[[ xX]\]|[-*•])\s*", "").Trim();
                if (line.Length == 0 || raw.TrimStart().StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
                    continue; // done items are no appointments
                if (Parse(line, now, language) is { } appointment)
                    result.Add(appointment with { Line = raw });
            }
            return result;
        }

        public static Appointment? Parse(string line, DateTime now, string language)
        {
            var time = Time.Match(line);
            if (!time.Success)
                return null;
            var hour = time.Groups["h"].Success ? int.Parse(time.Groups["h"].Value) : int.Parse(time.Groups["h12"].Value) % 12 + (time.Groups["ampm"].Value.ToLowerInvariant() == "p" ? 12 : 0);
            var minute = time.Groups["m"].Success ? int.Parse(time.Groups["m"].Value) : 0;
            var timeOfDay = new TimeSpan(hour, minute, 0);
            var rest = line.Remove(time.Index, time.Length);

            DateTime? day = null;
            (day, rest) = FindDate(rest, now);
            if (day == null)
                (day, rest) = FindRelative(rest, now, language);
            if (day == null)
                (day, rest) = FindWeekday(rest, now, timeOfDay, language);
            if (day == null)
            {
                // Only a time: counts when the line starts with it ("15:00 Call")
                if (time.Index > 0)
                    return null;
                day = now.Date + timeOfDay > now ? now.Date : now.Date.AddDays(1);
            }

            var when = day.Value.Date + timeOfDay;
            if (when <= now)
                return null;
            var title = Regex.Replace(rest, @"\s+", " ").Trim(' ', ',', ';', ':', '-', '–', '·', '.', '/');
            return new Appointment(when, title, line);
        }

        private static (DateTime?, string) FindDate(string text, DateTime now)
        {
            foreach (var regex in new[] { IsoDate, DottedDate, SlashDate })
            {
                var m = regex.Match(text);
                if (!m.Success)
                    continue;
                var dayOfMonth = int.Parse(m.Groups["d"].Value);
                var month = int.Parse(m.Groups["mo"].Value);
                var year = m.Groups["y"].Success ? int.Parse(m.Groups["y"].Value) : now.Year;
                if (year < 100)
                    year += 2000;
                if (month is < 1 or > 12 || dayOfMonth < 1 || dayOfMonth > DateTime.DaysInMonth(year, month))
                    continue;
                var date = new DateTime(year, month, dayOfMonth);
                // "12.1." in December means next January
                if (!m.Groups["y"].Success && date < now.Date)
                    date = date.AddYears(1);
                return (date, text.Remove(m.Index, m.Length));
            }
            return (null, text);
        }

        private static (DateTime?, string) FindRelative(string text, DateTime now, string language)
        {
            foreach (var code in new[] { language, "en" }.Distinct())
            {
                if (!Words.TryGetValue(code, out var words))
                    continue;
                // Longest first: "übermorgen" before "morgen", "pasado mañana" before "mañana"
                var candidates = words.AfterTomorrow.Select(w => (w, 2)).Concat(words.Tomorrow.Select(w => (w, 1))).Concat(words.Today.Select(w => (w, 0)));
                foreach (var (word, days) in candidates)
                {
                    var m = Regex.Match(text, $@"(?<!\w){Regex.Escape(word)}(?!\w)", RegexOptions.IgnoreCase);
                    if (m.Success)
                        return (now.Date.AddDays(days), text.Remove(m.Index, m.Length));
                }
            }
            return (null, text);
        }

        private static (DateTime?, string) FindWeekday(string text, DateTime now, TimeSpan timeOfDay, string language)
        {
            foreach (var code in new[] { language, "en" }.Distinct())
            {
                DateTimeFormatInfo format;
                try
                {
                    format = new CultureInfo(code).DateTimeFormat;
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }
                // Full names first, then abbreviations (with or without the dot), never single letters
                var names = Enumerable.Range(0, 7).SelectMany(d => new[] { (format.DayNames[d], d), (format.AbbreviatedDayNames[d], d), (format.ShortestDayNames[d], d) })
                    .Select(n => (Name: n.Item1.TrimEnd('.'), Day: n.d))
                    .Where(n => n.Name.Length >= 2)
                    .OrderByDescending(n => n.Name.Length);
                foreach (var (name, dayIndex) in names)
                {
                    var m = Regex.Match(text, $@"(?<!\w){Regex.Escape(name)}\.?(?!\w)", RegexOptions.IgnoreCase);
                    if (!m.Success)
                        continue;
                    var ahead = ((dayIndex - (int)now.DayOfWeek) + 7) % 7;
                    if (ahead == 0 && now.Date + timeOfDay <= now)
                        ahead = 7;
                    return (now.Date.AddDays(ahead), text.Remove(m.Index, m.Length));
                }
            }
            return (null, text);
        }
    }
}
