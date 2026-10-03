using NoFences.Util;
using NoFences.Win32;

namespace NoFences.Widgets
{
    /// <summary>
    /// Volume of the current playback device (click or scroll on the bar), mute for speakers and
    /// microphone, and one click to switch between playback devices (headset ↔ speakers).
    /// </summary>
    public sealed class AudioWidget : FenceWidget
    {
        private List<AudioDevice> devices = new();
        private float volume;
        private bool muted;
        private bool? micMuted;
        private DateTime nextDevices;
        private RectangleF bar, muteButton, micButton;
        private readonly List<(RectangleF Rect, AudioDevice Device)> deviceRows = new();

        public override string Type => "audio";

        public override int RefreshMs => 1000;

        internal void SetPreview(List<AudioDevice> demo, float demoVolume, bool demoMic)
        {
            devices = demo;
            volume = demoVolume;
            micMuted = demoMic;
            nextDevices = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            volume = CoreAudio.Volume;
            muted = CoreAudio.Muted;
            micMuted = CoreAudio.MicMuted;
            // Device list changes rarely (headset plugged in); no need to ask every second
            if (DateTime.UtcNow >= nextDevices)
            {
                devices = CoreAudio.Outputs();
                nextDevices = DateTime.UtcNow.AddSeconds(5);
            }
        }

        /// <summary>"Kopfhörer (Logitech G Pro)" → shortened for the small rows, keeps the useful part.</summary>
        public static string ShortName(string name) => name.Length <= 38 ? name : name[..37].TrimEnd() + "…";

