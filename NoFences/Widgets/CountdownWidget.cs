using System.Globalization;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Counts down to a date ("12 days" … "12 min 30 s"), e.g. holidays or a game event.</summary>
    public sealed class CountdownWidget : FenceWidget
    {
        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;

        public CountdownWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "countdown";

        public override int RefreshMs => 1000;

        /// <summary>Stored as "yyyy-MM-ddTHH:mm|Title" in the fence's widget option.</summary>
        public static (DateTime Target, string Title)? Parse(string? option)
        {
            if (string.IsNullOrEmpty(option))
                return null;
            var sep = option.IndexOf('|');
            var date = sep < 0 ? option : option[..sep];
            var title = sep < 0 ? "" : option[(sep + 1)..];
            return DateTime.TryParseExact(date, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var target)
                ? (target, title)
                : null;
        }

        public static string Format(DateTime target, string title) =>
            $"{target.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture)}|{title}";

        /// <summary>The big text: the coarsest units that still say something.</summary>
        public static string Remaining(TimeSpan left)
        {
            if (left <= TimeSpan.Zero)
                return "";
            if (left.TotalDays >= 2)
                return Strings.CountdownDays((int)left.TotalDays);
            if (left.TotalDays >= 1)
                return $"{Strings.CountdownDays(1)} {left.Hours} h";
            if (left.TotalHours >= 1)
                return $"{(int)left.TotalHours} h {left.Minutes} min";
            return $"{left.Minutes} min {left.Seconds} s";
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var parsed = Parse(getOption());
            if (parsed is not var (target, title))
            {
                c.Text(Strings.CountdownHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }

            var left = target - DateTime.Now;
            using var big = c.Sized(Math.Min(c.Area.Height * 0.32f, c.Area.Width * 0.16f), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            var y = c.Area.Y + Math.Max(0, (c.Area.Height - bigHeight - 2 * line) / 2);
            var text = left > TimeSpan.Zero ? Remaining(left) : Strings.CountdownReached;
            c.Text(text, new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big, StringAlignment.Center);
            y += bigHeight + c.Px(4);
            if (title.Length > 0)
            {
                c.Text(title, new RectangleF(c.Area.X, y, c.Area.Width, line), align: StringAlignment.Center);
                y += line;
            }
            c.Text(target.ToString("ddd d. MMMM yyyy, HH:mm", CultureInfo.CurrentUICulture), new RectangleF(c.Area.X, y, c.Area.Width, line), align: StringAlignment.Center);
        }

        public override void DoubleClick(Point p) => Edit(null);

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner) =>
            menu.Add(Strings.CountdownSet, null, (_, _) => Edit(owner));

        private void Edit(IWin32Window? owner)
        {
            var current = Parse(getOption());
            using var dialog = new CountdownDialog(current?.Target ?? DateTime.Today.AddDays(7).AddHours(18), current?.Title ?? "");
            if (dialog.ShowDialog(owner) == DialogResult.OK)
                setOption(Format(dialog.Target, dialog.Title));
        }
    }

    /// <summary>Title and date/time of a countdown.</summary>
    internal sealed class CountdownDialog : Form
    {
        private readonly TextBox titleBox = new() { Width = 240 };
        private readonly DateTimePicker picker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy   HH:mm", Width = 240 };

        public DateTime Target => new(picker.Value.Year, picker.Value.Month, picker.Value.Day, picker.Value.Hour, picker.Value.Minute, 0);
        public string Title => titleBox.Text.Trim().Replace("|", "/");

        public CountdownDialog(DateTime target, string title)
        {
            Text = Strings.WidgetCountdown;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            titleBox.Text = title;
            picker.Value = target;

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            SettingsKit.Row(grid, Strings.CountdownTitleLabel, titleBox);
            SettingsKit.Row(grid, Strings.CountdownDateLabel, picker);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(grid);
            Controls.Add(buttons);
        }
    }
}
