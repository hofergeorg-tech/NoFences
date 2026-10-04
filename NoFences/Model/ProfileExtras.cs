namespace NoFences.Model
{
    /// <summary>A wallpaper that applies from a time of day on (until the next entry starts).</summary>
    public sealed class TimedWallpaper
    {
        /// <summary>"07:00"</summary>
        public string From { get; set; } = "07:00";

        public string Image { get; set; } = "";
    }

    public static class WallpaperSchedule
    {
        /// <summary>
        /// The image for <paramref name="now"/>: the entry that started last. Before the day's first entry
        /// the previous evening's (the last one) still applies. Null without entries.
        /// </summary>
        public static string? Current(IReadOnlyList<TimedWallpaper> entries, DateTime now)
        {
            if (entries.Count == 0)
                return null;
            var sorted = entries.OrderBy(e => ProfileRule.ParseTime(e.From)).ToList();
            var time = now.TimeOfDay;
            return (sorted.LastOrDefault(e => ProfileRule.ParseTime(e.From) <= time) ?? sorted[^1]).Image;
        }

        /// <summary>What the desktop should show: the profile's own wallpaper wins, then the time plan.</summary>
        public static string? Wanted(string? profileWallpaper, IReadOnlyList<TimedWallpaper> plan, DateTime now) =>
            profileWallpaper ?? Current(plan, now);
    }

    /// <summary>A program started when a profile becomes active.</summary>
    public sealed class ProfileProgram
    {
        public string Path { get; set; } = "";

        /// <summary>Close it again when switching to another profile (only if NoFences started it).</summary>
        public bool CloseOnLeave { get; set; }

        public string ProcessName => System.IO.Path.GetFileNameWithoutExtension(Path);
    }

    public static class ProfileProgramPlan
    {
        /// <summary>Programs of the new profile that aren't running yet.</summary>
        public static List<ProfileProgram> ToStart(IEnumerable<ProfileProgram> programs, ISet<string> running) =>
            programs.Where(p => p.ProcessName.Length > 0 && !running.Contains(p.ProcessName)).ToList();

        /// <summary>
        /// Programs of the old profile to close: marked "close on leave", started by NoFences, and not
        /// also wanted by the new profile.
        /// </summary>
        public static List<string> ToClose(IEnumerable<ProfileProgram> oldPrograms, IEnumerable<ProfileProgram> newPrograms, ISet<string> startedByUs)
        {
            var keep = new HashSet<string>(newPrograms.Select(p => p.ProcessName), StringComparer.OrdinalIgnoreCase);
            return oldPrograms.Where(p => p.CloseOnLeave && startedByUs.Contains(p.ProcessName) && !keep.Contains(p.ProcessName))
                .Select(p => p.ProcessName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