        public override void Draw(WidgetCanvas c)
        {
            deviceRows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            float y = c.Area.Y;
            var current = devices.FirstOrDefault(d => d.IsDefault);
            if (current == null && devices.Count == 0)
            {
                c.Text(Strings.AudioNone, new RectangleF(c.Area.X, y, c.Area.Width, line));
                return;
            }

            using (var bold = new Font(c.Label.FontFamily, c.Label.Size, FontStyle.Bold, c.Label.Unit))
                c.Text(ShortName(current?.Name ?? "?"), new RectangleF(c.Area.X, y, c.Area.Width, line), bold);
            y += line + c.Px(4);

            // Volume: big percentage, then a thick bar you can click
            using var big = c.Sized(Math.Min(c.Area.Height * 0.16f, c.Px(28)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            c.Text(muted ? Strings.AudioMuted : $"{volume * 100:0} %", new RectangleF(c.Area.X, y, c.Area.Width * 0.6f, bigHeight), big);

            // Mute buttons on the right of the number
            var size = Math.Min(bigHeight, c.Px(30));
            micButton = new RectangleF(c.Area.Right - size, y + (bigHeight - size) / 2, size, size);
            muteButton = new RectangleF(micButton.X - size - c.Px(6), micButton.Y, size, size);
            DrawToggle(c, muteButton, DrawSpeaker, muted);
            if (micMuted != null)
                DrawToggle(c, micButton, DrawMic, micMuted == true);
            else
                micButton = RectangleF.Empty;
            y += bigHeight + c.Px(4);

            bar = new RectangleF(c.Area.X, y, c.Area.Width, c.Px(10));
            c.Bar(bar, muted ? 0 : volume);
            y += bar.Height + c.Px(12);

            // Other devices: one click makes them the default
            foreach (var device in devices.Where(d => !d.IsDefault))
            {
                if (y + line > c.Area.Bottom)
                    break;
                var rect = new RectangleF(c.Area.X, y, c.Area.Width, line + c.Px(4));
                c.Text("↪ " + ShortName(device.Name), new RectangleF(rect.X, rect.Y + c.Px(2), rect.Width, line));
                deviceRows.Add((rect, device));
                y += rect.Height;
            }
        }

        /// <summary>A round button with a drawn icon; red and crossed out when muted.</summary>
        private static void DrawToggle(WidgetCanvas c, RectangleF r, Action<Graphics, RectangleF, Pen, Brush> icon, bool muted)
        {
            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var back = new SolidBrush(muted ? Color.FromArgb(220, 220, 60, 60) : Color.FromArgb(70, c.Theme.HintColor)))
                c.G.FillEllipse(back, r);
            using var pen = new Pen(Color.White, Math.Max(1.4f, r.Height / 16)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            using var brush = new SolidBrush(Color.White);
            icon(c.G, RectangleF.Inflate(r, -r.Width * 0.24f, -r.Height * 0.24f), pen, brush);
            if (muted)
                c.G.DrawLine(pen, r.X + r.Width * 0.24f, r.Bottom - r.Height * 0.24f, r.Right - r.Width * 0.24f, r.Y + r.Height * 0.24f);
            c.G.SmoothingMode = oldMode;
        }

        private static void DrawSpeaker(Graphics g, RectangleF r, Pen pen, Brush brush)
        {
            var w = r.Width;
            g.FillPolygon(brush, new[]
            {
                new PointF(r.X, r.Y + w * 0.35f), new PointF(r.X + w * 0.25f, r.Y + w * 0.35f), new PointF(r.X + w * 0.55f, r.Y + w * 0.08f),
                new PointF(r.X + w * 0.55f, r.Y + w * 0.92f), new PointF(r.X + w * 0.25f, r.Y + w * 0.65f), new PointF(r.X, r.Y + w * 0.65f)
            });
            g.DrawArc(pen, r.X + w * 0.45f, r.Y + w * 0.3f, w * 0.3f, w * 0.4f, -50, 100);
            g.DrawArc(pen, r.X + w * 0.45f, r.Y + w * 0.12f, w * 0.5f, w * 0.76f, -50, 100);
        }

        private static void DrawMic(Graphics g, RectangleF r, Pen pen, Brush brush)
        {
            var w = r.Width;
            using var body = new System.Drawing.Drawing2D.GraphicsPath();
            var cap = new RectangleF(r.X + w * 0.32f, r.Y, w * 0.36f, w * 0.62f);
            body.AddArc(cap.X, cap.Y, cap.Width, cap.Width, 180, 180);
            body.AddArc(cap.X, cap.Bottom - cap.Width, cap.Width, cap.Width, 0, 180);
            body.CloseFigure();
            g.FillPath(brush, body);
            g.DrawArc(pen, r.X + w * 0.18f, r.Y + w * 0.2f, w * 0.64f, w * 0.58f, 0, 180);
            g.DrawLine(pen, r.X + w * 0.5f, r.Y + w * 0.78f, r.X + w * 0.5f, r.Y + w * 0.98f);
        }

        public override bool IsClickable(Point p) =>
            bar.Contains(p) || muteButton.Contains(p) || micButton.Contains(p) || deviceRows.Any(r => r.Rect.Contains(p));

        public override string? TooltipAt(Point p) =>
            muteButton.Contains(p) ? Strings.AudioMuteTip
            : micButton.Contains(p) ? Strings.AudioMicTip
            : deviceRows.FirstOrDefault(r => r.Rect.Contains(p)).Device?.Name;

        public override bool Click(Point p)
        {
            if (RectangleF.Inflate(bar, 0, 6).Contains(p) && bar.Width > 0)
            {
                CoreAudio.Volume = volume = (float)Math.Clamp((p.X - bar.X) / bar.Width, 0, 1);
                if (muted)
                    CoreAudio.Muted = muted = false;
                return true;
            }
            if (muteButton.Contains(p))
            {
                CoreAudio.Muted = muted = !muted;
                return true;
            }
            if (micButton.Contains(p) && micMuted != null)
            {
                CoreAudio.MicMuted = micMuted = !micMuted;
                return true;
            }
            var row = deviceRows.FirstOrDefault(r => r.Rect.Contains(p));
            if (row.Device == null)
                return false;
            CoreAudio.SetDefault(row.Device.Id);
            nextDevices = DateTime.MinValue;
            Refresh();
            return true;
        }

        /// <summary>The wheel anywhere on the widget changes the volume in 2 % steps.</summary>
        public override bool Wheel(int delta)
        {
            CoreAudio.Volume = volume = Math.Clamp(volume + Math.Sign(delta) * 0.02f, 0, 1);
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner) =>
            menu.Add(Strings.AudioSettings, null, (_, _) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:sound") { UseShellExecute = true }); } catch { }
            });
    }
}
