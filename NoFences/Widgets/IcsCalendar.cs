using System.Globalization;
using System.Text;

namespace NoFences.Widgets
{
    /// <summary>One appointment (a single occurrence), in local time.</summary>
    public sealed record CalendarEvent(DateTime Start, DateTime End, string Title, bool AllDay);

    /// <summary>
    /// Reads iCalendar (.ics) feeds as published by Google, Outlook, iCloud: single and recurring events
    /// (daily/weekly/monthly/yearly with interval, count, until, weekdays), time zones, exceptions
    /// (EXDATE) and moved occurrences (RECURRENCE-ID). Enough for "what's next", not a full RFC 5545.
    /// </summary>
    public static class IcsCalendar
    {
        private sealed class Raw
        {
            public string Uid = "";
            public string Summary = "";
            public DateTime Start;
            public DateTime? End;
            public TimeZoneInfo? Zone;
            public bool Utc, AllDay, Cancelled;
            public string? RRule;
            public readonly List<DateTime> ExDates = new();
            public DateTime? RecurrenceId;
        }

        /// <summary>Occurrences that overlap [from, to), sorted by start.</summary>
        public static List<CalendarEvent> Parse(string ics, DateTime from, DateTime to)
        {
            var raws = ReadEvents(ics);
            // Moved/changed single occurrences replace the original one of the series
            var overrides = raws.Where(r => r.RecurrenceId != null).ToList();
            var result = new List<CalendarEvent>();
            foreach (var raw in raws.Where(r => r.RecurrenceId == null && !r.Cancelled))
            {
                var skip = overrides.Where(o => o.Uid == raw.Uid).Select(o => o.RecurrenceId!.Value).ToHashSet();
                foreach (var start in Occurrences(raw, from, to))
                {
                    if (skip.Contains(start) || raw.ExDates.Contains(start))
                        continue;
                    Add(result, raw, start, from, to);
                }
            }
            foreach (var o in overrides.Where(o => !o.Cancelled))
                Add(result, o, o.Start, from, to);
            return result.OrderBy(e => e.Start).ThenBy(e => e.Title).ToList();
        }

        private static void Add(List<CalendarEvent> result, Raw raw, DateTime zoneStart, DateTime from, DateTime to)
        {
            var length = raw.End is { } end ? end - raw.Start : raw.AllDay ? TimeSpan.FromDays(1) : TimeSpan.FromHours(1);
            var start = ToLocal(raw, zoneStart);
            var stop = ToLocal(raw, zoneStart + length);
            if (stop > from && start < to)
                result.Add(new CalendarEvent(start, stop, raw.Summary, raw.AllDay));
        }

