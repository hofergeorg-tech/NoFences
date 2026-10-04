using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// A ringing timer or alarm: a small window at the bottom right with "Stop" and snooze buttons
    /// (a tray notification can't have buttons). Closes itself after two minutes.
    /// </summary>
    internal sealed class AlarmPopup : Form
    {
        private static readonly int[] SnoozeMinutes = { 5, 10 };
        private readonly System.Windows.Forms.Timer closeTimer = new() { Interval = 120_000 };

        public static void Show(string text, Action stop, Action<int> snooze) => new AlarmPopup(text, stop, snooze).Show();

        private AlarmPopup(string text, Action stop, Action<int> snooze)
        {
            Text = "NoFences";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            layout.Controls.Add(new Label { Text = text, AutoSize = true, MaximumSize = new Size(320, 0), Font = new Font(Font.FontFamily, Font.Size * 1.15f, FontStyle.Bold) });
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            var stopButton = new Button { Text = Strings.AlarmStop, AutoSize = true };
            stopButton.Click += (_, _) =>
            {
                stop();
                Close();
            };
            buttons.Controls.Add(stopButton);
            foreach (var minutes in SnoozeMinutes)
            {
                var button = new Button { Text = Strings.AlarmSnooze(minutes), AutoSize = true };
                button.Click += (_, _) =>
                {
                    snooze(minutes);
                    Close();
                };
                buttons.Controls.Add(button);
            }
            layout.Controls.Add(buttons);
            Controls.Add(layout);
            AcceptButton = stopButton;

            closeTimer.Tick += (_, _) => Close();
            closeTimer.Start();
            FormClosed += (_, _) => stop(); // closing the window also silences it
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Cursor.Position).WorkingArea;
            // Several ringing at once stack upwards
            var others = Application.OpenForms.OfType<AlarmPopup>().Count(p => p != this);
            Location = new Point(area.Right - Width - 12, area.Bottom - (Height + 12) * (others + 1));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                closeTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
