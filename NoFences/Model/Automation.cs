using System.Globalization;

namespace NoFences.Model
{
    public enum ProfileTrigger { Program, Time }

    /// <summary>"Switch to profile X while program Y runs" or "… on these days between these times".</summary>
    public sealed class ProfileRule
    {
        public string Profile { get; set; } = "";

        public ProfileTrigger Trigger { get; set; }

        /// <summary>Program rules: the exe (full path or just the file name).</summary>
        public string? Program { get; set; }

        /// <summary>Time rules: the days it applies (a range past midnight belongs to the day it starts).</summary>
        public List<DayOfWeek> Days { get; set; } = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        public string From { get; set; } = "08:00";

        public string To { get; set; } = "17:00";

        /// <summary>Process name as Windows reports it ("eldenring" for "C:\…\eldenring.exe").</summary>
        public string ProcessName => Path.GetFileNameWithoutExtension(Program ?? "");

        public static TimeSpan ParseTime(string text) =>
            TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var t) ? t : TimeSpan.Zero;

        public bool Matches(DateTime now, ISet<string> runningProcesses)
        {
            if (Trigger == ProfileTrigger.Program)
                return ProcessName.Length > 0 && runningProcesses.Contains(ProcessName);

            var from = ParseTime(From);
            var to = ParseTime(To);
            var time = now.TimeOfDay;
            if (from <= to)
                return Days.Contains(now.DayOfWeek) && time >= from && time < to;
            // e.g. 22:00–02:00: the evening part today, or the early part of a range that started yesterday
            return (Days.Contains(now.DayOfWeek) && time >= from)
                   || (Days.Contains(now.AddDays(-1).DayOfWeek) && time < to);
        }
    }

    public static class ProfileRules
    {
        /// <summary>
        /// The profile the rules ask for right now, or null if none applies. A running program wins over
        /// a time rule (playing during work hours is gaming); otherwise the first matching rule counts.
        /// </summary>
        public static string? Match(IEnumerable<ProfileRule> rules, DateTime now, ISet<string> runningProcesses, IReadOnlyCollection<string> profiles)
        {
            var valid = rules.Where(r => profiles.Contains(r.Profile)).ToList();
            return valid.FirstOrDefault(r => r.Trigger == ProfileTrigger.Program && r.Matches(now, runningProcesses))?.Profile
                   ?? valid.FirstOrDefault(r => r.Trigger == ProfileTrigger.Time && r.Matches(now, runningProcesses))?.Profile;
        }
    }

    public enum AutoThemeMode { Off, Windows, Time }

    public static class ThemeSchedule
    {
        /// <summary>Whether the dark style applies now.</summary>
        public static bool IsDark(AutoThemeMode mode, DateTime now, bool windowsUsesLightTheme, string darkFrom, string darkTo)
        {
            switch (mode)
            {
                case AutoThemeMode.Windows:
                    return !windowsUsesLightTheme;
                case AutoThemeMode.Time:
                    var from = ProfileRule.ParseTime(darkFrom);
                    var to = ProfileRule.ParseTime(darkTo);
                    var t = now.TimeOfDay;
                    return from <= to ? t >= from && t < to : t >= from || t < to;
                default:
                    return false;
            }
        }
    }
}
