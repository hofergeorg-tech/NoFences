using System.Drawing.Drawing2D;
using System.Globalization;
using NoFences.Util;

namespace NoFences.Widgets
{
    public enum ClockFace { Digital, Analog, Binary }

    /// <summary>The clock widget's settings, stored as "face=analog;weeks=1".</summary>
    public sealed record ClockOptions(ClockFace Face = ClockFace.Digital, bool Weeks = true)
    {
        public static ClockOptions Parse(string? option)
        {
            var result = new ClockOptions();
            foreach (var part in (option ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (pair.Length != 2)
                    continue;
                if (pair[0] == "face" && Enum.TryParse<ClockFace>(pair[1], true, out var face) && Enum.IsDefined(face))
                    result = result with { Face = face };
                else if (pair[0] == "weeks")
                    result = result with { Weeks = pair[1] != "0" };
            }
            return result;
        }

        public string Format() => $"face={Face.ToString().ToLowerInvariant()};weeks={(Weeks ? 1 : 0)}";
    }

    /// <summary>
    /// Time (digital, analog or binary), date and as many months as fit (up to a year), with week numbers;
    /// the mouse wheel or the arrows page through the months.
    /// </summary>
    public sealed class ClockWidget : FenceWidget
    {
        /// <summary>After this long without paging, the calendar goes back to the current month.</summary>
        private static readonly TimeSpan BackToToday = TimeSpan.FromMinutes(5);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;

        // Months away from the current one (not saved: a restart shows today again)
        private int shift;
        private DateTime lastPaging;
        private int shownMonths;
        private RectangleF prevButton, nextButton, clockRect;
        private readonly List<RectangleF> titles = new();

        public ClockWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "clock";

        public override int RefreshMs => 1000;

        private ClockOptions Options => ClockOptions.Parse(getOption());

        /// <summary>
        /// How many months fit side by side and below each other, and how many are shown (at most 12, as a
        /// balanced grid: a year becomes 4 × 3 rather than 5 + 5 + 2).
        /// </summary>
        public static (int Columns, int Rows, int Count) MonthGrid(float width, float height, float monthWidth, float monthHeight, float gapX, float gapY)
        {
            var columns = Math.Max(1, (int)((width + gapX) / (monthWidth + gapX)));
            var rows = (int)((height + gapY) / (monthHeight + gapY));
            if (rows <= 0)
                return (columns, 0, 0);
            if (columns * rows >= 12)
            {
                columns = Math.Min(columns, (int)Math.Ceiling(12.0 / rows));
                rows = Math.Min(rows, (int)Math.Ceiling(12.0 / columns));
            }
            return (columns, rows, Math.Min(12, columns * rows));
        }

        /// <summary>First month shown: the current one (moved by <paramref name="shift"/>), or January when a whole year fits.</summary>
        public static DateTime FirstShown(DateTime today, int shift, int count)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(shift);
            return count >= 12 ? new DateTime(month.Year, 1, 1) : month;
        }

        /// <summary>Months one wheel notch or arrow click moves: a year in the year view or with Ctrl/Shift.</summary>
        private int Step => shownMonths >= 12 || (Control.ModifierKeys & (Keys.Control | Keys.Shift)) != 0 ? 12 : 1;

        public override void Refresh()
        {
            if (shift != 0 && DateTime.UtcNow - lastPaging > BackToToday)
            {
                shift = 0;
                RequestRedraw();
            }
        }

        public override void Draw(WidgetCanvas c)
        {
            var options = Options;
            var now = DateTime.Now;
            var culture = CultureInfo.CurrentUICulture;
            var area = c.Area;
            float y = area.Y;

            switch (options.Face)
            {
                case ClockFace.Analog:
                {
                    var size = Math.Min(Math.Min(area.Height * 0.4f, area.Width * 0.8f), c.Px(130));
                    clockRect = new RectangleF(area.X + (area.Width - size) / 2, y, size, size);
                    DrawAnalog(c, clockRect, now);
                    y += size + c.Px(6);
                    break;
                }
                case ClockFace.Binary:
                {
                    var dot = Math.Min(Math.Min(area.Width / 9f, area.Height * 0.32f / 4.2f), c.Px(17));
                    clockRect = DrawBinary(c, area.X + area.Width / 2f, y, dot, now);
                    y = clockRect.Bottom + c.Px(8);
                    break;
                }
                default:
                {
                    var timeSize = Math.Min(Math.Min(area.Height * 0.32f, area.Width * 0.24f), c.Px(72));
                    using var timeFont = c.Sized(timeSize, FontStyle.Bold);
                    var timeHeight = timeFont.GetHeight(c.G);
                    clockRect = RectangleF.Empty;
                    c.Text(now.ToString("HH:mm", culture), new RectangleF(area.X, y, area.Width, timeHeight), timeFont, StringAlignment.Center);
                    y += timeHeight;
                    break;
                }
            }

            var dateHeight = c.Label.GetHeight(c.G) + c.Px(2);
            c.Text(now.ToString("dddd, d. MMMM yyyy", culture), new RectangleF(area.X, y, area.Width, dateHeight), align: StringAlignment.Center);
            y += dateHeight + c.Px(8);

            DrawCalendar(c, new RectangleF(area.X, y, area.Width, area.Bottom - y), now.Date, options.Weeks);
        }

