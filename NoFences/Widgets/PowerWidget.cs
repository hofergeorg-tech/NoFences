using NoFences.Util;
using NoFences.Win32;

namespace NoFences.Widgets
{
    /// <summary>The Windows power plans; one click makes one active (e.g. High performance for games).</summary>
    public sealed class PowerWidget : FenceWidget
    {
        private List<PowerPlan> plans = new();
        private readonly List<(RectangleF Rect, PowerPlan Plan)> rows = new();

        public override string Type => "power";

        public override int RefreshMs => 5000;

        internal void SetPreview(List<PowerPlan> demo) => plans = demo;

        public override void Refresh()
        {
            if (!PreviewMode)
                plans = PowerPlans.All();
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (plans.Count == 0)
            {
                c.Text(Strings.PowerNone, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }
            float y = c.Area.Y;
            var rowHeight = line + c.Px(10);
            var dot = c.Px(12);
            using var bold = new Font(c.Label.FontFamily, c.Label.Size, FontStyle.Bold, c.Label.Unit);
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var plan in plans)
            {
                if (y + rowHeight > c.Area.Bottom + c.Px(4))
                    break;
                var rect = new RectangleF(c.Area.X, y, c.Area.Width, rowHeight - c.Px(4));
                if (plan.Active)
                {
                    using var back = new SolidBrush(Color.FromArgb(60, c.Theme.Accent));
                    c.G.FillRectangle(back, rect);
                }
                var dotRect = new RectangleF(rect.X + c.Px(6), rect.Y + (rect.Height - dot) / 2, dot, dot);
                using (var pen = new Pen(c.Theme.Accent, Math.Max(1.5f, 1.5f * c.S)))
                    c.G.DrawEllipse(pen, dotRect);
                if (plan.Active)
                {
                    using var fill = new SolidBrush(c.Theme.Accent);
                    c.G.FillEllipse(fill, RectangleF.Inflate(dotRect, -dot * 0.25f, -dot * 0.25f));
                }
                var x = dotRect.Right + c.Px(10);
                c.Text(plan.Name, new RectangleF(x, rect.Y, rect.Right - x, rect.Height), plan.Active ? bold : null, valign: StringAlignment.Center);
                rows.Add((rect, plan));
                y += rowHeight;
            }
        }

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p) && !r.Plan.Active);

        public override bool Click(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Rect.Contains(p));
            if (row.Plan == null || row.Plan.Active)
                return false;
            PowerPlans.SetActive(row.Plan.Id);
            Refresh();
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner) =>
            menu.Add(Strings.PowerSettings, null, (_, _) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:powersleep") { UseShellExecute = true }); } catch { }
            });
    }
}
