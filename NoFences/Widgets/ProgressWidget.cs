using System.Globalization;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>How much of the day, week, month and year has passed – as bars with percent.</summary>
    public sealed class ProgressWidget : FenceWidget
    {
        public override string Type => "progress";

        public override int RefreshMs => 30_000;

        /// <summary>Share (0..1) of the day, week (Monday start), month and year that has passed at <paramref name="now"/>.</summary>
        public static (double Day, double Week, double Month, double Year) Shares(DateTime now)
        {
            var day = now.TimeOfDay.TotalSeconds / 86400;
            var weekStart = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            var week = (now - weekStart).TotalSeconds / (7 * 86400);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var month = (now - monthStart).TotalSeconds / (DateTime.DaysInMonth(now.Year, now.Month) * 86400.0);
            var yearStart = new DateTime(now.Year, 1, 1);
            var year = (now - yearStart).TotalSeconds / ((DateTime.IsLeapYear(now.Year) ? 366 : 365) * 86400.0);
            return (day, week, month, year);
        }

        public override void Draw(WidgetCanvas c)
        {
            var now = DateTime.Now;
            var (day, week, month, year) = Shares(now);
            var culture = new CultureInfo(Strings.Effective);
            float y = c.Area.Y;
            Row(Strings.ProgressDay, day);
            Row(Strings.ProgressWeek(ISOWeek.GetWeekOfYear(now)), week);
            Row(now.ToString("MMMM", culture), month);
            Row(now.Year.ToString(culture), year);

            void Row(string name, double share)
            {
                // Plain bar here: "red above 90 %" from the canvas would look like a warning at the end of every year
                var line = c.Label.GetHeight(c.G) + c.Px(2);
                c.Text(name, new RectangleF(c.Area.X, y, c.Area.Width * 0.6f, line));
                c.Text($"{share * 100:0} %", new RectangleF(c.Area.X + c.Area.Width * 0.4f, y, c.Area.Width * 0.6f, line), align: StringAlignment.Far);
                y += line + c.Px(2);
                var bar = new RectangleF(c.Area.X, y, c.Area.Width, c.Px(8));
                using (var track = new SolidBrush(Color.FromArgb(55, c.Theme.HintColor)))
                    c.G.FillRectangle(track, bar);
                using (var fill = new SolidBrush(Color.FromArgb(230, c.Theme.Accent)))
                    c.G.FillRectangle(fill, bar.X, bar.Y, (float)(bar.Width * Math.Clamp(share, 0, 1)), bar.Height);
                y += bar.Height + c.Px(10);
            }
        }
    }
}
