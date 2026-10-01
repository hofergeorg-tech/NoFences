using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Terminal / code editor: window dots, "~/name $" prompt title, vim tildes, editor selection.</summary>
    public sealed class NerdTheme : FenceTheme
    {
        private static readonly Color Bg = Color.FromArgb(17, 19, 26);
        private static readonly Color Bar = Color.FromArgb(30, 33, 43);
        private static readonly Color Green = Color.FromArgb(80, 250, 123);
        private static readonly Color Comment = Color.FromArgb(98, 114, 164);
        private static readonly Color Fg = Color.FromArgb(222, 225, 232);
        private static readonly Color Selection = Color.FromArgb(44, 74, 118);

        private static readonly string[] Mono = { "Cascadia Code", "Cascadia Mono", "Consolas" };

        public override string Id => "nerd";

        public override string DisplayName => "Nerd (Terminal)";

        public override int CornerPreference => 3;

        public override int MinAlpha => 200;

        public override int ContentInset => 8;

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(Mono, Math.Max(6, titleHeightPx * 0.4f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(Mono, 11.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override string FormatTitle(string title) => "~/" + title.Trim().ToLowerInvariant().Replace(' ', '-') + " $";

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Bg)))
                g.FillRectangle(bg, bounds);
            using (var bar = new SolidBrush(Color.FromArgb(235, Bar)))
                g.FillRectangle(bar, bounds.X, bounds.Y, bounds.Width, titleHeight);

            // Window buttons
            var r = 5f * s;
            var cy = bounds.Y + titleHeight / 2f;
            var colors = new[] { Color.FromArgb(255, 95, 86), Color.FromArgb(255, 189, 46), Color.FromArgb(39, 201, 63) };
            for (var i = 0; i < 3; i++)
            {
                using var b = new SolidBrush(colors[i]);
                g.FillEllipse(b, bounds.X + 12 * s + i * 16 * s - r, cy - r, 2 * r, 2 * r);
            }

            // vim-style "~" for empty lines down the left edge
            using var font = new Font("Consolas", 11 * s, FontStyle.Regular, GraphicsUnit.Pixel);
            using var tilde = new SolidBrush(Color.FromArgb(110, Comment));
            for (var y = bounds.Y + titleHeight + 4 * s; y < bounds.Bottom - 12 * s; y += 17 * s)
                g.DrawString("~", font, tilde, bounds.X + 3 * s, y);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Green, new RectangleF(titleRect.X + 58 * s, titleRect.Y, titleRect.Width - 66 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 230 : 150, Selection), rect, 3 * s);
            if (selected)
                DrawRounded(g, Color.FromArgb(140, Green), 1, rect, 3 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawPlainString(g, text, font, Fg, rect, format);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(180, Comment), s);

        // A block cursor
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            using var b = new SolidBrush(Color.FromArgb(220, Green));
            g.FillRectangle(b, x - 2 * s, top, 4 * s, height);
        }
    }
}
