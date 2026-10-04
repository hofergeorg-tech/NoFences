using System.Globalization;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Kitchen timers (quick buttons +1/+5/+10/+15/+30 min) and alarms. NoFences rings them itself, also
    /// while the widget is hidden; click a timer's × to cancel it, click an alarm to switch it on/off.
    /// </summary>
    public sealed class TimerWidget : FenceWidget
    {
        private static readonly int[] Quick = { 1, 5, 10, 15, 30 };

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly Action stopSound;
        private readonly List<(RectangleF Rect, int Minutes)> quickButtons = new();
        private readonly List<(RectangleF Rect, int Index)> cancelButtons = new();
        private readonly List<(RectangleF Rect, int Index)> alarmRows = new();
        private int hoveredAlarm = -1;

        public TimerWidget(Func<string?> getOption, Action<string?> setOption, Action stopSound)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            this.stopSound = stopSound;
        }

        public override string Type => "timer";

        public override int RefreshMs => 1000;

        private TimerSet Set => TimerSet.Parse(getOption());

        private void Save(TimerSet set) => setOption(set.Format());

        public static string FormatLeft(TimeSpan left) =>
            left.TotalHours >= 1 ? $"{(int)left.TotalHours}:{left.Minutes:00}:{left.Seconds:00}" : $"{left.Minutes:00}:{left.Seconds:00}";

        public override void Draw(WidgetCanvas c)
        {
            quickButtons.Clear();
            cancelButtons.Clear();
            alarmRows.Clear();
            var set = Set;
            var now = DateTime.Now;
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            float y = c.Area.Y;

            // Quick buttons
            var gap = c.Px(4);
            var w = (c.Area.Width - gap * (Quick.Length - 1)) / Quick.Length;
            for (var i = 0; i < Quick.Length; i++)
            {
                var r = new RectangleF(c.Area.X + i * (w + gap), y, w, line + c.Px(4));
                using (var back = new SolidBrush(Color.FromArgb(70, c.Theme.Accent)))
                    c.G.FillRectangle(back, r);
                c.Text($"+{Quick[i]}", r, align: StringAlignment.Center, valign: StringAlignment.Center);
                quickButtons.Add((r, Quick[i]));
            }
            y += line + c.Px(12);

            // Running timers: big remaining time, label, bar, ×
            using var big = c.Sized(Math.Min(c.Px(30), c.Area.Width * 0.14f), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            for (var i = 0; i < set.Timers.Count; i++)
            {
                var t = set.Timers[i];
                if (y + bigHeight > c.Area.Bottom)
                    break;
                var left = t.End - now;
                var text = t.Fired || left <= TimeSpan.Zero ? Strings.TimerDone : FormatLeft(left);
                c.Text(text, new RectangleF(c.Area.X, y, c.Area.Width * 0.6f, bigHeight), big);
                c.Text(t.Label.Length > 0 ? t.Label : Strings.WidgetTimer, new RectangleF(c.Area.X + c.Area.Width * 0.5f, y, c.Area.Width * 0.5f - c.Px(22), bigHeight),
                    align: StringAlignment.Far, valign: StringAlignment.Center);
                var cancel = new RectangleF(c.Area.Right - c.Px(18), y + (bigHeight - c.Px(18)) / 2, c.Px(18), c.Px(18));
                using (var pen = new Pen(c.Ink, Math.Max(1.5f, 1.5f * c.S)))
                {
                    var inset = cancel.Width * 0.28f;
                    c.G.DrawLine(pen, cancel.X + inset, cancel.Y + inset, cancel.Right - inset, cancel.Bottom - inset);
                    c.G.DrawLine(pen, cancel.Right - inset, cancel.Y + inset, cancel.X + inset, cancel.Bottom - inset);
                }
                cancelButtons.Add((cancel, i));
                y += bigHeight;
                var total = (t.End - t.Start).TotalSeconds;
                c.Bar(new RectangleF(c.Area.X, y, c.Area.Width, c.Px(4)), total <= 0 ? 1 : 1 - Math.Max(0, left.TotalSeconds) / total);
                y += c.Px(12);
            }

            // Alarms
            if (set.Alarms.Count > 0)
            {
                c.Muted(Strings.AlarmsHeading, c.Area.X, y, c.Label, Color.FromArgb(220, c.Theme.Accent));
                y += line + c.Px(2);
            }
            var culture = new CultureInfo(Strings.Effective);
            for (var i = 0; i < set.Alarms.Count; i++)
            {
                var a = set.Alarms[i];
                if (y + line > c.Area.Bottom)
                    break;
                var r = new RectangleF(c.Area.X, y, c.Area.Width, line + c.Px(4));
                var dot = c.Px(10);
                using (var brush = new SolidBrush(a.Enabled ? c.Theme.Accent : Color.FromArgb(90, c.Theme.HintColor)))
                    c.G.FillEllipse(brush, r.X, r.Y + (r.Height - dot) / 2, dot, dot);
                using var bold = new Font(c.Label, a.Enabled ? FontStyle.Bold : FontStyle.Regular);
                c.Text(a.Time, new RectangleF(r.X + dot + c.Px(8), r.Y, c.Px(60), r.Height), bold, valign: StringAlignment.Center);
                var days = a.Once ? Strings.AlarmOnce : a.Days.Count == 0 || a.Days.Count == 7 ? Strings.RepeatName(Repeat.Daily)
                    : string.Join(" ", a.Days.OrderBy(d => ((int)d + 6) % 7).Select(d => culture.DateTimeFormat.GetAbbreviatedDayName(d)));
                c.Text($"{a.Label}  {days}".Trim(), new RectangleF(r.X + dot + c.Px(70), r.Y, r.Width - dot - c.Px(70), r.Height), valign: StringAlignment.Center);
                alarmRows.Add((r, i));
                y += r.Height;
            }

            if (set.Timers.Count == 0 && set.Alarms.Count == 0 && y + line * 2 < c.Area.Bottom)
                c.TextWrapped(Strings.TimerHint, c.Area.X, y, c.Area.Width, c.Label, 3);
        }

        public override bool IsClickable(Point p)
        {
            hoveredAlarm = alarmRows.FirstOrDefault(r => r.Rect.Contains(p)) is { Rect.Width: > 0 } row ? row.Index : -1;
            return quickButtons.Any(b => b.Rect.Contains(p)) || cancelButtons.Any(b => b.Rect.Contains(p)) || hoveredAlarm >= 0;
        }

        public override bool Click(Point p)
        {
            var set = Set;
            var now = DateTime.Now;
            var quick = quickButtons.FirstOrDefault(b => b.Rect.Contains(p));
            if (quick.Minutes > 0)
            {
                set.Timers.Add(new CountdownTimer { Label = $"{quick.Minutes} min", Start = now, End = now.AddMinutes(quick.Minutes) });
                Save(set);
                return true;
            }
            var cancel = cancelButtons.FirstOrDefault(b => b.Rect.Contains(p));
            if (cancel.Rect.Width > 0 && cancel.Index < set.Timers.Count)
            {
                set.Timers.RemoveAt(cancel.Index);
                Save(set);
                stopSound();
                return true;
            }
            var alarm = alarmRows.FirstOrDefault(r => r.Rect.Contains(p));
            if (alarm.Rect.Width > 0 && alarm.Index < set.Alarms.Count)
            {
                set.Alarms[alarm.Index].Enabled = !set.Alarms[alarm.Index].Enabled;
                Save(set);
                return true;
            }
            return false;
        }

        public override void DoubleClick(Point p)
        {
            if (!quickButtons.Any(b => b.Rect.Contains(p)) && !alarmRows.Any(r => r.Rect.Contains(p)))
                NewAlarm(null);
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.TimerNew, null, (_, _) => NewTimer(owner));
            menu.Add(Strings.AlarmNew, null, (_, _) => NewAlarm(owner));
            if (hoveredAlarm >= 0 && hoveredAlarm < Set.Alarms.Count)
            {
                var index = hoveredAlarm;
                menu.Add(Strings.AlarmDelete, null, (_, _) =>
                {
                    var set = Set;
                    if (index < set.Alarms.Count)
                        set.Alarms.RemoveAt(index);
                    Save(set);
                });
            }
            menu.Add(Strings.TimerStopSound, null, (_, _) => stopSound());
        }

        private void NewTimer(IWin32Window? owner)
        {
            using var dialog = new InputDialog(Strings.TimerNew.TrimEnd('…'), Strings.TimerPrompt, "15");
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            // "15" or "15 Pasta" or "1:30 Pizza"
            var text = dialog.Value.Trim();
            var space = text.IndexOf(' ');
            var amount = space > 0 ? text[..space] : text;
            var label = space > 0 ? text[(space + 1)..].Trim() : "";
            TimeSpan length;
            if (amount.Contains(':') && TimeSpan.TryParseExact(amount, @"h\:mm", CultureInfo.InvariantCulture, out var hm))
                length = hm;
            else if (double.TryParse(amount.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes) && minutes > 0)
                length = TimeSpan.FromMinutes(minutes);
            else
                return;
            var now = DateTime.Now;
            var set = Set;
            set.Timers.Add(new CountdownTimer { Label = label.Length > 0 ? label : FormatLeft(length), Start = now, End = now + length });
            Save(set);
            RequestRedraw();
        }

        private void NewAlarm(IWin32Window? owner)
        {
            using var dialog = new AlarmDialog();
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            var set = Set;
            set.Alarms.Add(dialog.Alarm);
            set.Alarms.Sort((a, b) => string.CompareOrdinal(a.Time, b.Time));
            Save(set);
            RequestRedraw();
        }
    }

    /// <summary>Time, label and days of a new alarm.</summary>
    internal sealed class AlarmDialog : Form
    {
        private readonly DateTimePicker time = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 90 };
        private readonly TextBox label = new() { Width = 200 };
        private readonly CheckBox once = new() { AutoSize = true };
        private readonly CheckBox[] days = new CheckBox[7];

        public Alarm Alarm => new()
        {
            Time = time.Value.ToString("HH:mm", CultureInfo.InvariantCulture),
            Label = label.Text.Trim(),
            Once = once.Checked,
            Days = once.Checked ? new() : days.Where(d => d.Checked).Select(d => (DayOfWeek)d.Tag!).ToList()
        };

        public AlarmDialog()
        {
            Text = Strings.AlarmNew.TrimEnd('…');
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            time.Value = DateTime.Today.AddHours(7);
            once.Text = Strings.AlarmOnce;
            var culture = new CultureInfo(Strings.Effective);
            var dayRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            for (var i = 0; i < 7; i++)
            {
                var day = (DayOfWeek)((i + 1) % 7);
                days[i] = new CheckBox { Text = culture.DateTimeFormat.GetAbbreviatedDayName(day), AutoSize = true, Checked = i < 5, Tag = day };
                dayRow.Controls.Add(days[i]);
            }
            once.CheckedChanged += (_, _) => dayRow.Enabled = !once.Checked;

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            SettingsKit.Row(grid, Strings.AlarmTimeLabel, time);
            SettingsKit.Row(grid, Strings.CountdownTitleLabel, label);
            SettingsKit.Wide(grid, dayRow);
            SettingsKit.Wide(grid, once);

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
