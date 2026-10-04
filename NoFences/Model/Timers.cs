using System.Globalization;
using System.Text.Json;

namespace NoFences.Model
{
    /// <summary>A running countdown ("10 minutes, pasta").</summary>
    public sealed class CountdownTimer
    {
        public string Label { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool Fired { get; set; }
    }

    /// <summary>An alarm at a time of day, on chosen days (none = every day), or once.</summary>
    public sealed class Alarm
    {
        public string Label { get; set; } = "";
        public string Time { get; set; } = "07:00";
        public List<DayOfWeek> Days { get; set; } = new();
        public bool Once { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime? LastFired { get; set; }

        public TimeSpan TimeOfDay => TimeSpan.TryParseExact(Time, @"hh\:mm", CultureInfo.InvariantCulture, out var t) ? t : TimeSpan.Zero;
    }

    public sealed class TimerSet
    {
        public List<CountdownTimer> Timers { get; set; } = new();
        public List<Alarm> Alarms { get; set; } = new();

        public static TimerSet Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();
            try { return JsonSerializer.Deserialize<TimerSet>(json, FenceStore.JsonOptions) ?? new(); }
            catch (JsonException) { return new(); }
        }

        public string Format() => JsonSerializer.Serialize(this, FenceStore.JsonOptions);

        /// <summary>Rings again in <paramref name="minutes"/> minutes (a short timer with the same label).</summary>
        public void Snooze(string label, int minutes, DateTime now) =>
            Timers.Add(new CountdownTimer { Label = label, Start = now, End = now.AddMinutes(minutes) });

        /// <summary>
        /// What rings now: finished timers (marked fired) and alarms whose time has come today (missed by at
        /// most 10 minutes, e.g. the PC was asleep). One-time alarms switch off after ringing.
        /// </summary>
        public List<string> Due(DateTime now)
        {
            var due = new List<string>();
            foreach (var t in Timers.Where(t => !t.Fired && t.End <= now))
            {
                t.Fired = true;
                due.Add(t.Label);
            }
            foreach (var a in Alarms.Where(a => a.Enabled))
            {
                var at = now.Date + a.TimeOfDay;
                var dayOk = a.Days.Count == 0 || a.Days.Contains(now.DayOfWeek);
                if (!dayOk || now < at || now - at > TimeSpan.FromMinutes(10) || a.LastFired?.Date == now.Date)
                    continue;
                a.LastFired = now;
                if (a.Once)
                    a.Enabled = false;
                due.Add(a.Label.Length > 0 ? a.Label : a.Time);
            }
            // Finished timers disappear a minute after ringing
            Timers.RemoveAll(t => t.Fired && now - t.End > TimeSpan.FromMinutes(1));
            return due;
        }
    }
}
