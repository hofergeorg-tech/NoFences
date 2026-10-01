using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>
    /// Sci-fi ship HUD: deep navy glass, cyan line work, corner brackets, chamfered highlights
    /// and condensed uppercase type (Bahnschrift ships with Windows 10/11).
    /// </summary>
    public sealed class StarCitizenTheme : FenceTheme
    {
        private static readonly Color Navy = Color.FromArgb(4, 14, 26);
        private static readonly Color Cyan = Color.FromArgb(0, 200, 255);
        private static readonly Color Ice = Color.FromArgb(200, 238, 255);
        private static readonly Color Amber = Color.FromArgb(255, 170, 40);

        private static readonly string[] TitleFonts = { "Bahnschrift SemiBold SemiConden", "Bahnschrift SemiBold", "Bahnschrift", "Segoe UI Semibold" };
        private static readonly string[] LabelFonts = { "Bahnschrift SemiLight SemiConde", "Bahnschrift Light", "Bahnschrift", "Segoe UI" };

        public override string Id => "starcitizen";

        public override string DisplayName => "Star Citizen (HUD)";

        public override int CornerPreference => 1;

        public override int MinAlpha => 140;

        public override int ContentInset => 4;

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(TitleFonts, Math.Max(6, titleHeightPx * 0.45f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(LabelFonts, 12.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        // Hair spaces give the wide-tracked look of HUD labels.
        public override string FormatTitle(string title) => string.Join(" ", title.ToUpperInvariant().ToCharArray());

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new SolidBrush(Color.FromArgb(alpha, Navy)))
                g.FillRectangle(bg, bounds);

            // Faint vertical gradient for depth
            var body = new Rectangle(bounds.X, bounds.Y + titleHeight, bounds.Width, Math.Max(1, bounds.Height - titleHeight));
            using (var glow = new LinearGradientBrush(body, Color.FromArgb(28, Cyan), Color.FromArgb(0, Cyan), LinearGradientMode.Vertical))
                g.FillRectangle(glow, body);

            // Title band
            var title = new Rectangle(bounds.X, bounds.Y, bounds.Width, titleHeight);
            using (var band = new LinearGradientBrush(title, Color.FromArgb(90, 0, 60, 90), Color.FromArgb(30, 0, 60, 90), LinearGradientMode.Horizontal))
                g.FillRectangle(band, title);

            var lineY = bounds.Y + titleHeight - 1;
            using (var thin = new Pen(Color.FromArgb(90, Cyan), 1))
                g.DrawLine(thin, bounds.X, lineY, bounds.Right, lineY);
            using (var thick = new Pen(Color.FromArgb(230, Cyan), 2 * s))
                g.DrawLine(thick, bounds.X, lineY, bounds.X + bounds.Width * 0.35f, lineY);

            // Status ticks on the right of the title band
            using (var tick = new Pen(Color.FromArgb(160, Cyan), Math.Max(1, s)))
            {
                var tx = bounds.Right - 10 * s;
                for (var i = 0; i < 4; i++)
                {
                    var h = (i + 1) * 2.5f * s;
                    g.DrawLine(tick, tx - i * 4 * s, lineY - 4 * s, tx - i * 4 * s, lineY - 4 * s - h);
                }
            }

            // Hull outline + corner brackets
            var outline = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1.5f, bounds.Height - 1.5f);
            using (var border = new Pen(Color.FromArgb(55, Cyan), 1))
                g.DrawRectangle(border, outline.X, outline.Y, outline.Width, outline.Height);

            var bw = 2 * s;
            var inner = RectangleF.Inflate(outline, -bw / 2, -bw / 2);
            using (var bracket = new Pen(Color.FromArgb(235, Cyan), bw) { LineJoin = LineJoin.Miter })
                DrawCornerBrackets(g, inner, Math.Min(14 * s, Math.Min(inner.Width, inner.Height) / 3), bracket);

            // Small amber "power" pip in the title
            using (var pip = new SolidBrush(Color.FromArgb(220, Amber)))
                g.FillRectangle(pip, bounds.X + 9 * s, bounds.Y + titleHeight / 2f - 2.5f * s, 5 * s, 5 * s);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var rect = new RectangleF(titleRect.X + 20 * s, titleRect.Y, titleRect.Width - 60 * s, titleRect.Height);
            using var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            DrawShadowedString(g, text, font, Ice, Color.FromArgb(120, 0, 100, 160), rect, format, 0);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;

            var r = new RectangleF(rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1, rect.Height - 1);
            using (var path = ChamferedRect(r, 7 * s))
            {
                using var fill = new SolidBrush(Color.FromArgb(selected ? 70 : 38, Cyan));
                g.FillPath(fill, path);
                if (selected)
                {
                    using var outline = new Pen(Color.FromArgb(170, Cyan), 1);
                    g.DrawPath(outline, path);
                }
            }

            if (hover)
            {
                using var pen = new Pen(Color.FromArgb(230, Cyan), Math.Max(1, 1.5f * s));
                DrawCornerBrackets(g, RectangleF.Inflate(r, -1, -1), 6 * s, pen);
            }
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Ice, Color.FromArgb(200, 0, 8, 16), rect, format, 1f * s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            var cx = track.X + track.Width / 2f;
            using (var line = new Pen(Color.FromArgb(50, Cyan), 1))
                g.DrawLine(line, cx, track.Top, cx, track.Bottom);
            using var brush = new SolidBrush(Color.FromArgb(190, Cyan));
            g.FillRectangle(brush, cx - 1.5f * s, thumb.Y, 3 * s, thumb.Height);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            using var pen = new Pen(Amber, 2 * s);
            g.DrawLine(pen, x, top, x, top + height);
            using var brush = new SolidBrush(Amber);
            var t = 4 * s;
            g.FillPolygon(brush, new[] { new PointF(x - t, top), new PointF(x + t, top), new PointF(x, top + t) });
            g.FillPolygon(brush, new[] { new PointF(x - t, top + height), new PointF(x + t, top + height), new PointF(x, top + height - t) });
        }
    }
}
