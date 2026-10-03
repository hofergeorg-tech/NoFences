using System.Globalization;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>One profile rule: which profile, and when (a program runs / days and times).</summary>
    internal sealed class ProfileRuleDialog : Form
    {
        private readonly ComboBox profile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly RadioButton byProgram = new() { AutoSize = true };
        private readonly RadioButton byTime = new() { AutoSize = true };
        private readonly TextBox program = new() { Width = 260 };
        private readonly CheckBox[] days = new CheckBox[7];
        private readonly DateTimePicker from = TimePicker();
        private readonly DateTimePicker to = TimePicker();

        public ProfileRule Rule { get; private set; } = new();

        private static DateTimePicker TimePicker() => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 80 };

        public ProfileRuleDialog(IReadOnlyList<string> profiles, ProfileRule? existing)
        {
            Text = Strings.RuleTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var rule = existing ?? new ProfileRule { Profile = profiles.FirstOrDefault() ?? "" };
            profile.Items.AddRange(profiles.Cast<object>().ToArray());
            profile.SelectedIndex = Math.Max(0, profiles.ToList().IndexOf(rule.Profile));
            byProgram.Text = Strings.RuleByProgram;
            byTime.Text = Strings.RuleByTime;
            byProgram.Checked = rule.Trigger == ProfileTrigger.Program;
            byTime.Checked = rule.Trigger == ProfileTrigger.Time;
            program.Text = rule.Program ?? "";
            from.Value = DateTime.Today + ProfileRule.ParseTime(rule.From);
            to.Value = DateTime.Today + ProfileRule.ParseTime(rule.To);

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            SettingsKit.Row(grid, Strings.RuleProfile, profile);
            SettingsKit.Wide(grid, byProgram);

            var browse = new Button { Text = Strings.Browse, AutoSize = true };
            browse.Click += (_, _) =>
            {
                using var dialog = new OpenFileDialog { Filter = Strings.PlaytimeExeFilter };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    program.Text = dialog.FileName;
            };
            var programRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 0, 0, 6) };
            programRow.Controls.Add(program);
            programRow.Controls.Add(browse);
            SettingsKit.Wide(grid, programRow);
            SettingsKit.Wide(grid, byTime);

            // Monday first, as calendars here do
            var dayRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 0, 0, 0) };
            var culture = new CultureInfo(Strings.Effective);
            for (var i = 0; i < 7; i++)
            {
                var day = (DayOfWeek)((i + 1) % 7);
                days[i] = new CheckBox { Text = culture.DateTimeFormat.GetAbbreviatedDayName(day), AutoSize = true, Checked = rule.Days.Contains(day), Tag = day };
                dayRow.Controls.Add(days[i]);
            }
            SettingsKit.Wide(grid, dayRow);
            var timeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 4, 0, 0) };
            timeRow.Controls.Add(new Label { Text = Strings.RuleFrom, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            timeRow.Controls.Add(from);
            timeRow.Controls.Add(new Label { Text = Strings.RuleTo, AutoSize = true, Margin = new Padding(12, 6, 6, 0) });
            timeRow.Controls.Add(to);
            SettingsKit.Wide(grid, timeRow);

            void UpdateEnabled()
            {
                programRow.Enabled = byProgram.Checked;
                dayRow.Enabled = timeRow.Enabled = byTime.Checked;
            }
            byProgram.CheckedChanged += (_, _) => UpdateEnabled();
            UpdateEnabled();

            var ok = new Button { Text = Strings.Ok, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            ok.Click += (_, _) =>
            {
                if (byProgram.Checked && program.Text.Trim().Length == 0)
                {
                    program.Focus();
                    return;
                }
                Rule = new ProfileRule
                {
                    Profile = profile.SelectedItem as string ?? "",
                    Trigger = byProgram.Checked ? ProfileTrigger.Program : ProfileTrigger.Time,
                    Program = byProgram.Checked ? program.Text.Trim() : null,
                    Days = days.Where(d => d.Checked).Select(d => (DayOfWeek)d.Tag!).ToList(),
                    From = from.Value.ToString("HH:mm", CultureInfo.InvariantCulture),
                    To = to.Value.ToString("HH:mm", CultureInfo.InvariantCulture)
                };
                DialogResult = DialogResult.OK;
                Close();
            };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(grid);
            Controls.Add(buttons);
        }

        /// <summary>"Gaming – while eldenring runs" / "Work – Mon–Fri 08:00–17:00".</summary>
        public static string Describe(ProfileRule rule)
        {
            if (rule.Trigger == ProfileTrigger.Program)
                return Strings.RuleWhileRunning(rule.Profile, rule.ProcessName);
            var culture = new CultureInfo(Strings.Effective);
            var names = rule.Days.OrderBy(d => ((int)d + 6) % 7).Select(d => culture.DateTimeFormat.GetAbbreviatedDayName(d));
            return Strings.RuleAtTimes(rule.Profile, string.Join(" ", names), rule.From, rule.To);
        }
    }
}
