using System.Drawing.Drawing2D;
using System.Drawing.Text;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>
    /// Everything that defines the look of a fence. The window does layout and input;
    /// the theme only paints. All sizes passed in are already in device pixels, <c>s</c> is the DPI scale.
    /// </summary>
    public abstract class FenceTheme
    {
        public abstract string Id { get; }

        public abstract string DisplayName { get; }

        /// <summary>DWM corner preference: 1 = square, 2 = round, 3 = small round.</summary>
        public virtual int CornerPreference => 2;

        /// <summary>Whether the theme uses the fence's own background color.</summary>
        public virtual bool UsesCustomColor => false;

        /// <summary>Lower bound for the background alpha, so the style stays recognizable.</summary>
        public virtual int MinAlpha => 0;

        /// <summary>Extra inner padding (logical px) so items don't overlap theme decorations.</summary>
        public virtual int ContentInset => 0;

        public abstract Font CreateTitleFont(int titleHeightPx);

        /// <summary>Label font in pixels; <paramref name="s"/> is the DPI scale.</summary>
        public abstract Font CreateLabelFont(float s);

        public virtual string FormatTitle(string title) => title;

        public abstract void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s);

        public abstract void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s);

        public abstract void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s);

        public abstract void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s);

        public abstract void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s);

        public abstract void DrawInsertMarker(Graphics g, int x, int top, int height, float s);

        protected int Alpha(FenceInfo info) => Math.Clamp(Math.Max(info.BackgroundAlpha, MinAlpha), 0, 255);

        protected static Font CreateFont(string[] families, float size, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Point)
        {
            using var installed = new InstalledFontCollection();
            foreach (var name in families)
            {
                if (installed.Families.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    return new Font(name, size, style, unit);
            }
            return new Font("Segoe UI", size, style, unit);
        }

        protected static void DrawShadowedString(Graphics g, string text, Font font, Color color, Color shadow, RectangleF rect, StringFormat format, float offset)
        {
            using (var shadowBrush = new SolidBrush(shadow))
            {
                var shadowRect = rect;
                shadowRect.Offset(offset, offset);
                g.DrawString(text, font, shadowBrush, shadowRect, format);
            }
            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, rect, format);
        }

        /// <summary>L-shaped corner brackets, the classic HUD frame.</summary>
        protected static void DrawCornerBrackets(Graphics g, RectangleF r, float len, Pen pen)
        {
            g.DrawLines(pen, new[] { new PointF(r.Left, r.Top + len), new PointF(r.Left, r.Top), new PointF(r.Left + len, r.Top) });
            g.DrawLines(pen, new[] { new PointF(r.Right - len, r.Top), new PointF(r.Right, r.Top), new PointF(r.Right, r.Top + len) });
            g.DrawLines(pen, new[] { new PointF(r.Right, r.Bottom - len), new PointF(r.Right, r.Bottom), new PointF(r.Right - len, r.Bottom) });
            g.DrawLines(pen, new[] { new PointF(r.Left + len, r.Bottom), new PointF(r.Left, r.Bottom), new PointF(r.Left, r.Bottom - len) });
        }

        protected static GraphicsPath ChamferedRect(RectangleF r, float cut)
        {
            var path = new GraphicsPath();
            path.AddPolygon(new[]
            {
                new PointF(r.Left, r.Top),
                new PointF(r.Right - cut, r.Top),
                new PointF(r.Right, r.Top + cut),
                new PointF(r.Right, r.Bottom),
                new PointF(r.Left + cut, r.Bottom),
                new PointF(r.Left, r.Bottom - cut),
            });
            return path;
        }
    }

    public static class ThemeRegistry
    {
        public static IReadOnlyList<FenceTheme> All { get; } = new FenceTheme[]
        {
            new DefaultTheme(),
            new StarCitizenTheme(),
            new RetroArcadeTheme()
        };

        public static FenceTheme Get(string? id) =>
            All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
    }
}
