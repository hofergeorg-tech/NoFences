using System.Net.NetworkInformation;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Download and upload rate with a one-minute graph, plus the ping to 1.1.1.1.</summary>
    public sealed class NetworkWidget : FenceWidget
    {
        private const int HistoryLength = 60;
        private const string PingTarget = "1.1.1.1";

        private readonly Queue<(double Down, double Up)> history = new();
        private long lastReceived = -1, lastSent;
        private DateTime lastSample;
        private double down, up;
        private long? pingMs;
        private DateTime nextPing;
        private bool pinging;
        private SpeedTest.Result? speed;
        private string? speedPhase;
        private RectangleF speedRow;

        public override string Type => "network";

        public override int RefreshMs => 1000;

        public override void Refresh()
        {
            var (received, sent) = TotalBytes();
            var now = DateTime.UtcNow;
            if (lastReceived >= 0)
            {
                var seconds = Math.Max(0.1, (now - lastSample).TotalSeconds);
                // Counters reset when an adapter goes away; never show negative rates
                down = Math.Max(0, (received - lastReceived) / seconds);
                up = Math.Max(0, (sent - lastSent) / seconds);
                history.Enqueue((down, up));
                while (history.Count > HistoryLength)
                    history.Dequeue();
            }
            lastReceived = received;
            lastSent = sent;
            lastSample = now;

            if (now >= nextPing && !pinging)
            {
                nextPing = now.AddSeconds(5);
                _ = PingAsync();
            }
        }

        private async Task PingAsync()
        {
            pinging = true;
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(PingTarget, 2000);
                pingMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
            }
            catch (Exception)
            {
                pingMs = null;
            }
            finally
            {
                pinging = false;
            }
        }

        private static (long Received, long Sent) TotalBytes()
        {
            long received = 0, sent = 0;
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;
                try
                {
                    var stats = nic.GetIPStatistics();
                    received += stats.BytesReceived;
                    sent += stats.BytesSent;
                }
                catch (NetworkInformationException) { }
            }
            return (received, sent);
        }

        public static string FormatRate(double bytesPerSecond) => DrivesWidget.FormatSize((long)bytesPerSecond) + "/s";

        public override void Draw(WidgetCanvas c)
        {
            float y = c.Area.Y;
            using var big = c.Sized(Math.Min(c.Area.Height * 0.16f, c.Px(28)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            c.Text($"↓ {FormatRate(down)}", new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big);
            y += bigHeight;
            c.Text($"↑ {FormatRate(up)}", new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big);
            y += bigHeight + c.Px(6);

            // Room below for two rows: ping and speed test
            var graphHeight = Math.Max(0, c.Area.Bottom - y - 2 * (c.Label.GetHeight(c.G) + c.Px(9)) - c.Px(4));
            if (graphHeight > c.Px(20))
            {
                DrawGraph(c, new RectangleF(c.Area.X, y, c.Area.Width, graphHeight));
                y += graphHeight + c.Px(8);
            }
            c.Row(ref y, "Ping", pingMs is long ms ? $"{ms} ms" : "–");
            // Speed test: last result, progress, or the invitation to start one
            var text = speedPhase != null ? Strings.SpeedTestRunning(speedPhase)
                : speed != null ? $"↓ {speed.DownMbit:0} · ↑ {speed.UpMbit:0} Mbit/s"
                : Strings.SpeedTestStart;
            speedRow = c.Row(ref y, Strings.SpeedTest, text);
            if (speedRow.Bottom > c.Area.Bottom + c.Px(4))
                speedRow = RectangleF.Empty; // no room: only via the menu
        }

        internal void SetPreview(SpeedTest.Result result) => speed = result;

        private async void StartSpeedTest()
        {
            if (speedPhase != null || PreviewMode)
                return;
            speedPhase = "…";
            RequestRedraw();
            try
            {
                speed = await SpeedTest.RunAsync(new Progress<string>(p =>
                {
                    speedPhase = p;
                    RequestRedraw();
                }));
            }
            catch (Exception e)
            {
                Log.Write("Speed test", Log.Describe(e));
                speed = null;
            }
            finally
            {
                speedPhase = null;
                RequestRedraw();
            }
        }

        public override bool IsClickable(Point p) => speedRow.Contains(p) && speedPhase == null;

        public override bool Click(Point p)
        {
            if (!speedRow.Contains(p))
                return false;
            StartSpeedTest();
            return true;
        }

        public override string? TooltipAt(Point p) => speedRow.Contains(p) ? Strings.SpeedTestHint : null;

        public override void AddMenuItems(ToolStripItemCollection items, IWin32Window owner) =>
            items.Add(new ToolStripMenuItem(Strings.SpeedTestStart, null, (_, _) => StartSpeedTest()) { Enabled = speedPhase == null });

        private void DrawGraph(WidgetCanvas c, RectangleF rect)
        {
            using (var track = new SolidBrush(Color.FromArgb(35, c.Theme.HintColor)))
                c.G.FillRectangle(track, rect);
            if (history.Count < 2)
                return;
            var samples = history.ToArray();
            // Scale to the busiest second in view, at least 64 KB/s so an idle line stays flat
            var max = Math.Max(64 * 1024, samples.Max(s => Math.Max(s.Down, s.Up)));
            var step = rect.Width / (HistoryLength - 1);
            var start = rect.Right - step * (samples.Length - 1);
            PointF At(int i, double v) => new(start + i * step, rect.Bottom - (float)(v / max) * (rect.Height - c.Px(2)));

            var downPoints = samples.Select((s, i) => At(i, s.Down)).ToList();
            var area = new List<PointF>(downPoints) { new(downPoints[^1].X, rect.Bottom), new(downPoints[0].X, rect.Bottom) };
            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var fill = new SolidBrush(Color.FromArgb(70, c.Theme.Accent)))
                c.G.FillPolygon(fill, area.ToArray());
            using (var pen = new Pen(Color.FromArgb(230, c.Theme.Accent), Math.Max(1, 1.5f * c.S)))
                c.G.DrawLines(pen, downPoints.ToArray());
            using (var pen = new Pen(Color.FromArgb(200, c.Theme.HintColor), Math.Max(1, c.S)))
                c.G.DrawLines(pen, samples.Select((s, i) => At(i, s.Up)).ToArray());
            c.G.SmoothingMode = oldMode;
        }
    }
}
