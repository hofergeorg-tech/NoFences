namespace NoFences.Model
{
    public enum DockEdge { Left, Right, Top, Bottom }

    /// <summary>
    /// A fence group docked to a screen edge: a sidebar (left/right) or a bar (top/bottom). Its fences
    /// are stacked along the edge. Either it hides itself and slides in when the mouse touches the edge,
    /// or it reserves its space like the taskbar (maximized windows end next to it).
    /// </summary>
    public sealed class DockBar
    {
        public string Group { get; set; } = "";

        public DockEdge Edge { get; set; } = DockEdge.Right;

        /// <summary>Monitor (device name like \\.\DISPLAY2); null = the main monitor.</summary>
        public string? Screen { get; set; }

        /// <summary>Hidden until the mouse touches the edge (otherwise the space is reserved).</summary>
        public bool AutoHide { get; set; } = true;

        /// <summary>Width of a sidebar or height of a top/bottom bar, in logical (96 dpi) pixels.</summary>
        public int Thickness { get; set; } = 320;

        /// <summary>Style of the bar's background (e.g. "jause" = a wooden table); null = no background.</summary>
        public string? Theme { get; set; }

        /// <summary>Order of the fences along the bar (fence ids); others follow.</summary>
        public List<Guid> Order { get; set; } = new();

        /// <summary>Where each fence was before it was docked ([x, y, width, height]), to put it back.</summary>
        public Dictionary<Guid, int[]> Saved { get; set; } = new();

        public const int MinThickness = 120;
        public const int MaxThickness = 900;
    }

    /// <summary>Geometry of docked bars (pure, for tests).</summary>
    public static class DockLayout
    {
        public static bool Vertical(DockEdge edge) => edge is DockEdge.Left or DockEdge.Right;

        /// <summary>The bar's strip inside <paramref name="area"/> (usually the monitor's work area).</summary>
        public static Rectangle Strip(Rectangle area, DockEdge edge, int thickness)
        {
            thickness = Math.Min(thickness, Vertical(edge) ? area.Width : area.Height);
            return edge switch
            {
                DockEdge.Left => new Rectangle(area.Left, area.Top, thickness, area.Height),
                DockEdge.Right => new Rectangle(area.Right - thickness, area.Top, thickness, area.Height),
                DockEdge.Top => new Rectangle(area.Left, area.Top, area.Width, thickness),
                _ => new Rectangle(area.Left, area.Bottom - thickness, area.Width, thickness)
            };
        }

        /// <summary>
        /// Places fences one after another along the strip, each keeping its own length (height in a
        /// sidebar, width in a top/bottom bar) but at most the strip's length.
        /// </summary>
        public static List<Rectangle> Arrange(Rectangle strip, DockEdge edge, IReadOnlyList<int> lengths, int gap)
        {
            var result = new List<Rectangle>();
            var vertical = Vertical(edge);
            var pos = (vertical ? strip.Top : strip.Left) + gap;
            var max = Math.Max(16, (vertical ? strip.Height : strip.Width) - 2 * gap);
            foreach (var wanted in lengths)
            {
                var length = Math.Clamp(wanted, 16, max);
                result.Add(vertical
                    ? new Rectangle(strip.Left, pos, strip.Width, length)
                    : new Rectangle(pos, strip.Top, length, strip.Height));
                pos += length + gap;
            }
            return result;
        }

        /// <summary>The thin band along the monitor's edge that brings a hidden bar in.</summary>
        public static Rectangle Trigger(Rectangle screen, DockEdge edge, int depth = 2) => edge switch
        {
            DockEdge.Left => new Rectangle(screen.Left, screen.Top, depth, screen.Height),
            DockEdge.Right => new Rectangle(screen.Right - depth, screen.Top, depth, screen.Height),
            DockEdge.Top => new Rectangle(screen.Left, screen.Top, screen.Width, depth),
            _ => new Rectangle(screen.Left, screen.Bottom - depth, screen.Width, depth)
        };

        /// <summary>The fences in the order they are now along the bar (after one was dragged to another place).</summary>
        public static List<Guid> OrderByPosition(IEnumerable<(Guid Id, Rectangle Bounds)> fences, DockEdge edge) =>
            fences.OrderBy(f => Vertical(edge) ? f.Bounds.Top + f.Bounds.Height / 2 : f.Bounds.Left + f.Bounds.Width / 2)
                .Select(f => f.Id).ToList();

        /// <summary>Fences of the group in the bar's order: known ones first, new ones after them.</summary>
        public static List<T> InOrder<T>(IEnumerable<T> fences, Func<T, Guid> id, IReadOnlyList<Guid> order)
        {
            var list = fences.ToList();
            var known = order.ToList();
            return list.OrderBy(f => known.IndexOf(id(f)) is var i && i >= 0 ? i : int.MaxValue)
                .ThenBy(f => list.IndexOf(f)).ToList();
        }
    }
}
