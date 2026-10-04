using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// CPU, RAM, GPU load and temperature with a two-minute graph, FPS when the FPS helper is enabled,
    /// and a notification when the graphics card stays too hot.
    /// </summary>
    public sealed class SystemWidget : FenceWidget
    {
        public const int HistoryLength = 120;
        public static readonly int[] WarnChoices = { 0, 75, 80, 85, 90 };
        private const int DefaultWarnAt = 85;

        private readonly SystemStats stats = new();
        private readonly Func<bool> fpsEnabled;
        private readonly Action toggleFps;
        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly Action<string> notify;
        private readonly Queue<(double Cpu, double? Gpu)> history = new();
        private readonly TemperatureWatch watch = new();
        private FpsReading? fps;

        public SystemWidget(Func<bool> fpsEnabled, Action toggleFps, Func<string?> getOption, Action<string?> setOption, Action<string> notify)
        {
            this.fpsEnabled = fpsEnabled;
            this.toggleFps = toggleFps;
            this.getOption = getOption;
            this.setOption = setOption;
            this.notify = notify;
        }

        public override string Type => "system";

        public override int RefreshMs => 1000;

        /// <summary>GPU temperature that triggers the warning (0 = never).</summary>
        private int WarnAt => int.TryParse(getOption(), out var t) ? t : DefaultWarnAt;

        internal void SetPreview(IEnumerable<(double Cpu, double? Gpu)> samples)
        {
            history.Clear();
            foreach (var s in samples)
                history.Enqueue(s);
        }

        public override void Refresh()
        {
            stats.Sample();
            fps = fpsEnabled() ? FpsReading.TryRead() : null;
            if (PreviewMode)
                return;
            history.Enqueue((stats.CpuLoad, stats.GpuLoad));
            while (history.Count > HistoryLength)
                history.Dequeue();
            if (watch.Update(stats.GpuTemperature, WarnAt, DateTime.Now) is int hot)
                notify(Strings.TemperatureWarning(hot));
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

            // The last two minutes, if there's room: CPU in the accent color, GPU as a thin line
            var graphHeight = c.Area.Bottom - y;
            if (graphHeight >= c.Px(36) && history.Count > 1)
                DrawGraph(c, new RectangleF(c.Area.X, y, c.Area.Width, Math.Min(graphHeight, c.Px(90))));
        }

        private void DrawGraph(WidgetCanvas c, RectangleF rect)
        {
            using (var track = new SolidBrush(Color.FromArgb(35, c.Theme.HintColor)))
                c.G.FillRectangle(track, rect);
            var samples = history.ToArray();
            var step = rect.Width / (HistoryLength - 1);
            var start = rect.Right - step * (samples.Length - 1);
            PointF At(int i, double v) => new(start + i * step, rect.Bottom - (float)Math.Clamp(v, 0, 1) * (rect.Height - c.Px(2)));

            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var cpu = samples.Select((s, i) => At(i, s.Cpu)).ToList();
            var area = new List<PointF>(cpu) { new(cpu[^1].X, rect.Bottom), new(cpu[0].X, rect.Bottom) };
            using (var fill = new SolidBrush(Color.FromArgb(70, c.Theme.Accent)))
                c.G.FillPolygon(fill, area.ToArray());
            using (var pen = new Pen(Color.FromArgb(230, c.Theme.Accent), Math.Max(1, 1.5f * c.S)))
                c.G.DrawLines(pen, cpu.ToArray());
            if (samples.All(s => s.Gpu != null))
            {
                using var pen = new Pen(Color.FromArgb(200, c.Theme.HintColor), Math.Max(1, c.S));
                c.G.DrawLines(pen, samples.Select((s, i) => At(i, s.Gpu!.Value)).ToArray());
            }
            c.G.SmoothingMode = oldMode;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(new ToolStripMenuItem(Strings.FpsMenu, null, (_, _) => toggleFps()) { Checked = fpsEnabled() });
            var warn = new ToolStripMenuItem(Strings.TemperatureWarnMenu);
            foreach (var t in WarnChoices)
            {
                var value = t;
                warn.DropDownItems.Add(new ToolStripMenuItem(t == 0 ? Strings.HotkeyName("Off") : $"{t} °C", null, (_, _) => setOption(value.ToString())) { Checked = WarnAt == t });
            }
            menu.Add(warn);
        }

        public override void Dispose() => stats.Dispose();
    }

    /// <summary>
    /// Warns once when the temperature stays at or above the limit for <see cref="HotFor"/>, then not
    /// again for <see cref="Quiet"/> (no flood of notifications during a long gaming session).
    /// </summary>
    public sealed class TemperatureWatch
    {
        public static readonly TimeSpan HotFor = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan Quiet = TimeSpan.FromMinutes(15);

        private DateTime? hotSince;
        private DateTime lastWarning = DateTime.MinValue;

        /// <summary>The temperature to warn about now, or null.</summary>
        public int? Update(int? temperature, int limit, DateTime now)
        {
            if (limit <= 0 || temperature is not int t || t < limit)
            {
                hotSince = null;
                return null;
            }
            hotSince ??= now;
            if (now - hotSince < HotFor || now - lastWarning < Quiet)
                return null;
            lastWarning = now;
            return t;
        }
    }
}
