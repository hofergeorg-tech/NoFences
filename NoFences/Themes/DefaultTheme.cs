using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>The original NoFences look: frosted glass, white text.</summary>
    public sealed class DefaultTheme : FenceTheme
    {
        public override string Id => "default";

        public override string DisplayName => "Standard (Glas)";


        public override Color Accent => Color.FromArgb(120, 190, 255);

        public override bool UsesCustomColor => true;

        public override Font CreateTitleFont(int titleHeightPx) =>
            new("Segoe UI", Math.Max(6, titleHeightPx / 2f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using var bg = new SolidBrush(Color.FromArgb(Alpha(info), Color.FromArgb(info.BackgroundColor)));
            g.FillRectangle(bg, bounds);
            using var title = new SolidBrush(Color.FromArgb(50, Color.Black));
            g.FillRectangle(title, new Rectangle(bounds.X, bounds.Y, bounds.Width, titleHeight));
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(text, font, Brushes.White, titleRect, format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;

            var fill = selected
                ? Color.FromArgb(hover ? 100 : 80, hover ? SystemColors.GradientActiveCaption : SystemColors.GradientInactiveCaption)
                : Color.FromArgb(80, SystemColors.ActiveCaption);
            using var brush = new SolidBrush(fill);
            g.FillRectangle(brush, rect);
            using var pen = new Pen(Color.FromArgb(120, SystemColors.ActiveBorder));
            g.DrawRectangle(pen, Rectangle.Inflate(rect, -1, -1));
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(180, 15, 15, 15), rect, format, 1.5f * s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            using var brush = new SolidBrush(Color.FromArgb(150, Color.Black));
            g.FillRectangle(brush, thumb);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            using var pen = new Pen(Color.FromArgb(220, Color.White), 2 * s);
            g.DrawLine(pen, x, top, x, top + height);
        }
    }
}