        private static DateTime ToLocal(Raw raw, DateTime value)
        {
            if (raw.AllDay)
                return value;
            try
            {
                if (raw.Utc)
                    return DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
                if (raw.Zone != null)
                    return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), raw.Zone, TimeZoneInfo.Local);
            }
            catch (ArgumentException) { }
            return value; // "floating" time: as written
        }

        #region Reading

        private static List<Raw> ReadEvents(string ics)
        {
            var events = new List<Raw>();
            Raw? current = null;
            foreach (var line in Unfold(ics))
            {
                if (line == "BEGIN:VEVENT")
                {
                    current = new Raw();
                    continue;
                }
                if (line == "END:VEVENT")
                {
                    if (current != null && current.Start != default)
                        events.Add(current);
                    current = null;
                    continue;
                }
                if (current == null)
                    continue;

                var colon = line.IndexOf(':');
                if (colon < 0)
                    continue;
                var head = line[..colon];
                var value = line[(colon + 1)..];
                var parts = head.Split(';');
                var name = parts[0].ToUpperInvariant();
                var parameters = parts.Skip(1).Select(p => p.Split('=', 2)).Where(p => p.Length == 2)
                    .ToDictionary(p => p[0].ToUpperInvariant(), p => p[1].Trim('"'));

                switch (name)
                {
                    case "UID":
                        current.Uid = value;
                        break;
                    case "SUMMARY":
                        current.Summary = Unescape(value);
                        break;
                    case "STATUS":
                        current.Cancelled = value.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "RRULE":
                        current.RRule = value;
                        break;
                    case "DTSTART":
                        var (start, utc, allDay) = ParseDate(value, parameters);
                        current.Start = start;
                        current.Utc = utc;
                        current.AllDay = allDay;
                        current.Zone = Zone(parameters);
                        break;
                    case "DTEND":
                        current.End = ParseDate(value, parameters).Value;
                        break;
                    case "EXDATE":
                        foreach (var d in value.Split(','))
                            current.ExDates.Add(ParseDate(d, parameters).Value);
                        break;
                    case "RECURRENCE-ID":
                        current.RecurrenceId = ParseDate(value, parameters).Value;
                        break;
                }
            }
            return events;
        }

        /// <summary>Joins folded lines (continuations start with a space or tab).</summary>
        private static IEnumerable<string> Unfold(string ics)
        {
            var sb = new StringBuilder();
            foreach (var raw in ics.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t'))
                {
                    sb.Append(raw, 1, raw.Length - 1);
                    continue;
                }
                if (sb.Length > 0)
                    yield return sb.ToString();
                sb.Clear().Append(raw.TrimEnd('\r'));
            }
            if (sb.Length > 0)
                yield return sb.ToString();
        }

        private static string Unescape(string s) =>
            s.Replace("\\n", " ").Replace("\\N", " ").Replace("\\,", ",").Replace("\\;", ";").Replace("\\\\", "\\").Trim();

        private static (DateTime Value, bool Utc, bool AllDay) ParseDate(string value, Dictionary<string, string> parameters)
        {
            value = value.Trim();
            if (value.Length == 8 || parameters.TryGetValue("VALUE", out var type) && type == "DATE")
                return (DateTime.ParseExact(value[..8], "yyyyMMdd", CultureInfo.InvariantCulture), false, true);
            var utc = value.EndsWith('Z');
            var dt = DateTime.ParseExact(value.TrimEnd('Z')[..15], "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
            return (dt, utc, false);
        }

        private static TimeZoneInfo? Zone(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("TZID", out var id))
                return null;
            try
            {
                // Windows ("W. Europe Standard Time") and IANA ("Europe/Vienna") ids both work
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Recurrence

        private static readonly Dictionary<string, DayOfWeek> Days = new()
        {
            ["MO"] = DayOfWeek.Monday, ["TU"] = DayOfWeek.Tuesday, ["WE"] = DayOfWeek.Wednesday, ["TH"] = DayOfWeek.Thursday,
            ["FR"] = DayOfWeek.Friday, ["SA"] = DayOfWeek.Saturday, ["SU"] = DayOfWeek.Sunday
        };

        /// <summary>Start times (in the event's own time zone) of all occurrences that may touch the window.</summary>
        private static IEnumerable<DateTime> Occurrences(Raw raw, DateTime from, DateTime to)
        {
            if (raw.RRule == null)
            {
                yield return raw.Start;
                yield break;
            }

            var rule = raw.RRule.Split(';').Select(p => p.Split('=', 2)).Where(p => p.Length == 2)
                .ToDictionary(p => p[0].ToUpperInvariant(), p => p[1]);
            var freq = rule.GetValueOrDefault("FREQ", "DAILY");
            var interval = int.TryParse(rule.GetValueOrDefault("INTERVAL"), out var i) && i > 0 ? i : 1;
            int? count = int.TryParse(rule.GetValueOrDefault("COUNT"), out var c) ? c : null;
            DateTime? until = rule.TryGetValue("UNTIL", out var u) ? ParseDate(u, new()).Value : null;
            var byDay = rule.TryGetValue("BYDAY", out var bd) ? bd.Split(',') : Array.Empty<string>();
            var byMonthDay = rule.TryGetValue("BYMONTHDAY", out var bmd) ? bmd.Split(',').Select(int.Parse).ToArray() : Array.Empty<int>();

            // Look a little past the window: occurrences are in the event's zone, the window is local
            var limit = to.AddDays(2);
            var produced = 0;
            var time = raw.Start.TimeOfDay;
            // Without COUNT, old series can start counting near the window instead of at their first date
            var first = 0;
            if (count == null && from > raw.Start)
            {
                var span = from.AddDays(-2) - raw.Start;
                var months = (from.Year - raw.Start.Year) * 12 + from.Month - raw.Start.Month - 1;
                first = Math.Max(0, freq switch
                {
                    "WEEKLY" => (int)(span.TotalDays / 7) / interval - 1,
                    "MONTHLY" => months / interval - 1,
                    "YEARLY" => (from.Year - raw.Start.Year) / interval - 1,
                    _ => (int)span.TotalDays / interval
                });
            }
            for (var period = first; period < first + 5000; period++)
            {
                var candidates = freq switch
                {
                    "WEEKLY" => WeekCandidates(raw.Start, period * interval, byDay, time),
                    "MONTHLY" => MonthCandidates(raw.Start, period * interval, byDay, byMonthDay, time),
                    "YEARLY" => YearCandidate(raw.Start, period * interval),
                    _ => new[] { raw.Start.AddDays(period * interval) }
                };
                foreach (var occurrence in candidates.Where(o => o >= raw.Start).OrderBy(o => o))
                {
                    if (until != null && occurrence > until.Value.AddDays(raw.AllDay ? 1 : 0) || count != null && produced >= count)
                        yield break;
                    produced++;
                    if (occurrence > limit)
                        yield break;
                    yield return occurrence;
                }
                if (candidates.Length > 0 && candidates.Min() > limit)
                    yield break;
            }
        }

        private static DateTime[] WeekCandidates(DateTime start, int weeks, string[] byDay, TimeSpan time)
        {
            var monday = start.Date.AddDays(-(((int)start.DayOfWeek + 6) % 7)).AddDays(7 * weeks);
            if (byDay.Length == 0)
                return new[] { start.AddDays(7 * weeks) };
            return byDay.Select(d => d.Length >= 2 && Days.TryGetValue(d[^2..], out var day) ? monday.AddDays(((int)day + 6) % 7) + time : DateTime.MinValue)
                .Where(d => d != DateTime.MinValue).ToArray();
        }

        private static DateTime[] MonthCandidates(DateTime start, int months, string[] byDay, int[] byMonthDay, TimeSpan time)
        {
            var month = new DateTime(start.Year, start.Month, 1).AddMonths(months);
            var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            if (byDay.Length > 0)
            {
                // "2TU" = second Tuesday, "-1FR" = last Friday
                var list = new List<DateTime>();
                foreach (var d in byDay)
                {
                    if (d.Length < 2 || !Days.TryGetValue(d[^2..], out var day))
                        continue;
                    var all = Enumerable.Range(1, daysInMonth).Select(n => month.AddDays(n - 1)).Where(x => x.DayOfWeek == day).ToList();
                    if (!int.TryParse(d[..^2], out var nth))
                        list.AddRange(all.Select(x => x + time));
                    else if (nth > 0 && nth <= all.Count)
                        list.Add(all[nth - 1] + time);
                    else if (nth < 0 && -nth <= all.Count)
                        list.Add(all[all.Count + nth] + time);
                }
                return list.ToArray();
            }
            var days = byMonthDay.Length > 0 ? byMonthDay : new[] { start.Day };
            return days.Select(n => n < 0 ? daysInMonth + n + 1 : n)
                .Where(n => n >= 1 && n <= daysInMonth)
                .Select(n => month.AddDays(n - 1) + time)
                .ToArray();
        }

        private static DateTime[] YearCandidate(DateTime start, int years)
        {
            var year = start.Year + years;
            // 29 February only in leap years
            return start.Month == 2 && start.Day == 29 && !DateTime.IsLeapYear(year)
                ? Array.Empty<DateTime>()
                : new[] { new DateTime(year, start.Month, start.Day) + start.TimeOfDay };
        }

        #endregion
    }
}
