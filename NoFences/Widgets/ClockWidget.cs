using System.Globalization;

namespace NoFences.Widgets
{
    /// <summary>Time, date and (if there is room) the current month.</summary>
    public sealed class ClockWidget : FenceWidget
    {
        public override string Type => "clock";

        public override int RefreshMs => 1000;

        public override void Draw(WidgetCanvas c)
        {
            var now = DateTime.Now;
            var culture = CultureInfo.CurrentUICulture;
            var area = c.Area;

            var timeSize = Math.Min(area.Height * 0.32f, area.Width * 0.24f);
            using var timeFont = c.Sized(timeSize, FontStyle.Bold);
            var timeHeight = timeFont.GetHeight(c.G);
            c.Text(now.ToString("HH:mm", culture), new RectangleF(area.X, area.Y, area.Width, timeHeight), timeFont, StringAlignment.Center);

            var y = area.Y + timeHeight;
            var dateHeight = c.Label.GetHeight(c.G) + c.Px(2);
            c.Text(now.ToString("dddd, d. MMMM yyyy", culture), new RectangleF(area.X, y, area.Width, dateHeight), align: StringAlignment.Center);
            y += dateHeight + c.Px(8);

            var rows = 7; // header + up to 6 weeks
            var cellHeight = c.Label.GetHeight(c.G) + c.Px(3);
            if (area.Bottom - y >= rows * cellHeight)
                DrawMonth(c, new RectangleF(area.X, y, area.Width, rows * cellHeight), now, cellHeight);
        }

        private static void DrawMonth(WidgetCanvas c, RectangleF rect, DateTime today, float cellHeight)
        {
            var culture = CultureInfo.CurrentUICulture;
            var cellWidth = rect.Width / 7;
            // Monday first, as in most of Europe
            for (var i = 0; i < 7; i++)
            {
                var name = culture.DateTimeFormat.GetShortestDayName((DayOfWeek)((i + 1) % 7));
                c.Text(name, new RectangleF(rect.X + i * cellWidth, rect.Y, cellWidth, cellHeight), align: StringAlignment.Center);
            }

            var first = new DateTime(today.Year, today.Month, 1);
            var offset = ((int)first.DayOfWeek + 6) % 7;
            var days = DateTime.DaysInMonth(today.Year, today.Month);
            // Days with appointments (from the appointments widgets) get a small dot
            var eventDays = AgendaWidget.EventDays();
            for (var d = 1; d <= days; d++)
            {
                var index = offset + d - 1;
                var cell = new RectangleF(rect.X + index % 7 * cellWidth, rect.Y + (index / 7 + 1) * cellHeight, cellWidth, cellHeight);
                if (d == today.Day)
                {
                    var size = Math.Min(cell.Width, cell.Height);
                    using var brush = new SolidBrush(Color.FromArgb(150, c.Theme.Accent));
                    c.G.FillEllipse(brush, cell.X + (cell.Width - size) / 2, cell.Y, size, size);
                }
                c.Text(d.ToString(culture), cell, align: StringAlignment.Center);
                if (eventDays.Contains(first.AddDays(d - 1)))
                {
                    var dot = Math.Max(3, cellHeight * 0.16f);
                    using var mark = new SolidBrush(d == today.Day ? Color.White : c.Theme.Accent);
                    c.G.FillEllipse(mark, cell.X + cell.Width / 2 - dot / 2, cell.Bottom - dot - 1, dot, dot);
                }
            }
        }
    }
}
