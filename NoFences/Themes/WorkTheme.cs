using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Clean, light business look: white paper, navy title bar, dark text.</summary>
    public sealed class WorkTheme : FenceTheme
    {
        private static readonly Color Paper = Color.FromArgb(246, 248, 251);
        private static readonly Color Navy = Color.FromArgb(28, 58, 105);
        private static readonly Color AccentBlue = Color.FromArgb(0, 120, 212);
        private static readonly Color Text = Color.FromArgb(32, 36, 44);
        private static readonly Color Line = Color.FromArgb(200, 208, 220);

        public override string Id => "work";

        public override string DisplayName => "Arbeit (Business)";


        public override Color Accent => AccentBlue;

        public override int CornerPreference => 3;

        public override int MinAlpha => 215;

        public override int ContentInset => 2;

        public override Color HintColor => Color.FromArgb(150, Text);

        public override Font CreateTitleFont(int titleHeightPx) => new("Segoe UI Semibold", Math.Max(6, titleHeightPx * 0.42f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Paper)))
                g.FillRectangle(bg, bounds);
            using (var bar = new SolidBrush(Color.FromArgb(245, Navy)))
                g.FillRectangle(bar, bounds.X, bounds.Y, bounds.Width, titleHeight);
            using (var accent = new SolidBrush(Accent))
                g.FillRectangle(accent, bounds.X, bounds.Y + titleHeight - 2 * s, bounds.Width, 2 * s);
            using var border = new Pen(Line, 1);
            g.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Color.White, new RectangleF(titleRect.X + 12 * s, titleRect.Y - 1 * s, titleRect.Width - 24 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, selected ? Color.FromArgb(204, 224, 250) : Color.FromArgb(225, 236, 250), rect, 4 * s);
            if (selected)
                DrawRounded(g, Accent, 1, rect, 4 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawPlainString(g, text, font, Text, rect, format);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(160, 170, 185), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Accent, x, top, height, 2 * s);
    }
}
