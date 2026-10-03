using NoFences.Util;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Cork pinboard: wooden frame, title on a slanted strip of masking tape with push pins.</summary>
    public sealed class HobbyTheme : FenceTheme
    {
        private static readonly Color Cork = Color.FromArgb(178, 130, 84);
        private static readonly Color CorkDark = Color.FromArgb(118, 78, 44);
        private static readonly Color CorkLight = Color.FromArgb(214, 172, 120);
        private static readonly Color Wood = Color.FromArgb(108, 68, 34);
        private static readonly Color Tape = Color.FromArgb(242, 230, 192);
        private static readonly Color Ink = Color.FromArgb(62, 44, 28);
        private static readonly Color Pin = Color.FromArgb(220, 48, 52);

        public override string Id => "hobby";

        public override string DisplayName => Strings.ThemeName(Id);


        public override Color Accent => Pin;

        public override int MinAlpha => 215;

        public override int ContentInset => 5;

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Segoe Print", "Ink Free", "Comic Sans MS" }, Math.Max(6, titleHeightPx * 0.36f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) =>
            CreateFont(new[] { "Segoe UI Semibold", "Segoe UI" }, 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new SolidBrush(Color.FromArgb(alpha, Cork)))
                g.FillRectangle(bg, bounds);

            // Cork grain
            var rng = Seeded(info);
            var count = Math.Min(900, bounds.Width * bounds.Height / (int)(120 * s * s));
            using var dark = new SolidBrush(Color.FromArgb(alpha / 2, CorkDark));
            using var light = new SolidBrush(Color.FromArgb(alpha / 2, CorkLight));
            for (var i = 0; i < count; i++)
            {
                var size = (1 + rng.Next(3)) * s * 0.8f;
                g.FillRectangle(i % 3 == 0 ? light : dark, bounds.X + rng.Next(bounds.Width), bounds.Y + rng.Next(bounds.Height), size, size);
            }

            // Wooden frame
            var w = 5 * s;
            using var frame = new Pen(Color.FromArgb(240, Wood), w);
            g.DrawRectangle(frame, bounds.X + w / 2, bounds.Y + w / 2, bounds.Width - w, bounds.Height - w);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var size = g.MeasureString(text, font);
            var tapeW = Math.Min(titleRect.Width - 30 * s, size.Width + 34 * s);
            var tapeH = Math.Min(titleRect.Height - 6 * s, size.Height + 6 * s);
            var cx = titleRect.X + titleRect.Width / 2f;
            var cy = titleRect.Y + titleRect.Height / 2f + 2 * s;

            var state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(-2.5f);
            var tape = new RectangleF(-tapeW / 2, -tapeH / 2, tapeW, tapeH);
            using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillRectangle(shadow, tape.X + 2 * s, tape.Y + 2 * s, tape.Width, tape.Height);
            using (var brush = new SolidBrush(Color.FromArgb(235, Tape)))
                g.FillRectangle(brush, tape);
            using (var format = TitleFormat(StringAlignment.Center))
                DrawPlainString(g, text, font, Ink, RectangleF.Inflate(tape, -10 * s, 0), format);
            g.Restore(state);

            DrawPin(g, cx - tapeW / 2 + 6 * s, cy - 1 * s, s);
            DrawPin(g, cx + tapeW / 2 - 6 * s, cy - 3 * s, s);
        }

        private static void DrawPin(Graphics g, float x, float y, float s)
        {
            var r = 4.5f * s;
            using (var shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                g.FillEllipse(shadow, x - r + 1.5f * s, y - r + 2 * s, 2 * r, 2 * r);
            using (var head = new SolidBrush(Pin))
                g.FillEllipse(head, x - r, y - r, 2 * r, 2 * r);
            using var shine = new SolidBrush(Color.FromArgb(180, Color.White));
            g.FillEllipse(shine, x - r * 0.5f, y - r * 0.6f, r * 0.7f, r * 0.6f);
        }

        // Hover = a little paper note behind the item
        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            var r = RectangleF.Inflate(rect, -2 * s, -1);
            using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                g.FillRectangle(shadow, r.X + 2 * s, r.Y + 2 * s, r.Width, r.Height);
            using (var note = new SolidBrush(Color.FromArgb(selected ? 150 : 95, 255, 248, 200)))
                g.FillRectangle(note, r);
            if (selected)
                DrawPin(g, r.X + r.Width / 2, r.Y + 3 * s, s * 0.8f);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(220, 55, 32, 14), rect, format, s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(220, Wood), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            DrawBarMarker(g, Pin, x, top + (int)(4 * s), height, 2 * s);
            DrawPin(g, x, top, s);
        }
    }
}
