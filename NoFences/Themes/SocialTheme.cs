using NoFences.Util;
using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Social app: purple–pink–orange gradient, chat bubble glyph, notification badge, bubbly hovers.</summary>
    public sealed class SocialTheme : FenceTheme
    {
        private static readonly Color Purple = Color.FromArgb(131, 58, 180);
        private static readonly Color Pink = Color.FromArgb(225, 48, 108);
        private static readonly Color Orange = Color.FromArgb(252, 160, 69);
        private static readonly Color Badge = Color.FromArgb(255, 59, 48);

        public override string Id => "social";

        public override string DisplayName => Strings.ThemeName(Id);


        public override Color Accent => Color.White;

        public override int MinAlpha => 190;

        public override int ContentInset => 3;

        public override Font CreateTitleFont(int titleHeightPx) => new("Segoe UI Semibold", Math.Max(6, titleHeightPx * 0.44f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => new("Segoe UI Semibold", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new LinearGradientBrush(bounds, Purple, Orange, LinearGradientMode.ForwardDiagonal))
            {
                bg.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(alpha, Purple), Color.FromArgb(alpha, Pink), Color.FromArgb(alpha, Orange) },
                    Positions = new[] { 0f, 0.55f, 1f }
                };
                g.FillRectangle(bg, bounds);
            }
            // Slightly darker content area for icon contrast
            using (var shade = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
                g.FillRectangle(shade, bounds.X, bounds.Y + titleHeight, bounds.Width, bounds.Height - titleHeight);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var cy = titleRect.Y + titleRect.Height / 2f;

            // Chat bubble glyph
            var bubble = new RectangleF(titleRect.X + 10 * s, cy - 7 * s, 18 * s, 13 * s);
            FillRounded(g, Color.White, bubble, 5 * s);
            using (var tail = new SolidBrush(Color.White))
                g.FillPolygon(tail, new[] { new PointF(bubble.X + 4 * s, bubble.Bottom - 1), new PointF(bubble.X + 9 * s, bubble.Bottom - 1), new PointF(bubble.X + 2 * s, bubble.Bottom + 4 * s) });
            using (var dot = new SolidBrush(Pink))
            {
                for (var i = 0; i < 3; i++)
                    g.FillEllipse(dot, bubble.X + 4 * s + i * 4 * s, bubble.Y + bubble.Height / 2 - 1.2f * s, 2.4f * s, 2.4f * s);
            }

            // Notification badge
            var br = 7 * s;
            var bx = titleRect.Right - 16 * s;
            using (var badge = new SolidBrush(Badge))
                g.FillEllipse(badge, bx - br, cy - br, 2 * br, 2 * br);
            using (var ring = new Pen(Color.White, 1.5f * s))
                g.DrawEllipse(ring, bx - br, cy - br, 2 * br, 2 * br);
            using (var badgeFont = new Font("Segoe UI", 9 * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString("♥", badgeFont, Brushes.White, new RectangleF(bx - br, cy - br, 2 * br, 2 * br), center);

            using var format = TitleFormat();
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(90, 60, 0, 60), new RectangleF(titleRect.X + 36 * s, titleRect.Y, titleRect.Width - 64 * s, titleRect.Height), format, s);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 100 : 60, Color.White), rect, 12 * s);
            if (selected)
                DrawRounded(g, Color.FromArgb(220, Color.White), 1.5f * s, rect, 12 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(170, 50, 0, 50), rect, format, s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(190, Color.White), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Color.White, x, top, height, 2.5f * s);
    }
}
