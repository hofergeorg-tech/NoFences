using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Trading desk: deep navy with a chart grid, gold serif title and a small sparkline.</summary>
    public sealed class FinanceTheme : FenceTheme
    {
        private static readonly Color Bg = Color.FromArgb(8, 18, 32);
        private static readonly Color Band = Color.FromArgb(14, 30, 52);
        private static readonly Color Gold = Color.FromArgb(212, 175, 55);
        private static readonly Color Up = Color.FromArgb(40, 200, 120);
        private static readonly Color Text = Color.FromArgb(235, 238, 245);

        public override string Id => "finance";

        public override string DisplayName => "Finanzen (Börse)";


        public override Color Accent => Gold;

        public override int CornerPreference => 1;

        public override int MinAlpha => 200;

        public override int ContentInset => 3;

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Georgia", "Cambria", "Times New Roman" }, Math.Max(6, titleHeightPx * 0.42f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Bg)))
                g.FillRectangle(bg, bounds);

            // Chart grid
            using (var grid = new Pen(Color.FromArgb(18, 255, 255, 255), 1))
            {
                var step = 18 * s;
                for (var x = bounds.X + step; x < bounds.Right; x += step)
                    g.DrawLine(grid, x, bounds.Y + titleHeight, x, bounds.Bottom);
                for (var y = bounds.Y + titleHeight + step; y < bounds.Bottom; y += step)
                    g.DrawLine(grid, bounds.X, y, bounds.Right, y);
            }

            using (var band = new SolidBrush(Color.FromArgb(235, Band)))
                g.FillRectangle(band, bounds.X, bounds.Y, bounds.Width, titleHeight);
            using (var line = new Pen(Gold, 1.5f * s))
                g.DrawLine(line, bounds.X, bounds.Y + titleHeight - 1, bounds.Right, bounds.Y + titleHeight - 1);

            // Double gold border
            using (var outer = new Pen(Color.FromArgb(170, Gold), 1))
                g.DrawRectangle(outer, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            using (var inner = new Pen(Color.FromArgb(60, Gold), 1))
                g.DrawRectangle(inner, bounds.X + 3 * s, bounds.Y + titleHeight + 3 * s, bounds.Width - 1 - 6 * s, bounds.Height - titleHeight - 1 - 6 * s);

            DrawSparkline(g, new RectangleF(bounds.Right - 72 * s, bounds.Y + titleHeight * 0.25f, 58 * s, titleHeight * 0.5f), info, s);
        }

        /// <summary>A small rising chart, random but stable per fence.</summary>
        private static void DrawSparkline(Graphics g, RectangleF r, FenceInfo info, float s)
        {
            var rng = Seeded(info);
            const int n = 12;
            var values = new float[n];
            var v = 0.3f;
            for (var i = 0; i < n; i++)
            {
                v = Math.Clamp(v + (float)(rng.NextDouble() - 0.38) * 0.35f, 0, 1);
                values[i] = v;
            }
            values[^1] = Math.Max(values[^1], values.Max() * 0.95f); // end on a high

            var points = values.Select((val, i) => new PointF(r.X + r.Width * i / (n - 1), r.Bottom - val * r.Height)).ToArray();
            using (var fill = new SolidBrush(Color.FromArgb(40, Up)))
                g.FillPolygon(fill, points.Append(new PointF(r.Right, r.Bottom)).Append(new PointF(r.X, r.Bottom)).ToArray());
            using (var pen = new Pen(Up, 1.5f * s))
                g.DrawLines(pen, points);
            var last = points[^1];
            using var dot = new SolidBrush(Up);
            g.FillEllipse(dot, last.X - 2.5f * s, last.Y - 2.5f * s, 5 * s, 5 * s);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawShadowedString(g, text, font, Gold, Color.FromArgb(150, 0, 0, 0), new RectangleF(titleRect.X + 12 * s, titleRect.Y, titleRect.Width - 96 * s, titleRect.Height), format, s);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            using (var fill = new SolidBrush(Color.FromArgb(selected ? 55 : 28, Gold)))
                g.FillRectangle(fill, rect);
            using var pen = new Pen(Color.FromArgb(selected ? 220 : 140, Gold), 1);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Text, Color.FromArgb(200, 0, 0, 0), rect, format, s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(200, Gold), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Gold, x, top, height, 2 * s);
    }
}
