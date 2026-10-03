using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Gamer RGB: black body, rainbow edge lighting, slanted title plate, italic caps.</summary>
    public sealed class GamingTheme : FenceTheme
    {
        private static readonly Color Bg = Color.FromArgb(10, 10, 15);
        private static readonly Color Red = Color.FromArgb(255, 0, 80);
        private static readonly Color Purple = Color.FromArgb(150, 0, 255);
        private static readonly Color Blue = Color.FromArgb(0, 140, 255);
        private static readonly Color Cyan = Color.FromArgb(0, 255, 200);

        public override string Id => "gaming";

        public override string DisplayName => "Gaming (RGB)";


        public override Color Accent => Purple;

        public override int CornerPreference => 1;

        public override int MinAlpha => 200;

        public override int ContentInset => 4;

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Bahnschrift SemiBold", "Bahnschrift", "Segoe UI Black" }, Math.Max(6, titleHeightPx * 0.46f), FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) =>
            CreateFont(new[] { "Bahnschrift", "Segoe UI" }, 12.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override string FormatTitle(string title) => title.ToUpperInvariant();

        private static LinearGradientBrush RgbBrush(RectangleF r, int alpha = 255)
        {
            var brush = new LinearGradientBrush(r, Red, Cyan, LinearGradientMode.ForwardDiagonal);
            brush.InterpolationColors = new ColorBlend
            {
                Colors = new[] { Color.FromArgb(alpha, Red), Color.FromArgb(alpha, Purple), Color.FromArgb(alpha, Blue), Color.FromArgb(alpha, Cyan) },
                Positions = new[] { 0f, 0.35f, 0.7f, 1f }
            };
            return brush;
        }

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Bg)))
                g.FillRectangle(bg, bounds);

            // Slanted title plate
            var plateW = bounds.Width * 0.62f;
            var slant = titleHeight * 0.6f;
            using (var plate = new LinearGradientBrush(new RectangleF(bounds.X, bounds.Y, plateW + slant, titleHeight), Color.FromArgb(220, Purple), Color.FromArgb(200, Red), LinearGradientMode.Horizontal))
            {
                g.FillPolygon(plate, new[]
                {
                    new PointF(bounds.X, bounds.Y),
                    new PointF(bounds.X + plateW + slant, bounds.Y),
                    new PointF(bounds.X + plateW, bounds.Y + titleHeight),
                    new PointF(bounds.X, bounds.Y + titleHeight)
                });
            }

            // Speed stripes after the plate
            using (var stripe = new SolidBrush(Color.FromArgb(120, Red)))
            {
                for (var i = 1; i <= 3; i++)
                {
                    var x = bounds.X + plateW + slant + i * 9 * s;
                    g.FillPolygon(stripe, new[]
                    {
                        new PointF(x, bounds.Y), new PointF(x + 4 * s, bounds.Y),
                        new PointF(x + 4 * s - slant, bounds.Y + titleHeight), new PointF(x - slant, bounds.Y + titleHeight)
                    });
                }
            }

            // RGB edge lighting with a soft glow
            var r = new RectangleF(bounds.X + 1 * s, bounds.Y + 1 * s, bounds.Width - 2.5f * s, bounds.Height - 2.5f * s);
            using (var glowBrush = RgbBrush(bounds, 60))
            using (var glow = new Pen(glowBrush, 5 * s))
                g.DrawRectangle(glow, r.X, r.Y, r.Width, r.Height);
            using (var edgeBrush = RgbBrush(bounds))
            using (var edge = new Pen(edgeBrush, 2 * s))
                g.DrawRectangle(edge, r.X, r.Y, r.Width, r.Height);
        }

        public override bool AnimatesOnHover => true;

        /// <summary>RGB light running around the edge.</summary>
        public override void DrawHoverEffect(Graphics g, Rectangle bounds, int titleHeight, float t, float s)
        {
            var r = new RectangleF(bounds.X + 1 * s, bounds.Y + 1 * s, bounds.Width - 2.5f * s, bounds.Height - 2.5f * s);
            using var brush = new LinearGradientBrush(bounds, Red, Cyan, (t * 120) % 360);
            brush.InterpolationColors = new ColorBlend
            {
                Colors = new[] { Red, Purple, Blue, Cyan, Red },
                Positions = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }
            };
            using var pen = new Pen(brush, 3 * s);
            g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            var rect = new RectangleF(titleRect.X + 12 * s, titleRect.Y, titleRect.Width * 0.62f, titleRect.Height);
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(170, 60, 0, 30), rect, format, 2 * s);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            using (var fill = new SolidBrush(Color.FromArgb(selected ? 70 : 45, selected ? Red : Purple)))
                g.FillRectangle(fill, rect);
            using var brush = RgbBrush(rect);
            using var pen = new Pen(brush, (selected ? 2 : 1.3f) * s);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(220, 0, 0, 0), rect, format, s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            using var brush = RgbBrush(thumb);
            g.FillRectangle(brush, track.X + track.Width / 2f - 2 * s, thumb.Y, 4 * s, thumb.Height);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Cyan, x, top, height, 3 * s);
    }
}
