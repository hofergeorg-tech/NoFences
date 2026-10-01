using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Warm and friendly: peach gradient, hearts, handwritten title, soft rounded highlights.</summary>
    public sealed class FamilyTheme : FenceTheme
    {
        private static readonly Color Top = Color.FromArgb(255, 216, 182);
        private static readonly Color Bottom = Color.FromArgb(255, 176, 162);
        private static readonly Color Ink = Color.FromArgb(96, 46, 38);
        private static readonly Color Heart = Color.FromArgb(228, 64, 92);

        public override string Id => "family";

        public override string DisplayName => "Familie";

        public override int MinAlpha => 205;

        public override int ContentInset => 3;

        public override Color HintColor => Color.FromArgb(170, Ink);

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Segoe Print", "Ink Free", "Segoe UI Semibold" }, Math.Max(6, titleHeightPx * 0.38f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) =>
            CreateFont(new[] { "Segoe UI Semibold", "Segoe UI" }, 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new LinearGradientBrush(bounds, Color.FromArgb(alpha, Top), Color.FromArgb(alpha, Bottom), LinearGradientMode.Vertical))
                g.FillRectangle(bg, bounds);
            using (var band = new SolidBrush(Color.FromArgb(70, Color.White)))
                g.FillRectangle(band, bounds.X, bounds.Y, bounds.Width, titleHeight);

            // Dotted divider
            using (var dots = new SolidBrush(Color.FromArgb(150, Heart)))
            {
                var y = bounds.Y + titleHeight - 2 * s;
                for (var x = bounds.X + 10 * s; x < bounds.Right - 10 * s; x += 8 * s)
                    g.FillEllipse(dots, x, y - 1.2f * s, 2.4f * s, 2.4f * s);
            }

            DrawRounded(g, Color.FromArgb(150, Color.White), 2 * s, RectangleF.Inflate(bounds, -2 * s, -2 * s), 8 * s);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var cy = titleRect.Y + titleRect.Height / 2f;
            var size = 11 * s;
            FillHeart(g, titleRect.X + 14 * s, cy, size, Heart);
            FillHeart(g, titleRect.Right - 14 * s, cy, size, Heart);
            using var format = TitleFormat(StringAlignment.Center);
            DrawPlainString(g, text, font, Ink, RectangleF.Inflate(titleRect, -28 * s, 0), format);
        }

        private static void FillHeart(Graphics g, float cx, float cy, float size, Color color)
        {
            using var path = new GraphicsPath();
            var w = size;
            var h = size;
            path.AddBezier(cx, cy + h * 0.4f, cx - w * 0.7f, cy - h * 0.05f, cx - w * 0.35f, cy - h * 0.6f, cx, cy - h * 0.2f);
            path.AddBezier(cx, cy - h * 0.2f, cx + w * 0.35f, cy - h * 0.6f, cx + w * 0.7f, cy - h * 0.05f, cx, cy + h * 0.4f);
            path.CloseFigure();
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 150 : 95, Color.White), rect, 10 * s);
            if (selected)
                DrawRounded(g, Color.FromArgb(170, Heart), 1.5f * s, rect, 10 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Ink, Color.FromArgb(110, Color.White), rect, format, s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(170, Heart), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            DrawBarMarker(g, Heart, x, top, height, 2 * s);
            FillHeart(g, x, top, 9 * s, Heart);
        }
    }
}
