using NoFences.Model;
using NoFences.Util;

namespace NoFences.Themes
{
    /// <summary>
    /// For tired or weak eyes: solid black, white bold text in a larger size, yellow title and thick
    /// borders – readable on any wallpaper, no transparency.
    /// </summary>
    public sealed class HighContrastTheme : FenceTheme
    {
        private static readonly Color Yellow = Color.FromArgb(255, 216, 0);

        public override string Id => "contrast";

        public override string DisplayName => Strings.ThemeName(Id);

        public override Color Accent => Yellow;

        public override bool Glass => false;

        public override int CornerPreference => 1;

        public override int MinAlpha => 255;

        public override int ContentInset => 3;

        public override Color HintColor => Color.White;

        public override (Color Back, Color Fore) EditorColors => (Color.Black, Color.White);

        public override Font CreateTitleFont(int titleHeightPx) => new("Segoe UI", Math.Max(7, titleHeightPx * 0.5f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => new("Segoe UI", 14f * s, FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateNoteFont(float s) => new("Segoe UI", 17f * s, FontStyle.Bold, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            // Not g.Clear: that would also wipe what's drawn around the fence (previews)
            g.FillRectangle(Brushes.Black, bounds);
            using (var bar = new SolidBrush(Yellow))
                g.FillRectangle(bar, bounds.X, bounds.Y + titleHeight - 3 * s, bounds.Width, 3 * s);
            var width = Math.Max(2, 3 * s);
            using var border = new Pen(Color.White, width);
            g.DrawRectangle(border, bounds.X + width / 2, bounds.Y + width / 2, bounds.Width - width, bounds.Height - width);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Yellow, new RectangleF(titleRect.X + 12 * s, titleRect.Y, titleRect.Width - 24 * s, titleRect.Height - 3 * s), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            // Clear outlines instead of faint tints
            using var pen = new Pen(selected ? Yellow : Color.White, (selected ? 3 : 2) * s);
            g.DrawRectangle(pen, rect.X + s, rect.Y + s, rect.Width - 2 * s, rect.Height - 2 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawPlainString(g, text, font, Color.White, rect, format);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            using var brush = new SolidBrush(Yellow);
            g.FillRectangle(brush, thumb.X, thumb.Y, Math.Max(4 * s, thumb.Width), thumb.Height);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Yellow, x, top, height, 4 * s);
    }
}
