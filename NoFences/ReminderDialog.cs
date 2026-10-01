using NoFences.Util;

namespace NoFences
{
    /// <summary>Picks the time for a note reminder, with quick choices.</summary>
    public sealed class ReminderDialog : Form
    {
        private readonly DateTimePicker picker = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd.MM.yyyy   HH:mm",
            ShowUpDown = false,
            Width = 200
        };

        /// <summary>The chosen time, or null if the reminder was removed.</summary>
        public DateTime? Result { get; private set; }

        public ReminderDialog(DateTime? current)
        {
            Text = Strings.ReminderTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var now = DateTime.Now;
            picker.Value = current is DateTime c && c > now ? c : now.AddHours(1);

            var quick = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 8, 0, 0) };
            void Quick(string text, DateTime when) =>
                quick.Controls.Add(new Button { Text = text, AutoSize = true }.Also(b => b.Click += (_, _) => picker.Value = when));
            Quick(Strings.ReminderIn1h, now.AddHours(1));
            var tonight = now.Date.AddHours(18);
            if (tonight > now)
                Quick(Strings.ReminderTonight, tonight);
            Quick(Strings.ReminderTomorrow, now.Date.AddDays(1).AddHours(9));

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var remove = new Button { Text = Strings.ReminderRemove, DialogResult = DialogResult.OK, AutoSize = true, Visible = current != null };
            ok.Click += (_, _) => Result = picker.Value;
            remove.Click += (_, _) => Result = null;
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok, remove });
            AcceptButton = ok;
            CancelButton = cancel;

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill };
            layout.Controls.Add(picker);
            layout.Controls.Add(quick);
            Controls.Add(layout);
            Controls.Add(buttons);
        }
    }

    internal static class ControlExtensions
    {
        /// <summary>Runs an action on an object and returns it, for building controls inline.</summary>
        public static T Also<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }
    }
}