        private void DrawCalendar(WidgetCanvas c, RectangleF rect, DateTime today, bool weeks)
        {
            prevButton = nextButton = RectangleF.Empty;
            titles.Clear();
            var cellHeight = c.Label.GetHeight(c.G) + c.Px(3);
            // Title + day names + up to 6 weeks
            var monthHeight = 8 * cellHeight;
            var columnsPerMonth = 7 + (weeks ? 0.85f : 0);
            float gapX = c.Px(16), gapY = c.Px(10);
            var (columns, rows, count) = MonthGrid(rect.Width, rect.Height, cellHeight * 1.3f * columnsPerMonth, monthHeight, gapX, gapY);
            shownMonths = count;
            if (count == 0)
                return;

            var monthWidth = (rect.Width - (columns - 1) * gapX) / columns;
            var first = FirstShown(today, shift, count);
            var eventDays = AgendaWidget.EventDays();
            for (var i = 0; i < count; i++)
            {
                var box = new RectangleF(rect.X + i % columns * (monthWidth + gapX), rect.Y + i / columns * (monthHeight + gapY), monthWidth, monthHeight);
                DrawMonth(c, box, first.AddMonths(i), today, cellHeight, weeks, eventDays);
            }

            // Paging arrows at the outer ends of the first row of titles
            var arrow = cellHeight;
            var lastInRow = Math.Min(columns, count) - 1;
            prevButton = new RectangleF(rect.X, rect.Y, arrow, arrow);
            nextButton = new RectangleF(rect.X + lastInRow * (monthWidth + gapX) + monthWidth - arrow, rect.Y, arrow, arrow);
            DrawChevron(c, prevButton, left: true);
            DrawChevron(c, nextButton, left: false);
        }

        private void DrawMonth(WidgetCanvas c, RectangleF box, DateTime month, DateTime today, float cellHeight, bool weeks, HashSet<DateTime> eventDays)
        {
            var culture = CultureInfo.CurrentUICulture;
            var weekWidth = weeks ? box.Width / (7 + 0.85f) * 0.85f : 0;
            var cellWidth = (box.Width - weekWidth) / 7;
            var x0 = box.X + weekWidth;

            var title = new RectangleF(box.X + cellHeight, box.Y, box.Width - 2 * cellHeight, cellHeight);
            titles.Add(title);
            using (var bold = new Font(c.Label, FontStyle.Bold))
                c.Text(month.ToString("MMMM yyyy", culture), title, bold, StringAlignment.Center);

            var y = box.Y + cellHeight;
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            if (weeks)
                Dim(c, Strings.ClockWeekHeader, new RectangleF(box.X, y, weekWidth, cellHeight), small);
            // Monday first, as in most of Europe
            for (var i = 0; i < 7; i++)
            {
                var name = culture.DateTimeFormat.GetShortestDayName((DayOfWeek)((i + 1) % 7));
                c.Text(name, new RectangleF(x0 + i * cellWidth, y, cellWidth, cellHeight), align: StringAlignment.Center);
            }
            y += cellHeight;

            var offset = ((int)month.DayOfWeek + 6) % 7;
            var days = DateTime.DaysInMonth(month.Year, month.Month);
            if (weeks)
                for (var row = 0; row * 7 < offset + days; row++)
                {
                    var monday = month.AddDays(row * 7 - offset);
                    Dim(c, ISOWeek.GetWeekOfYear(monday).ToString(culture), new RectangleF(box.X, y + row * cellHeight, weekWidth, cellHeight), small);
                }

            for (var d = 1; d <= days; d++)
            {
                var index = offset + d - 1;
                var cell = new RectangleF(x0 + index % 7 * cellWidth, y + index / 7 * cellHeight, cellWidth, cellHeight);
                var date = month.AddDays(d - 1);
                var isToday = date == today;
                if (isToday)
                {
                    var size = Math.Min(cell.Width, cell.Height);
                    using var brush = new SolidBrush(Color.FromArgb(150, c.Theme.Accent));
                    c.G.FillEllipse(brush, cell.X + (cell.Width - size) / 2, cell.Y, size, size);
                }
                c.Text(d.ToString(culture), cell, align: StringAlignment.Center);
                // Days with appointments (from the appointments widgets) get a small dot
                if (eventDays.Contains(date))
                {
                    var dot = Math.Max(3, cellHeight * 0.16f);
                    using var mark = new SolidBrush(isToday ? Color.White : c.Theme.Accent);
                    c.G.FillEllipse(mark, cell.X + cell.Width / 2 - dot / 2, cell.Bottom - dot - 1, dot, dot);
                }
            }
        }

