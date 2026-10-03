using System.Drawing.Drawing2D;
using System.Drawing.Text;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>
    /// 80s arcade cabinet: near-black CRT with scanlines, neon magenta/cyan double border
    /// with stepped "pixel" corners, yellow block-shadowed title.
    /// </summary>
    public sealed class RetroArcadeTheme : FenceTheme
    {
        private static readonly Color Screen = Color.FromArgb(14, 4, 30);
        private static readonly Color Magenta = Color.FromArgb(255, 40, 200);
        private static readonly Color Cyan = Color.FromArgb(0, 240, 255);
        private static readonly Color Yellow = Color.FromArgb(255, 230, 40);

        // "Press Start 2P" etc. are used when the user has a pixel font installed.
        private static readonly string[] TitleFonts = { "Press Start 2P", "Pixeloid Sans", "VT323", "Lucida Console", "Consolas" };
        private static readonly string[] LabelFonts = { "Consolas", "Lucida Console" };

        public override string Id => "retroarcade";

        public override string DisplayName => "Retro-Arcade";


        public override Color Accent => Cyan;

        public override int CornerPreference => 1;

        public override int MinAlpha => 170;

        public override int ContentInset => 6;

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(TitleFonts, Math.Max(6, titleHeightPx * 0.38f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(LabelFonts, 11.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override string FormatTitle(string title) => title.ToUpperInvariant();

        private static int Px(float s) => Math.Max(2, (int)Math.Round(2 * s));

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            var p = Px(s);

            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Screen)))
                g.FillRectangle(bg, bounds);

            // CRT scanlines
            using (var scan = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                for (var y = bounds.Y + titleHeight; y < bounds.Bottom; y += p * 2)
                    g.FillRectangle(scan, bounds.X, y, bounds.Width, p / 2 + 1);
            }

            // Title band with checkered strip underneath
            using (var band = new SolidBrush(Color.FromArgb(80, Magenta)))
                g.FillRectangle(band, bounds.X, bounds.Y, bounds.Width, titleHeight);
            using (var block = new SolidBrush(Yellow))
            {
                var stripY = bounds.Y + titleHeight - p;
                for (var x = bounds.X + 3 * p; x < bounds.Right - 3 * p; x += p * 4)
                    g.FillRectangle(block, x, stripY, p * 2, p);
            }

            // Neon double border with notched corners
            DrawPixelBox(g, bounds, p, Magenta);
            DrawPixelBox(g, Rectangle.Inflate(bounds, -2 * p, -2 * p), Math.Max(1, p / 2), Color.FromArgb(200, Cyan));

            g.SmoothingMode = oldMode;
        }

        public override bool AnimatesOnHover => true;

        /// <summary>Blinking "INSERT COIN" at the bottom, like an attract screen.</summary>
        public override void DrawHoverEffect(Graphics g, Rectangle bounds, int titleHeight, float t, float s)
        {
            if (bounds.Height - titleHeight < 60 * s || (int)(t * 2) % 2 == 1)
                return;
            using var font = CreateFont(new[] { "Press Start 2P", "Consolas" }, 10 * s, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat { Alignment = StringAlignment.Center };
            var rect = new RectangleF(bounds.X, bounds.Bottom - 22 * s, bounds.Width, 16 * s);
            var old = g.TextRenderingHint;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            DrawShadowedString(g, "INSERT COIN", font, Yellow, Magenta, rect, format, Px(s));
            g.TextRenderingHint = old;
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var oldHint = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            var rect = RectangleF.Inflate(titleRect, -4 * Px(s), 0);
            DrawShadowedString(g, text, font, Yellow, Magenta, rect, format, Px(s));
            g.TextRenderingHint = oldHint;
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;

            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            var p = Px(s);
            var color = selected ? Yellow : Cyan;
            using (var fill = new SolidBrush(Color.FromArgb(selected ? 55 : 35, color)))
                g.FillRectangle(fill, Rectangle.Inflate(rect, -p, -p));
            DrawPixelBox(g, rect, Math.Max(1, p / 2 + (selected ? 1 : 0)), color);
            g.SmoothingMode = oldMode;
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(220, Magenta), rect, format, Math.Max(1, s));

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            var p = Px(s);
            using (var dots = new SolidBrush(Color.FromArgb(90, Cyan)))
            {
                for (var y = track.Top; y < track.Bottom; y += p * 3)
                    g.FillRectangle(dots, track.X + track.Width / 2 - p / 2, y, p, p);
            }
            using var brush = new SolidBrush(Cyan);
            g.FillRectangle(brush, track.X + track.Width / 2 - p, thumb.Y, p * 2, thumb.Height);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            var p = Px(s);
            using var brush = new SolidBrush(Yellow);
            for (var y = top; y < top + height; y += p * 2)
                g.FillRectangle(brush, x - p / 2, y, p, p);
        }

        /// <summary>Outline whose corners are cut by one "pixel" for the 8-bit staircase look.</summary>
        private static void DrawPixelBox(Graphics g, Rectangle r, int p, Color color)
        {
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, r.X + p, r.Y, r.Width - 2 * p, p);                 // top
            g.FillRectangle(brush, r.X + p, r.Bottom - p, r.Width - 2 * p, p);        // bottom
            g.FillRectangle(brush, r.X, r.Y + p, p, r.Height - 2 * p);                // left
            g.FillRectangle(brush, r.Right - p, r.Y + p, p, r.Height - 2 * p);        // right
            // inner corner steps
            g.FillRectangle(brush, r.X + p, r.Y + p, p, p);
            g.FillRectangle(brush, r.Right - 2 * p, r.Y + p, p, p);
            g.FillRectangle(brush, r.X + p, r.Bottom - 2 * p, p, p);
            g.FillRectangle(brush, r.Right - 2 * p, r.Bottom - 2 * p, p, p);
        }
    }
}
