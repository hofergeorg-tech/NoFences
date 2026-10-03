using NoFences.Themes;

namespace NoFences.Widgets
{
    /// <summary>
    /// Live content of a widget fence. The fence window owns the timer and the theme; the widget only
    /// collects its data (<see cref="Refresh"/>) and draws it with the theme's colors (<see cref="Draw"/>).
    /// </summary>
    public abstract class FenceWidget : IDisposable
    {
        public abstract string Type { get; }

        /// <summary>
        /// Set by the preview renderer: widgets with online or personal data (games, calendar, news …)
        /// then never load anything real, so the images only ever show the demo content.
        /// </summary>
        public static bool PreviewMode { get; internal set; }

        /// <summary>How often <see cref="Refresh"/> runs, in milliseconds.</summary>
        public virtual int RefreshMs => 1000;

        /// <summary>Collects new data. Runs on the UI thread, so keep it quick.</summary>
        public virtual void Refresh() { }

        public abstract void Draw(WidgetCanvas c);

        /// <summary>Single click at a point inside the fence (client coordinates); true if handled.</summary>
        public virtual bool Click(Point p) => false;

        public virtual void DoubleClick(Point p) { }

        /// <summary>Whether the point is clickable (hand cursor).</summary>
        public virtual bool IsClickable(Point p) => false;

        /// <summary>Mouse wheel over the fence; true if handled (e.g. scrolled).</summary>
        public virtual bool Wheel(int delta) => false;

        /// <summary>Tooltip for the point, e.g. a game's name under its cover.</summary>
        public virtual string? TooltipAt(Point p) => null;

        /// <summary>Set by the fence: call to redraw now (e.g. after a download finished).</summary>
        public Action? Invalidated { get; set; }

        protected void RequestRedraw() => Invalidated?.Invoke();

        public virtual void AddMenuItems(ToolStripItemCollection items, IWin32Window owner) { }

        public virtual bool AcceptsDrop(IDataObject data) => false;

        public virtual void Drop(IDataObject data, IWin32Window owner) { }

        public virtual void Dispose() { }
    }

    /// <summary>Drawing helpers so every widget looks at home in every style.</summary>
    public sealed class WidgetCanvas
    {
        public required Graphics G { get; init; }
        public required Rectangle Area { get; init; }
        public required FenceTheme Theme { get; init; }
        public required Font Label { get; init; }
        public required Font Big { get; init; }
        public required float S { get; init; }

        public int Px(float v) => (int)Math.Round(v * S);

        /// <summary>Solid foreground for drawn shapes: light on dark styles, dark on light ones.</summary>
        public Color Ink => Color.FromArgb(235, Theme.HintColor);

        private static StringFormat Format(StringAlignment h, StringAlignment v) => new()
        {
            Alignment = h,
            LineAlignment = v,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        public void Text(string text, RectangleF rect, Font? font = null, StringAlignment align = StringAlignment.Near, StringAlignment valign = StringAlignment.Near)
        {
            using var format = Format(align, valign);
            Theme.DrawLabel(G, text, rect, font ?? Label, format, S);
        }

        /// <summary>
        /// Text that wraps onto at most <paramref name="maxLines"/> lines (ellipsis after the last word that
        /// fits); returns the height used.
        /// </summary>
        public float TextWrapped(string text, float x, float y, float width, Font font, int maxLines)
        {
            using var format = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit };
            var lineHeight = font.GetHeight(G);
            var needed = G.MeasureString(text, font, (int)width, format).Height;
            var height = Math.Min(needed, lineHeight * maxLines + 1);
            Theme.DrawLabel(G, text, new RectangleF(x, y, width, height), font, format, S);
            return height;
        }

        /// <summary>Small secondary text (dates, sources) in a dimmed color, without the style's effects.</summary>
        public float Muted(string text, float x, float y, Font font, Color? color = null)
        {
            using var brush = new SolidBrush(color ?? Color.FromArgb(Math.Max(170, (int)Theme.HintColor.A), Theme.HintColor));
            var oldHint = G.TextRenderingHint;
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            // Default format (with its small side padding) so it lines up with the labels above it
            using var format = new StringFormat();
            G.DrawString(text, font, brush, x, y, format);
            var width = G.MeasureString(text, font, PointF.Empty, format).Width - font.Size / 3;
            G.TextRenderingHint = oldHint;
            return width;
        }

        /// <summary>A font of the theme's note family at a pixel size (dispose it).</summary>
        public Font Sized(float px, FontStyle style = FontStyle.Regular) => new(Big.FontFamily, Math.Max(6, px), style, GraphicsUnit.Pixel);

        /// <summary>Horizontal bar filled to <paramref name="fraction"/> (0..1) in the accent color; red above 90 %.</summary>
        public void Bar(RectangleF rect, double fraction)
        {
            fraction = Math.Clamp(double.IsFinite(fraction) ? fraction : 0, 0, 1);
            using (var track = new SolidBrush(Color.FromArgb(55, Theme.HintColor)))
                G.FillRectangle(track, rect);
            var color = fraction > 0.9 ? Color.FromArgb(230, 255, 90, 70) : Color.FromArgb(230, Theme.Accent);
            using var fill = new SolidBrush(color);
            G.FillRectangle(fill, rect.X, rect.Y, (float)(rect.Width * fraction), rect.Height);
        }

        /// <summary>One labeled row: name left, value right, optional bar below. Advances <paramref name="y"/>.</summary>
        public RectangleF Row(ref float y, string name, string value, double? fraction = null)
        {
            var lineHeight = Label.GetHeight(G);
            var start = y;
            Text(name, new RectangleF(Area.X, y, Area.Width * 0.55f, lineHeight + 2));
            Text(value, new RectangleF(Area.X + Area.Width * 0.35f, y, Area.Width * 0.65f, lineHeight + 2), align: StringAlignment.Far);
            y += lineHeight + Px(2);
            if (fraction != null)
            {
                Bar(new RectangleF(Area.X, y, Area.Width, Px(5)), fraction.Value);
                y += Px(5);
            }
            y += Px(7);
            return new RectangleF(Area.X, start, Area.Width, y - start);
        }

        /// <summary>A small filled dot in the accent color (e.g. "live").</summary>
        public void Dot(float x, float y, float size)
        {
            using var brush = new SolidBrush(Theme.Accent);
            G.FillEllipse(brush, x, y, size, size);
        }
    }
}
