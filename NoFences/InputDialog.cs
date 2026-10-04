using NoFences.Util;

namespace NoFences
{
    /// <summary>Asks for one line of text (used for renaming).</summary>
    public sealed class InputDialog : Form
    {
        private readonly TextBox box = new() { Width = 340 };

        public string Value => box.Text;

        /// <param name="selectStem">Select only the part before the extension, like Explorer does.</param>
        public InputDialog(string title, string prompt, string value, bool selectStem = false, bool password = false)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);
            Font = SystemFonts.MessageBoxFont ?? Font;

            box.Text = value;
            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            AcceptButton = ok;
            CancelButton = cancel;

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill };
            layout.Controls.Add(new Label { Text = prompt, AutoSize = true });
            layout.Controls.Add(box);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 10, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            Controls.Add(layout);
            Controls.Add(buttons);

            Shown += (_, _) =>
            {
                box.Focus();
                var dot = selectStem ? value.LastIndexOf('.') : -1;
                box.Select(0, dot > 0 ? dot : value.Length);
            };
        }
    }
}
