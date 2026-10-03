using Microsoft.Win32;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Themes
{
    /// <summary>Glass like the standard style, tinted with the Windows accent color (follows changes to it).</summary>
    public sealed class WindowsAccentTheme : FenceTheme
    {
        private static Color cached = Color.FromArgb(0, 120, 212);
        private static DateTime cachedAt = DateTime.MinValue;

        public override string Id => "windows";

        public override string DisplayName => Strings.ThemeName(Id);

        /// <summary>The accent color from Settings → Personalization → Colors (re-read every few seconds).</summary>
        public static Color SystemAccent
        {
            get
            {
                if (DateTime.UtcNow - cachedAt < TimeSpan.FromSeconds(5))
                    return cached;
                cachedAt = DateTime.UtcNow;
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                    // Stored as 0xAABBGGRR
                    if (key?.GetValue("AccentColor") is int abgr)
                        cached = Color.FromArgb(abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);
                }
                catch (Exception)
                {
                }
                return cached;
            }
        }

        public override Color Accent => Lighten(SystemAccent, 0.25);

        private static Color Lighten(Color c, double amount) => Color.FromArgb(
            (int)(c.R + (255 - c.R) * amount), (int)(c.G + (255 - c.G) * amount), (int)(c.B + (255 - c.B) * amount));

        private static Color Darken(Color c, double amount) => Color.FromArgb(
            (int)(c.R * (1 - amount)), (int)(c.G * (1 - amount)), (int)(c.B * (1 - amount)));

        public override Font CreateTitleFont(int titleHeightPx) =>
            new("Segoe UI Semibold", Math.Max(6, titleHeightPx / 2f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var accent = SystemAccent;
            using var bg = new SolidBrush(Color.FromArgb(Alpha(info), Darken(accent, 0.65)));
            g.FillRectangle(bg, bounds);
            using var title = new SolidBrush(Color.FromArgb(Math.Min(255, Alpha(info) + 60), Darken(accent, 0.2)));
            g.FillRectangle(title, new Rectangle(bounds.X, bounds.Y, bounds.Width, titleHeight));
            using var line = new Pen(Color.FromArgb(220, Accent), Math.Max(1, 2 * s));
            g.DrawLine(line, bounds.X, bounds.Y + titleHeight - s, bounds.Right, bounds.Y + titleHeight - s);
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
            using var brush = new SolidBrush(Color.FromArgb(selected ? 110 : 70, Accent));
            g.FillRectangle(brush, rect);
            if (selected)
            {
                using var pen = new Pen(Color.FromArgb(200, Accent));
                g.DrawRectangle(pen, Rectangle.Inflate(rect, -1, -1));
            }
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(170, 10, 10, 10), rect, format, 1.5f * s);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            using var brush = new SolidBrush(Color.FromArgb(170, Accent));
            g.FillRectangle(brush, thumb);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s)
        {
            using var pen = new Pen(Accent, 2 * s);
            g.DrawLine(pen, x, top, x, top + height);
        }
    }
}
