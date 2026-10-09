using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Which programs were used how long – today or in the last seven days – with icons and bars.
    /// Recorded by NoFences itself (see NoFencesApp.ScreenTime), stays on this PC.
    /// </summary>
    public sealed class ScreenTimeWidget : FenceWidget
    {
        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly Func<UsageLog> log;
        private List<(string Exe, string Name, int Seconds)> totals = new();
        private RectangleF switchRect;

        public ScreenTimeWidget(Func<string?> getOption, Action<string?> setOption, Func<UsageLog> log)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            this.log = log;
            IconCache.Shared.ImageLoaded += IconsLoaded;
        }

        public override string Type => "screentime";

        public override int RefreshMs => 10_000;

        private bool Week => getOption() == "week";

        private void IconsLoaded(object? sender, EventArgs e) => RequestRedraw();

        public override void Refresh()
        {
            var today = DateTime.Today;
            totals = log().Totals(Week ? today.AddDays(-6) : today, today);
        }

        /// <summary>"3h 05m" or "42m".</summary>
        /// <summary>"0m", "45m", "3h 07m", "128h 15m".</summary>
        public static string Format(int seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            var hours = (int)t.TotalHours;
            return hours == 0 ? $"{t.Minutes}m" : $"{hours}h {t.Minutes:00}m";
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            float y = c.Area.Y;

            // Total, big, with "today / 7 days" as a switch next to it
            using var big = c.Sized(Math.Min(c.Area.Height * 0.16f, c.Px(30)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            var total = totals.Sum(t => t.Seconds);
            c.Text(Format(total), new RectangleF(c.Area.X, y, c.Area.Width * 0.6f, bigHeight), big);
            var label = Week ? Strings.ScreenTimeWeek : Strings.PlaytimeToday;
            switchRect = new RectangleF(c.Area.X + c.Area.Width * 0.5f, y, c.Area.Width * 0.5f, bigHeight);
            c.Text($"{label} ⇄", switchRect, align: StringAlignment.Far, valign: StringAlignment.Center);
            y += bigHeight + c.Px(8);

            if (totals.Count == 0)
            {
                c.TextWrapped(Strings.ScreenTimeHint, c.Area.X, y, c.Area.Width, c.Label, 4);
                return;
            }

            var max = Math.Max(1, totals[0].Seconds);
            var iconSize = (int)Math.Round(line);
            foreach (var (exe, name, seconds) in totals)
            {
                if (y + line + c.Px(8) > c.Area.Bottom)
                    break;
                var icon = IconCache.Shared.Get(exe, 32);
                if (icon != null)
                    c.G.DrawImage(icon, c.Area.X, y, iconSize, iconSize);
                var x = c.Area.X + iconSize + c.Px(6);
                c.Text(name, new RectangleF(x, y, c.Area.Right - x - c.Px(64), line));
                c.Text(Format(seconds), new RectangleF(c.Area.Right - c.Px(70), y, c.Px(70), line), align: StringAlignment.Far);
                y += line;
                c.Bar(new RectangleF(x, y, c.Area.Right - x, c.Px(4)), (double)seconds / max * 0.9);
                y += c.Px(10);
            }
        }

        public override bool IsClickable(Point p) => switchRect.Contains(p);

        public override bool Click(Point p)
        {
            if (!switchRect.Contains(p))
                return false;
            setOption(Week ? null : "week");
            Refresh();
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(new ToolStripMenuItem(Strings.PlaytimeToday, null, (_, _) => { setOption(null); Refresh(); RequestRedraw(); }) { Checked = !Week });
            menu.Add(new ToolStripMenuItem(Strings.ScreenTimeWeek, null, (_, _) => { setOption("week"); Refresh(); RequestRedraw(); }) { Checked = Week });
        }

        public override void Dispose() => IconCache.Shared.ImageLoaded -= IconsLoaded;
    }
}
