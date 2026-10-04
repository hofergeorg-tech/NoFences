using System.Globalization;
using System.Text.Json;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    public sealed class Habit
    {
        public string Name { get; set; } = "";

        /// <summary>Days it was done, as yyyy-MM-dd.</summary>
        public List<string> Done { get; set; } = new();

        public static string Key(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public bool DoneOn(DateTime day) => Done.Contains(Key(day));

        public void Toggle(DateTime day)
        {
            if (!Done.Remove(Key(day)))
                Done.Add(Key(day));
        }

        /// <summary>Days in a row up to today (today may still be open without breaking the streak).</summary>
        public int Streak(DateTime today)
        {
            var day = DoneOn(today) ? today.Date : today.Date.AddDays(-1);
            var streak = 0;
            while (DoneOn(day))
            {
                streak++;
                day = day.AddDays(-1);
            }
            return streak;
        }
    }

    /// <summary>Daily habits (sport, water, reading …): tick the last seven days, see the current streak.</summary>
    public sealed class HabitsWidget : FenceWidget
    {
        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly List<(RectangleF Rect, int Habit, DateTime Day)> boxes = new();
        private readonly List<(RectangleF Rect, int Habit)> rows = new();
        private int hovered = -1;

        public HabitsWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "habits";

        public override int RefreshMs => 60_000;

        public static List<Habit> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();
            try { return JsonSerializer.Deserialize<List<Habit>>(json, FenceStore.JsonOptions) ?? new(); }
            catch (JsonException) { return new(); }
        }

        private List<Habit> Habits => Parse(getOption());

        private void Save(List<Habit> habits)
        {
            // Keep the files small: only the last year
            var cutoff = Habit.Key(DateTime.Today.AddDays(-366));
            foreach (var h in habits)
                h.Done.RemoveAll(d => string.CompareOrdinal(d, cutoff) < 0);
            setOption(JsonSerializer.Serialize(habits, FenceStore.JsonOptions));
        }

        public override void Draw(WidgetCanvas c)
        {
            boxes.Clear();
            rows.Clear();
            var habits = Habits;
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (habits.Count == 0)
            {
                c.TextWrapped(Strings.HabitsHint, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                return;
            }
            var today = DateTime.Today;
            var culture = new CultureInfo(Strings.Effective);
            var box = Math.Min(c.Px(20), (c.Area.Width * 0.55f) / 7 - c.Px(3));
            var boxesWidth = 7 * (box + c.Px(3));
            var nameWidth = c.Area.Width - boxesWidth - c.Px(6);
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.78f, FontStyle.Regular, c.Label.Unit);
            float y = c.Area.Y;

            // Day letters above the boxes
            for (var i = 0; i < 7; i++)
            {
                var day = today.AddDays(i - 6);
                var x = c.Area.Right - boxesWidth + i * (box + c.Px(3));
                c.Text(culture.DateTimeFormat.GetShortestDayName(day.DayOfWeek), new RectangleF(x, y, box, small.GetHeight(c.G) + 2), small, StringAlignment.Center);
            }
            y += small.GetHeight(c.G) + c.Px(4);

            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            for (var h = 0; h < habits.Count; h++)
            {
                var habit = habits[h];
                var rowHeight = Math.Max(line, box) + small.GetHeight(c.G) + c.Px(6);
                if (y + rowHeight > c.Area.Bottom + c.Px(4))
                    break;
                c.Text(habit.Name, new RectangleF(c.Area.X, y, nameWidth, line));
                var streak = habit.Streak(today);
                if (streak > 1)
                    c.Muted(Strings.HabitStreak(streak), c.Area.X, y + line, small, Color.FromArgb(230, c.Theme.Accent));
                for (var i = 0; i < 7; i++)
                {
                    var day = today.AddDays(i - 6);
                    var r = new RectangleF(c.Area.Right - boxesWidth + i * (box + c.Px(3)), y, box, box);
                    var done = habit.DoneOn(day);
                    using (var fill = new SolidBrush(done ? Color.FromArgb(220, c.Theme.Accent) : Color.FromArgb(45, c.Theme.HintColor)))
                        c.G.FillRectangle(fill, r);
                    if (day == today)
                        using (var pen = new Pen(c.Ink, Math.Max(1, c.S)))
                            c.G.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                    boxes.Add((r, h, day));
                }
                rows.Add((new RectangleF(c.Area.X, y, c.Area.Width, rowHeight), h));
                y += rowHeight;
            }
        }

        public override bool IsClickable(Point p)
        {
            hovered = rows.FirstOrDefault(r => r.Rect.Contains(p)) is { Rect.Width: > 0 } row ? row.Habit : -1;
            return boxes.Any(b => b.Rect.Contains(p));
        }

        public override bool Click(Point p)
        {
            var hit = boxes.FirstOrDefault(b => b.Rect.Contains(p));
            if (hit.Rect.Width <= 0)
                return false;
            var habits = Habits;
            if (hit.Habit >= habits.Count)
                return false;
            habits[hit.Habit].Toggle(hit.Day);
            Save(habits);
            return true;
        }

        public override void DoubleClick(Point p)
        {
            if (!boxes.Any(b => b.Rect.Contains(p)))
                Add(null);
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.HabitAdd, null, (_, _) => Add(owner));
            if (hovered >= 0 && hovered < Habits.Count)
            {
                var index = hovered;
                menu.Add(Strings.HabitDelete(Habits[index].Name), null, (_, _) =>
                {
                    var habits = Habits;
                    if (index < habits.Count)
                        habits.RemoveAt(index);
                    Save(habits);
                });
            }
        }

        private void Add(IWin32Window? owner)
        {
            using var dialog = new InputDialog(Strings.WidgetHabits, Strings.HabitPrompt, "");
            if (dialog.ShowDialog(owner) != DialogResult.OK || dialog.Value.Trim().Length == 0)
                return;
            var habits = Habits;
            habits.Add(new Habit { Name = dialog.Value.Trim() });
            Save(habits);
            RequestRedraw();
        }
    }
}
