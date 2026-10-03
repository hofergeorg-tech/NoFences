namespace NoFences.Widgets
{
    /// <summary>One play session as stored by sc-playtime (Unix seconds).</summary>
    public readonly record struct PlaySession(string Channel, double Start, double End);

    /// <summary>Playtime totals like sc-playtime shows them (week starts on Monday). Pure, unit-tested.</summary>
    public sealed record PlaytimeSummary(TimeSpan Today, TimeSpan Week, TimeSpan Month, TimeSpan Total, bool Live, TimeSpan LiveFor, string? LiveChannel, int Days)
    {
        /// <summary>A session whose end was updated this recently is still running (sc-playtime updates it every few seconds).</summary>
        public static readonly TimeSpan LiveTolerance = TimeSpan.FromSeconds(90);

        public static PlaytimeSummary Compute(IReadOnlyCollection<PlaySession> sessions, DateTime nowLocal)
        {
            var now = new DateTimeOffset(nowLocal).ToUnixTimeSeconds();
            var today = nowLocal.Date;
            var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var monthStart = new DateTime(today.Year, today.Month, 1);

            double Since(DateTime from)
            {
                var f = new DateTimeOffset(from).ToUnixTimeSeconds();
                return sessions.Sum(s => Math.Max(0, Math.Min(s.End, now) - Math.Max(s.Start, f)));
            }

            var latest = sessions.OrderByDescending(s => s.End).FirstOrDefault();
            var live = sessions.Count > 0 && now - latest.End <= LiveTolerance.TotalSeconds;
            var days = sessions.Select(s => DateTimeOffset.FromUnixTimeSeconds((long)s.Start).LocalDateTime.Date).Distinct().Count();

            return new PlaytimeSummary(
                TimeSpan.FromSeconds(Since(today)),
                TimeSpan.FromSeconds(Since(weekStart)),
                TimeSpan.FromSeconds(Since(monthStart)),
                TimeSpan.FromSeconds(sessions.Sum(s => Math.Max(0, s.End - s.Start))),
                live,
                live ? TimeSpan.FromSeconds(Math.Max(0, now - latest.Start)) : TimeSpan.Zero,
                live && latest.Channel.Length > 0 ? latest.Channel : null,
                days);
        }

        /// <summary>"0m", "45m", "3h 07m", "128h 15m".</summary>
        public static string Format(TimeSpan t)
        {
            var hours = (int)t.TotalHours;
            return hours == 0 ? $"{t.Minutes}m" : $"{hours}h {t.Minutes:00}m";
        }
    }
}
