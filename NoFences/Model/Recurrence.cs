using System.Text.Json;

namespace NoFences.Model
{
    public enum Repeat { None, Daily, Weekdays, Weekly, Monthly }

    public static class Recurrence
    {
        /// <summary>The next time after <paramref name="now"/> on the same rhythm as <paramref name="at"/>; null for one-off.</summary>
        public static DateTime? Next(DateTime at, Repeat repeat, DateTime now)
        {
            if (repeat == Repeat.None)
                return null;
            var next = at;
            // Skips missed occurrences (e.g. after a few days with the PC off) instead of firing them all
            for (var guard = 0; next <= now && guard < 5000; guard++)
                next = Step(next, repeat, at.Day);
            return next;
        }

        private static DateTime Step(DateTime t, Repeat repeat, int dayOfMonth)
        {
            switch (repeat)
            {
                case Repeat.Daily:
                    return t.AddDays(1);
                case Repeat.Weekdays:
                    var d = t.AddDays(1);
                    while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        d = d.AddDays(1);
                    return d;
                case Repeat.Weekly:
                    return t.AddDays(7);
                default:
                    // Monthly: keep the original day (31st → last day of shorter months)
                    var month = new DateTime(t.Year, t.Month, 1).AddMonths(1);
                    return new DateTime(month.Year, month.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(month.Year, month.Month))) + t.TimeOfDay;
            }
        }
    }

    /// <summary>One to-do: text, done, optional due time and repetition.</summary>
    public sealed class TodoItem
    {
        public string Text { get; set; } = "";
        public bool Done { get; set; }
        public DateTime? Due { get; set; }
        public Repeat Repeat { get; set; }

        /// <summary>The due notification was shown (reset when the due time moves).</summary>
        public bool Notified { get; set; }

        /// <summary>
        /// Ticking a repeating to-do moves it to its next date instead of finishing it; ticking a normal
        /// one marks it done (and unticking makes it open again).
        /// </summary>
        public void Toggle(DateTime now)
        {
            if (!Done && Repeat != Repeat.None && Due is DateTime due)
            {
                Due = Recurrence.Next(due, Repeat, now);
                Notified = false;
                return;
            }
            Done = !Done;
        }
    }

    public static class TodoList
    {
        public static List<TodoItem> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();
            try
            {
                return JsonSerializer.Deserialize<List<TodoItem>>(json, FenceStore.JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        public static string Format(List<TodoItem> items) => JsonSerializer.Serialize(items, FenceStore.JsonOptions);

        /// <summary>Open items first (by due time, undated last), done items at the end.</summary>
        public static List<TodoItem> Ordered(IEnumerable<TodoItem> items) =>
            items.OrderBy(i => i.Done).ThenBy(i => i.Due ?? DateTime.MaxValue).ToList();
    }
}
