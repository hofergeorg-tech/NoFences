using System.Globalization;
using System.Text.Json;

namespace NoFences.Model
{
    /// <summary>
    /// Screen time: seconds per program per day (the program in front while you are at the PC). Stored
    /// locally in usage.json; days older than five weeks are dropped.
    /// </summary>
    public sealed class UsageLog
    {
        private const int KeepDays = 35;

        /// <summary>"yyyy-MM-dd" → exe path → seconds.</summary>
        public Dictionary<string, Dictionary<string, int>> Days { get; set; } = new();

        /// <summary>Exe path → readable program name.</summary>
        public Dictionary<string, string> Names { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static string Key(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public void Add(DateTime when, string exe, string name, int seconds)
        {
            if (!Days.TryGetValue(Key(when), out var day))
                Days[Key(when)] = day = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            day[exe] = day.GetValueOrDefault(exe) + seconds;
            Names[exe] = name;
            Prune(when);
        }

        private void Prune(DateTime now)
        {
            var oldest = Key(now.Date.AddDays(-KeepDays));
            foreach (var old in Days.Keys.Where(k => string.CompareOrdinal(k, oldest) < 0).ToList())
                Days.Remove(old);
        }

        /// <summary>Seconds per program from <paramref name="from"/> through <paramref name="to"/> (dates), most used first.</summary>
        public List<(string Exe, string Name, int Seconds)> Totals(DateTime from, DateTime to)
        {
            var sums = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                if (!Days.TryGetValue(Key(d), out var day))
                    continue;
                foreach (var (exe, seconds) in day)
                    sums[exe] = sums.GetValueOrDefault(exe) + seconds;
            }
            return sums.OrderByDescending(s => s.Value)
                .Select(s => (s.Key, Names.GetValueOrDefault(s.Key) ?? Path.GetFileNameWithoutExtension(s.Key), s.Value))
                .ToList();
        }

        public static UsageLog Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var log = JsonSerializer.Deserialize<UsageLog>(File.ReadAllText(path)) ?? new UsageLog();
                    log.Names = new Dictionary<string, string>(log.Names, StringComparer.OrdinalIgnoreCase);
                    return log;
                }
            }
            catch (Exception)
            {
            }
            return new UsageLog();
        }

        public void Save(string path)
        {
            try
            {
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(this));
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception)
            {
            }
        }
    }
}