        /// <summary>Dimmed, centered text (week numbers), without the style's effects.</summary>
        private static void Dim(WidgetCanvas c, string text, RectangleF rect, Font font)
        {
            using var brush = new SolidBrush(Color.FromArgb(Math.Max(150, (int)c.Theme.HintColor.A), c.Theme.HintColor));
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            var oldHint = c.G.TextRenderingHint;
            c.G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            c.G.DrawString(text, font, brush, rect, format);
            c.G.TextRenderingHint = oldHint;
        }

        private static void DrawChevron(WidgetCanvas c, RectangleF r, bool left)
        {
            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(c.Ink, Math.Max(1.5f, c.Px(1.8f))) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, h = r.Height * 0.22f, w = h * 0.6f;
            var tip = left ? cx - w : cx + w;
            var back = left ? cx + w : cx - w;
            c.G.DrawLines(pen, new[] { new PointF(back, cy - h), new PointF(tip, cy), new PointF(back, cy + h) });
            c.G.SmoothingMode = oldMode;
        }

        private static void DrawAnalog(WidgetCanvas c, RectangleF r, DateTime now)
        {
            var g = c.G;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, radius = r.Width / 2 - c.Px(2);

            using (var face = new SolidBrush(Color.FromArgb(28, c.Theme.HintColor)))
                g.FillEllipse(face, cx - radius, cy - radius, 2 * radius, 2 * radius);
            using (var rim = new Pen(Color.FromArgb(90, c.Theme.HintColor), Math.Max(1, radius * 0.025f)))
                g.DrawEllipse(rim, cx - radius, cy - radius, 2 * radius, 2 * radius);

            PointF At(double degrees, float length)
            {
                var a = degrees * Math.PI / 180;
                return new PointF(cx + (float)Math.Sin(a) * length, cy - (float)Math.Cos(a) * length);
            }

            // Hour marks (12, 3, 6, 9 stronger) and, on bigger faces, minute marks
            using (var hourPen = new Pen(c.Ink, Math.Max(1.5f, radius * 0.045f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var minutePen = new Pen(Color.FromArgb(110, c.Theme.HintColor), Math.Max(1, radius * 0.018f)))
                for (var i = 0; i < 60; i++)
                {
                    if (i % 5 == 0)
                        g.DrawLine(hourPen, At(i * 6, radius * (i % 15 == 0 ? 0.76f : 0.82f)), At(i * 6, radius * 0.92f));
                    else if (radius > c.Px(40))
                        g.DrawLine(minutePen, At(i * 6, radius * 0.87f), At(i * 6, radius * 0.92f));
                }

            void Hand(double degrees, float length, float width, Color color)
            {
                using var pen = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, At(degrees + 180, radius * 0.12f), At(degrees, length));
            }

            var t = now.TimeOfDay;
            Hand((t.TotalHours % 12) * 30, radius * 0.5f, Math.Max(2.5f, radius * 0.075f), c.Ink);
            Hand(t.TotalMinutes % 60 * 6, radius * 0.74f, Math.Max(2, radius * 0.05f), c.Ink);
            Hand(now.Second * 6, radius * 0.82f, Math.Max(1, radius * 0.022f), c.Theme.Accent);
            var hub = Math.Max(4, radius * 0.09f);
            using (var accent = new SolidBrush(c.Theme.Accent))
                g.FillEllipse(accent, cx - hub / 2, cy - hub / 2, hub, hub);
            g.SmoothingMode = oldMode;
        }

        /// <summary>Lit bits per digit of HH:MM:SS (each digit on its own, as binary clocks show it).</summary>
        public static readonly int[] BinaryBits = { 2, 4, 3, 4, 3, 4 };

        /// <summary>Six columns of dots (the digits of HH MM SS, lowest bit at the bottom), centered on <paramref name="centerX"/>.</summary>
        private static RectangleF DrawBinary(WidgetCanvas c, float centerX, float top, float dot, DateTime now)
        {
            var g = c.G;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var gap = dot * 0.4f;
            var pairGap = dot * 0.7f;
            var width = 6 * dot + 5 * gap + 2 * pairGap;
            var left = centerX - width / 2;
            int[] digits = { now.Hour / 10, now.Hour % 10, now.Minute / 10, now.Minute % 10, now.Second / 10, now.Second % 10 };
            using var on = new SolidBrush(Color.FromArgb(235, c.Theme.Accent));
            using var off = new SolidBrush(Color.FromArgb(45, c.Theme.HintColor));
            // Reading aid: the value of each row (lit dots in a column add up to the digit)
            using (var small = new Font(c.Label.FontFamily, Math.Max(6, Math.Min(c.Label.Size * 0.8f, dot * 0.75f)), FontStyle.Regular, c.Label.Unit))
                for (var bit = 0; bit < 4; bit++)
                    Dim(c, (1 << bit).ToString(CultureInfo.InvariantCulture), new RectangleF(left - dot * 1.4f, top + (3 - bit) * (dot + gap), dot, dot), small);
            for (var col = 0; col < 6; col++)
            {
                var x = left + col * (dot + gap) + col / 2 * pairGap;
                for (var bit = 0; bit < BinaryBits[col]; bit++)
                {
                    var y = top + (3 - bit) * (dot + gap);
                    g.FillEllipse((digits[col] >> bit & 1) == 1 ? on : off, x, y, dot, dot);
                }
            }
            g.SmoothingMode = oldMode;
            return new RectangleF(left, top, width, 4 * dot + 3 * gap);
        }

        public override bool Wheel(int delta)
        {
            if (shownMonths == 0)
                return false;
            Page(-Math.Sign(delta) * Step);
            return true;
        }

        private void Page(int months)
        {
            shift = Math.Clamp(shift + months, -1200, 1200);
            lastPaging = DateTime.UtcNow;
        }

        private bool OnTitle(Point p) => shift != 0 && titles.Any(t => t.Contains(p));

        public override bool IsClickable(Point p) => prevButton.Contains(p) || nextButton.Contains(p) || OnTitle(p);

        public override bool Click(Point p)
        {
            if (prevButton.Contains(p))
                Page(-Step);
            else if (nextButton.Contains(p))
                Page(Step);
            else if (OnTitle(p))
                shift = 0;
            else
                return false;
            return true;
        }

        public override string? TooltipAt(Point p) =>
            prevButton.Contains(p) || nextButton.Contains(p) ? Strings.ClockPagingTip
            : OnTitle(p) ? Strings.ClockToday
            : clockRect.Contains(p) ? DateTime.Now.ToString("T", CultureInfo.CurrentUICulture)
                + (Options.Face == ClockFace.Binary ? "\n" + Strings.ClockBinaryHowTo : "")
            : null;

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            var options = Options;
            var faces = new ToolStripMenuItem(Strings.ClockFaceMenu);
            foreach (var (face, name) in new[] { (ClockFace.Digital, Strings.ClockDigital), (ClockFace.Analog, Strings.ClockAnalog), (ClockFace.Binary, Strings.ClockBinary) })
                faces.DropDownItems.Add(new ToolStripMenuItem(name, null, (_, _) => Save(options with { Face = face })) { Checked = options.Face == face });
            menu.Add(faces);
            menu.Add(new ToolStripMenuItem(Strings.ClockWeekNumbers, null, (_, _) => Save(options with { Weeks = !options.Weeks })) { Checked = options.Weeks });
            if (shift != 0)
                menu.Add(Strings.ClockToday, null, (_, _) => { shift = 0; RequestRedraw(); });
        }

        private void Save(ClockOptions options)
        {
            setOption(options.Format());
            RequestRedraw();
        }
    }
}
