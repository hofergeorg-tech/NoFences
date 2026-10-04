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

        /// <summary>Extra space at the bottom (logical px) that is not part of the fence's surface, e.g. a drawn shadow.</summary>
        public virtual int BottomInset => 0;

        /// <summary>
        /// Clear margin (logical px) between the window edge and the visible surface. Resize handles sit
        /// on the surface edge, because fully transparent pixels don't receive mouse clicks.
        /// </summary>
        public virtual Padding SurfaceInsets => Padding.Empty;

        /// <summary>Whether <see cref="DrawHoverEffect"/> animates while the mouse is over the fence.</summary>
        public virtual bool AnimatesOnHover => false;

        /// <summary>Animated overlay while hovered; <paramref name="t"/> = seconds since the mouse entered.</summary>
        public virtual void DrawHoverEffect(Graphics g, Rectangle bounds, int titleHeight, float t, float s) { }

        /// <summary>Collapsed height on top of the title height (logical px), so margins don't eat the title.</summary>
        public virtual int CollapsedExtra => 0;

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

        /// <summary>Highlight color of the style, used by widgets for bars, the clock and "live" markers.</summary>
        public virtual Color Accent => Color.FromArgb(0, 150, 255);

        /// <summary>Frosted-glass blur behind the fence. Off = clear, so the theme can paint free shapes.</summary>
        public virtual bool Glass => true;

        /// <summary>Rectangular DWM drop shadow around the window (off when the theme draws its own).</summary>
        public virtual bool WindowShadow => true;

        /// <summary>Color of the "drop files here" hint in empty fences (dark for light styles).</summary>
        public virtual Color HintColor => Color.FromArgb(150, Color.White);

        /// <summary>Background and text color of the note editor (a normal, opaque text box).</summary>
        public virtual (Color Back, Color Fore) EditorColors => (Color.FromArgb(28, 30, 36), Color.FromArgb(235, 238, 245));

        /// <summary>Font for note text; by default the label font a bit larger.</summary>
        public virtual Font CreateNoteFont(float s)
        {
            using var label = CreateLabelFont(s);
            return new Font(label.FontFamily, label.Size * 1.2f, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        protected int Alpha(FenceInfo info) => Math.Clamp(Math.Max(info.BackgroundAlpha, MinAlpha), 0, 255);

        /// <summary>Stable per-fence randomness, so decorations don't jump between repaints.</summary>
        protected static Random Seeded(FenceInfo info) => new(info.Id.GetHashCode());

        protected static StringFormat TitleFormat(StringAlignment alignment = StringAlignment.Near) => new()
        {
            Alignment = alignment,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        protected static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected static void FillRounded(Graphics g, Color color, RectangleF r, float radius)
        {
            using var path = RoundedRect(r, radius);
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }

        protected static void DrawRounded(Graphics g, Color color, float width, RectangleF r, float radius)
        {
            using var path = RoundedRect(r, radius);
            using var pen = new Pen(color, width);
            g.DrawPath(pen, path);
        }

        protected static void DrawPlainString(Graphics g, string text, Font font, Color color, RectangleF rect, StringFormat format)
        {
            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, rect, format);
        }

        /// <summary>Simple thin rounded scrollbar thumb used by several styles.</summary>
        protected static void DrawPillThumb(Graphics g, Rectangle track, Rectangle thumb, Color color, float s)
        {
            var w = 4 * s;
            FillRounded(g, color, new RectangleF(track.X + (track.Width - w) / 2, thumb.Y, w, thumb.Height), w / 2);
        }

        protected static void DrawBarMarker(Graphics g, Color color, int x, int top, int height, float width)
        {
            using var pen = new Pen(color, width);
            g.DrawLine(pen, x, top, x, top + height);
        }

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
        private static readonly List<FenceTheme> BuiltIn = new FenceTheme[]
        {
            new DefaultTheme(),
            new WindowsAccentTheme(),
            new HighContrastTheme(),
            new StarCitizenTheme(),
            new RetroArcadeTheme(),
            new HardwareTheme(),
            new NerdTheme(),
            new HobbyTheme(),
            new WorkTheme(),
            new FamilyTheme(),
            new GamingTheme(),
            new FinanceTheme(),
            new SocialTheme(),
            new DocumentsTheme(),
            new MultimediaTheme(),
            new MusicTheme(),
            new SportTheme(),
            new PhotosTheme(),
            new TravelTheme(),
            new CookingTheme(),
            new NatureTheme()
        }.Concat(PostItTheme.AllColors()).ToList();

        public static IReadOnlyList<FenceTheme> All { get; private set; } = BuiltIn;

        /// <summary>Built-in styles plus user styles; a user style can't replace a built-in id.</summary>
        public static void SetCustom(IEnumerable<FenceTheme> custom) =>
            All = BuiltIn.Concat(custom.Where(c => BuiltIn.All(b => !b.Id.Equals(c.Id, StringComparison.OrdinalIgnoreCase)))).ToList();

        public enum Group { Basic, GamingTech, WorkLife, Leisure, PostIt, Own }

        /// <summary>The menu group of a style; user styles (JSON) go to "Own".</summary>
        public static Group GroupOf(FenceTheme theme) => theme.Id switch
        {
            "default" or "windows" or "contrast" => Group.Basic,
            "starcitizen" or "retroarcade" or "gaming" or "hardware" or "nerd" => Group.GamingTech,
            "work" or "finance" or "documents" or "social" or "family" => Group.WorkLife,
            "hobby" or "music" or "multimedia" or "sport" or "photos" or "travel" or "cooking" or "nature" => Group.Leisure,
            _ when theme.Id.StartsWith("postit", StringComparison.OrdinalIgnoreCase) => Group.PostIt,
            _ => Group.Own
        };

        public static FenceTheme Get(string? id) =>
            All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
    }
}
