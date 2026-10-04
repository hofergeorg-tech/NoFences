using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Programs that start with Windows, each with a switch (like Task Manager's "Startup apps").
    /// Machine-wide entries are shown dimmed: switching them needs admin rights.
    /// </summary>
    public sealed class AutostartWidget : FenceWidget
    {
        private List<AutostartEntry> entries = new();
        private readonly List<(RectangleF Rect, AutostartEntry Entry)> rows = new();
        private int scroll;
        private int maxScroll;
        private DateTime nextRead;

        public override string Type => "autostart";

        public override int RefreshMs => 2000;

        internal void SetPreview(List<AutostartEntry> demo)
        {
            entries = demo;
            nextRead = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode || DateTime.Now < nextRead)
                return;
            nextRead = DateTime.Now.AddSeconds(10);
            entries = Autostart.ReadAll();
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            if (entries.Count == 0 && nextRead == default)
                Refresh();
            var line = c.Label.GetHeight(c.G) + c.Px(8);
            if (entries.Count == 0)
            {
                c.Text(Strings.AutostartNone, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }
            maxScroll = Math.Max(0, (int)(entries.Count * line - c.Area.Height));
            scroll = Math.Clamp(scroll, 0, maxScroll);
            var state = c.G.Save();
            c.G.SetClip(c.Area);
            float y = c.Area.Y - scroll;
            foreach (var entry in entries)
            {
                if (y + line >= c.Area.Y && y <= c.Area.Bottom)
                {
                    var switchRect = new RectangleF(c.Area.Right - c.Px(36), y + (line - c.Px(18)) / 2, c.Px(34), c.Px(18));
                    DrawSwitch(c, switchRect, entry.Enabled, entry.CanToggle);
                    c.Text(entry.Name, new RectangleF(c.Area.X, y + c.Px(4), c.Area.Width - c.Px(44), line));
                    rows.Add((new RectangleF(c.Area.X, y, c.Area.Width, line), entry));
                }
                y += line;
            }
            c.G.Restore(state);
        }

        private static void DrawSwitch(WidgetCanvas c, RectangleF r, bool on, bool enabled)
        {
            var alpha = enabled ? 230 : 90;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            var d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 90, 180);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 180);
            path.CloseFigure();
            using (var fill = new SolidBrush(on ? Color.FromArgb(alpha, c.Theme.Accent) : Color.FromArgb(alpha / 3, c.Theme.HintColor)))
                c.G.FillPath(fill, path);
            using (var knob = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                c.G.FillEllipse(knob, on ? r.Right - d + c.Px(2) : r.X + c.Px(2), r.Y + c.Px(2), d - c.Px(4), d - c.Px(4));
        }

        private AutostartEntry? At(Point p) => rows.FirstOrDefault(r => r.Rect.Contains(p)).Entry;

        public override bool IsClickable(Point p) => At(p) is { CanToggle: true };

        public override string? TooltipAt(Point p) => At(p) is { } e ? (e.CanToggle ? e.Command : $"{e.Command}\n{Strings.AutostartAdminOnly}") : null;

        public override bool Click(Point p)
        {
            if (At(p) is not { CanToggle: true } entry || PreviewMode)
                return false;
            if (Autostart.SetEnabled(entry, !entry.Enabled))
            {
                var i = entries.IndexOf(entry);
                if (i >= 0)
                    entries[i] = entry with { Enabled = !entry.Enabled };
            }
            return true;
        }

        public override bool Wheel(int delta)
        {
            if (maxScroll == 0 && scroll == 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 40, 0, maxScroll);
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection items, IWin32Window owner)
        {
            items.Add(Strings.AutostartOpenSettings, null, (_, _) => NoFencesApp.OpenUrl("ms-settings:startupapps"));
        }
    }
}
