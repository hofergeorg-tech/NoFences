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
