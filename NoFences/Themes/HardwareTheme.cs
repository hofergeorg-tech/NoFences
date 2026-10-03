using NoFences.Util;
using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Circuit board: green solder mask, copper traces with vias, silkscreen type, IC chip hover.</summary>
    public sealed class HardwareTheme : FenceTheme
    {
        private static readonly Color Board = Color.FromArgb(10, 52, 32);
        private static readonly Color Mask = Color.FromArgb(4, 32, 18);
        private static readonly Color Copper = Color.FromArgb(214, 162, 72);
        private static readonly Color Silk = Color.FromArgb(236, 240, 228);

        public override string Id => "hardware";

        public override string DisplayName => Strings.ThemeName(Id);


        public override Color Accent => Copper;

        public override int CornerPreference => 1;

        public override int MinAlpha => 185;

        public override int ContentInset => 4;

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Cascadia Mono", "Consolas" }, Math.Max(6, titleHeightPx * 0.4f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(new[] { "Segoe UI" }, 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override string FormatTitle(string title) => title.ToUpperInvariant();

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Board)))
                g.FillRectangle(bg, bounds);

            DrawTraces(g, bounds, titleHeight, info, s);

            using (var band = new SolidBrush(Color.FromArgb(210, Mask)))
                g.FillRectangle(band, bounds.X, bounds.Y, bounds.Width, titleHeight);
            using (var line = new Pen(Copper, 1.5f * s))
                g.DrawLine(line, bounds.X, bounds.Y + titleHeight - 1, bounds.Right, bounds.Y + titleHeight - 1);

            using (var border = new Pen(Color.FromArgb(150, Copper), 1))
                g.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);

            // Mounting holes
            var r = 4.5f * s;
            foreach (var p in new[] { new PointF(bounds.Right - 10 * s, bounds.Y + titleHeight / 2f), new PointF(bounds.X + 10 * s, bounds.Bottom - 10 * s), new PointF(bounds.Right - 10 * s, bounds.Bottom - 10 * s) })
                DrawVia(g, p, r, s);
        }

        private static void DrawTraces(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var rng = Seeded(info);
            var body = Rectangle.FromLTRB(bounds.X, bounds.Y + titleHeight, bounds.Right, bounds.Bottom);
            if (body.Height < 10)
                return;
            var count = Math.Clamp(body.Width * body.Height / (int)(7000 * s * s), 5, 35);
            using var pen = new Pen(Color.FromArgb(70, Copper), 1.6f * s) { LineJoin = LineJoin.Round };
            for (var i = 0; i < count; i++)
            {
                var x = body.X + (float)rng.NextDouble() * body.Width;
                var y = body.Y + (float)rng.NextDouble() * body.Height;
                var run1 = (20 + rng.Next(60)) * s * (rng.Next(2) == 0 ? -1 : 1);
                var diag = (8 + rng.Next(20)) * s;
                var dirY = rng.Next(2) == 0 ? -1 : 1;
                var run2 = (15 + rng.Next(50)) * s * Math.Sign(run1);
                var p1 = new PointF(x + run1, y);
                var p2 = new PointF(p1.X + diag * Math.Sign(run1), y + diag * dirY);
                var p3 = new PointF(p2.X + run2, p2.Y);
                g.DrawLines(pen, new[] { new PointF(x, y), p1, p2, p3 });
                DrawVia(g, new PointF(x, y), 2.6f * s, s, 90);
                DrawVia(g, p3, 2.6f * s, s, 90);
            }
        }

        private static void DrawVia(Graphics g, PointF c, float r, float s, int alpha = 200)
        {
            using (var ring = new SolidBrush(Color.FromArgb(alpha, Copper)))
                g.FillEllipse(ring, c.X - r, c.Y - r, 2 * r, 2 * r);
            var hole = r * 0.45f;
            using var holeBrush = new SolidBrush(Color.FromArgb(Math.Min(255, alpha + 40), Mask));
            g.FillEllipse(holeBrush, c.X - hole, c.Y - hole, 2 * hole, 2 * hole);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            // Small IC package with pins as the title glyph
            var chip = new RectangleF(titleRect.X + 9 * s, titleRect.Y + titleRect.Height / 2f - 6 * s, 14 * s, 12 * s);
            using (var body = new SolidBrush(Color.FromArgb(28, 28, 28)))
                g.FillRectangle(body, chip);
            using (var outline = new Pen(Color.FromArgb(200, Silk), Math.Max(1, s)))
                g.DrawRectangle(outline, chip.X, chip.Y, chip.Width, chip.Height);
            using (var notch = new SolidBrush(Silk))
                g.FillEllipse(notch, chip.X + 2.5f * s, chip.Y + 2.5f * s, 2.5f * s, 2.5f * s);
            using (var pin = new Pen(Silk, Math.Max(1, s)))
            {
                for (var i = 0; i < 3; i++)
                {
                    var py = chip.Y + 2.5f * s + i * 3.5f * s;
                    g.DrawLine(pin, chip.X - 3 * s, py, chip.X, py);
                    g.DrawLine(pin, chip.Right, py, chip.Right + 3 * s, py);
                }
            }

            using var format = TitleFormat();
            DrawPlainString(g, text, font, Silk, new RectangleF(titleRect.X + 30 * s, titleRect.Y, titleRect.Width - 52 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            var r = RectangleF.Inflate(rect, -4 * s, -1);
            using (var fill = new SolidBrush(Color.FromArgb(selected ? 70 : 35, Copper)))
                g.FillRectangle(fill, r);
            using var pen = new Pen(Color.FromArgb(selected ? 230 : 180, Copper), Math.Max(1, s));
            g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
            // IC pins along both sides
            for (var y = r.Y + 6 * s; y < r.Bottom - 4 * s; y += 7 * s)
            {
                g.DrawLine(pen, r.X - 3 * s, y, r.X, y);
                g.DrawLine(pen, r.Right, y, r.Right + 3 * s, y);
            }
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Silk, Color.FromArgb(200, 0, 15, 5), rect, format, s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(200, Copper), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            DrawBarMarker(g, Copper, x, top, height, 2 * s);
            DrawVia(g, new PointF(x, top), 3 * s, s);
            DrawVia(g, new PointF(x, top + height), 3 * s, s);
        }
    }
}
