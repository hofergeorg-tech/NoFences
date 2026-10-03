using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>CPU, RAM, GPU load and temperature, and FPS when the FPS helper is enabled.</summary>
    public sealed class SystemWidget : FenceWidget
    {
        private readonly SystemStats stats = new();
        private readonly Func<bool> fpsEnabled;
        private readonly Action toggleFps;
        private FpsReading? fps;

        public SystemWidget(Func<bool> fpsEnabled, Action toggleFps)
        {
            this.fpsEnabled = fpsEnabled;
            this.toggleFps = toggleFps;
        }

        public override string Type => "system";

        public override int RefreshMs => 1000;

        public override void Refresh()
        {
            stats.Sample();
            fps = fpsEnabled() ? FpsReading.TryRead() : null;
        }

        public override void Draw(WidgetCanvas c)
        {
            float y = c.Area.Y;
            if (fpsEnabled())
            {
                // FPS first and big: it's what you look at while playing
                using var big = c.Sized(Math.Min(c.Area.Height * 0.22f, c.Px(40)), FontStyle.Bold);
                var h = big.GetHeight(c.G);
                c.Text(fps != null ? $"{fps.Fps:0} FPS" : "– FPS", new RectangleF(c.Area.X, y, c.Area.Width, h), big);
                y += h;
                var line = c.Label.GetHeight(c.G) + c.Px(2);
                c.Text(fps != null ? fps.Process : Strings.FpsWaiting, new RectangleF(c.Area.X, y, c.Area.Width, line));
                y += line + c.Px(8);
            }

            c.Row(ref y, "CPU", $"{stats.CpuLoad * 100:0} %", stats.CpuLoad);
            c.Row(ref y, "RAM", $"{DrivesWidget.FormatSize((long)stats.RamUsedBytes)} / {DrivesWidget.FormatSize((long)stats.RamTotalBytes)}", stats.RamUsed);
            if (stats.GpuLoad is double gpu)
                c.Row(ref y, "GPU", $"{gpu * 100:0} %", gpu);
            if (stats.GpuTemperature is int temp)
                c.Row(ref y, Strings.GpuTemperature, $"{temp} °C", temp / 100.0);
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(new ToolStripMenuItem(Strings.FpsMenu, null, (_, _) => toggleFps()) { Checked = fpsEnabled() });
        }

        public override void Dispose() => stats.Dispose();
    }
}
