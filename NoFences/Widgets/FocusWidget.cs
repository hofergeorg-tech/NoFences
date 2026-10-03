using System.Drawing.Drawing2D;
using System.Media;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Pomodoro focus timer: work, short break, and after four rounds a long break. A notification and a
    /// sound mark each change; start/pause and reset with the buttons.
    /// </summary>
    public sealed class FocusWidget : FenceWidget
    {
        public enum Phase { Focus, ShortBreak, LongBreak }

        /// <summary>Focus / short break / long break in minutes.</summary>
        public static readonly (int Focus, int Short, int Long)[] Presets = { (25, 5, 15), (50, 10, 20), (15, 3, 10) };

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly Action<string> notify;
        private readonly IFenceHost? host;
        private Phase phase = Phase.Focus;
        private int round = 1;
        private bool running;
        private TimeSpan left;
        private DateTime lastTick;
        private RectangleF startButton, resetButton;

        /// <summary>Focus mode switched the profile; this one comes back in breaks and when stopped.</summary>
        private bool profileSwitched;
        private string? profileBefore;

        public FocusWidget(Func<string?> getOption, Action<string?> setOption, Action<string> notify, IFenceHost? host = null)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            this.notify = notify;
            this.host = host;
            left = Length(Phase.Focus);
        }

        public override string Type => "focus";

        public override int RefreshMs => 500;

        /// <summary>Stored as "presetIndex|profile" (the profile part optional).</summary>
        public static (int Preset, string? Profile) ParseOption(string? option)
        {
            var parts = (option ?? "").Split('|', 2);
            var preset = int.TryParse(parts[0], out var i) && i >= 0 && i < Presets.Length ? i : 0;
            return (preset, parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null);
        }

        public static string FormatOption(int preset, string? profile) => profile == null ? preset.ToString() : $"{preset}|{profile}";

        private (int Focus, int Short, int Long) Timing => Presets[ParseOption(getOption()).Preset];

        private string? FocusProfile => ParseOption(getOption()).Profile;

        /// <summary>Focus mode: the chosen profile while a focus round runs, the previous one otherwise.</summary>
        private void UpdateFocusProfile()
        {
            if (host == null)
                return;
            var wanted = running && phase == Phase.Focus ? FocusProfile : null;
            if (wanted != null && host.Profiles.Contains(wanted))
            {
                if (!profileSwitched)
                {
                    profileBefore = host.ActiveProfile;
                    profileSwitched = true;
                }
                if (host.ActiveProfile != wanted)
                    host.SwitchProfile(wanted, automatic: true);
            }
            else if (profileSwitched)
            {
                profileSwitched = false;
                if (host.ActiveProfile != profileBefore)
                    host.SwitchProfile(profileBefore, automatic: true);
            }
        }

        private TimeSpan Length(Phase p) => TimeSpan.FromMinutes(p switch
        {
            Phase.Focus => Timing.Focus,
            Phase.ShortBreak => Timing.Short,
            _ => Timing.Long
        });

        /// <summary>The phase after <paramref name="current"/>; a long break after every fourth focus round.</summary>
        public static (Phase Phase, int Round) After(Phase current, int round) => current switch
        {
            Phase.Focus => (round % 4 == 0 ? Phase.LongBreak : Phase.ShortBreak, round),
            _ => (Phase.Focus, current == Phase.LongBreak ? 1 : round + 1)
        };

        internal void SetPreview(TimeSpan remaining, bool isRunning, int previewRound)
        {
            left = remaining;
            running = isRunning;
            round = previewRound;
            lastTick = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            var now = DateTime.UtcNow;
            if (running && lastTick != DateTime.MaxValue)
            {
                left -= now - lastTick;
                if (left <= TimeSpan.Zero)
                {
                    (phase, round) = After(phase, round);
                    left = Length(phase);
                    SystemSounds.Asterisk.Play();
                    notify(phase == Phase.Focus ? Strings.FocusBackToWork : Strings.FocusBreak(Length(phase).Minutes));
                    UpdateFocusProfile();
                }
            }
            if (lastTick != DateTime.MaxValue)
                lastTick = now;
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var buttonHeight = c.Px(26);
            var size = Math.Min(c.Area.Width, c.Area.Height - buttonHeight - line - c.Px(10));
            var ring = new RectangleF(c.Area.X + (c.Area.Width - size) / 2, c.Area.Y, size, size);
            var stroke = Math.Max(3, size / 14);
            ring.Inflate(-stroke / 2, -stroke / 2);

            var total = Length(phase).TotalSeconds;
            var done = total <= 0 ? 0 : 1 - left.TotalSeconds / total;
            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = SmoothingMode.AntiAlias;
            using (var track = new Pen(Color.FromArgb(60, c.Theme.HintColor), stroke))
                c.G.DrawEllipse(track, ring);
            var color = phase == Phase.Focus ? c.Theme.Accent : Color.FromArgb(255, 80, 200, 120);
            using (var arc = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (done > 0.002)
                    c.G.DrawArc(arc, ring, -90, (float)(360 * Math.Clamp(done, 0, 1)));
            }
            c.G.SmoothingMode = oldMode;

            using var big = c.Sized(size * 0.24f, FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            var mid = ring.Y + ring.Height / 2;
            c.Text($"{(int)left.TotalMinutes:00}:{left.Seconds:00}", new RectangleF(ring.X, mid - bigHeight * 0.65f, ring.Width, bigHeight), big, StringAlignment.Center);
            var label = phase switch
            {
                Phase.Focus => Strings.FocusRound(round),
                Phase.ShortBreak => Strings.FocusShortBreak,
                _ => Strings.FocusLongBreak
            };
            c.Text(label, new RectangleF(ring.X, mid + bigHeight * 0.35f, ring.Width, line), align: StringAlignment.Center);

            // Buttons: start/pause and reset
            var y = Math.Min(c.Area.Bottom - buttonHeight, ring.Bottom + stroke + c.Px(8));
            var width = Math.Min(c.Px(96), c.Area.Width / 2f - c.Px(6));
            startButton = new RectangleF(c.Area.X + c.Area.Width / 2f - width - c.Px(4), y, width, buttonHeight);
            resetButton = new RectangleF(c.Area.X + c.Area.Width / 2f + c.Px(4), y, width, buttonHeight);
            DrawButton(c, startButton, running ? Strings.FocusPause : Strings.FocusStart, true);
            DrawButton(c, resetButton, Strings.FocusReset, false);
        }

        private static void DrawButton(WidgetCanvas c, RectangleF r, string text, bool primary)
        {
            using (var brush = new SolidBrush(primary ? Color.FromArgb(200, c.Theme.Accent) : Color.FromArgb(60, c.Theme.HintColor)))
                c.G.FillRectangle(brush, r);
            c.Text(text, r, valign: StringAlignment.Center, align: StringAlignment.Center);
        }

        public override bool IsClickable(Point p) => startButton.Contains(p) || resetButton.Contains(p);

        public override bool Click(Point p)
        {
            if (startButton.Contains(p))
            {
                running = !running;
                lastTick = DateTime.UtcNow;
                UpdateFocusProfile();
                return true;
            }
            if (resetButton.Contains(p))
            {
                running = false;
                phase = Phase.Focus;
                round = 1;
                left = Length(Phase.Focus);
                UpdateFocusProfile();
                return true;
            }
            return false;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            var (preset, profile) = ParseOption(getOption());
            var timing = new ToolStripMenuItem(Strings.FocusTiming);
            for (var i = 0; i < Presets.Length; i++)
            {
                var index = i;
                var (f, s, l) = Presets[i];
                timing.DropDownItems.Add(new ToolStripMenuItem(Strings.FocusPreset(f, s, l), null, (_, _) =>
                {
                    setOption(FormatOption(index, profile));
                    if (!running)
                        left = Length(phase);
                    RequestRedraw();
                }) { Checked = preset == i });
            }
            menu.Add(timing);

            // Focus mode: switch to a profile (e.g. "Focus" with only work fences) while concentrating
            if (host != null)
            {
                var mode = new ToolStripMenuItem(Strings.FocusProfileMenu);
                mode.DropDownItems.Add(new ToolStripMenuItem(Strings.FocusProfileNone, null, (_, _) => SetFocusProfile(preset, null)) { Checked = profile == null });
                foreach (var p in host.Profiles)
                    mode.DropDownItems.Add(new ToolStripMenuItem(p, null, (_, _) => SetFocusProfile(preset, p)) { Checked = profile == p });
                if (host.Profiles.Count == 0)
                    mode.DropDownItems.Add(new ToolStripMenuItem(Strings.FocusProfileHint) { Enabled = false });
                menu.Add(mode);
            }

            menu.Add(Strings.FocusSkip, null, (_, _) =>
            {
                (phase, round) = After(phase, round);
                left = Length(phase);
                UpdateFocusProfile();
                RequestRedraw();
            });
        }

        private void SetFocusProfile(int preset, string? profile)
        {
            setOption(FormatOption(preset, profile));
            UpdateFocusProfile();
        }

        public override void Dispose()
        {
            // Closing the widget mid-focus gives the previous profile back
            running = false;
            UpdateFocusProfile();
        }
    }
}
