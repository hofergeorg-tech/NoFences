using System.Globalization;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// To-dos with optional due time and repetition. Click the circle to tick one off (repeating ones
    /// move to their next date), double-click to add, right-click for edit/delete. Due to-dos are
    /// announced by NoFences even while the widget is hidden.
    /// </summary>
    public sealed class TodoWidget : FenceWidget
    {
        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly List<(RectangleF Circle, RectangleF Row, TodoItem Item)> rows = new();
        private TodoItem? hovered;
        private float scroll, maxScroll;

        public TodoWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "todo";

        public override int RefreshMs => 30_000;

        private List<TodoItem> Items => TodoList.Parse(getOption());

        private void Save(List<TodoItem> items) => setOption(TodoList.Format(items));

        /// <summary>"today 18:00", "Mon 09:00", "12.10."; red when overdue.</summary>
        public static string FormatDue(DateTime due, DateTime now)
        {
            var culture = new CultureInfo(Strings.Effective);
            var time = due.TimeOfDay == TimeSpan.Zero ? "" : " " + due.ToString("t", culture);
            if (due.Date == now.Date)
                return Strings.PlaytimeToday + time;
            if (due.Date == now.Date.AddDays(1))
                return Strings.AgendaTomorrow + time;
            if (due.Date > now.Date && due.Date < now.Date.AddDays(7))
                return due.ToString("ddd", culture) + time;
            return due.ToString("d. MMM", culture) + time;
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var items = TodoList.Ordered(Items);
            if (items.Count == 0)
            {
                c.TextWrapped(Strings.TodoHint, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                return;
            }
            var now = DateTime.Now;
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            using var done = new Font(c.Label, FontStyle.Strikeout);
            var circle = c.Px(14);
            var state = c.G.Save();
            c.G.SetClip(c.Area);
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float y = c.Area.Y - scroll;
            foreach (var item in items)
            {
                var top = y;
                var circleRect = new RectangleF(c.Area.X, y + (line - circle) / 2, circle, circle);
                using (var pen = new Pen(item.Done ? Color.FromArgb(150, c.Theme.HintColor) : c.Theme.Accent, Math.Max(1.5f, 1.5f * c.S)))
                    c.G.DrawEllipse(pen, circleRect);
                if (item.Done)
                {
                    using var check = new Pen(Color.FromArgb(200, c.Theme.HintColor), Math.Max(1.5f, 1.8f * c.S));
                    c.G.DrawLines(check, new[]
                    {
                        new PointF(circleRect.X + circle * 0.25f, circleRect.Y + circle * 0.52f),
                        new PointF(circleRect.X + circle * 0.43f, circleRect.Y + circle * 0.7f),
                        new PointF(circleRect.X + circle * 0.76f, circleRect.Y + circle * 0.3f)
                    });
                }
                var x = circleRect.Right + c.Px(8);
                var textHeight = c.TextWrapped(item.Text, x, y, c.Area.Right - x, item.Done ? done : c.Label, 3);
                y += Math.Max(line, textHeight);
                if (item.Due is DateTime due && !item.Done)
                {
                    var overdue = due <= now;
                    var meta = FormatDue(due, now) + (item.Repeat != Repeat.None ? "  ↻ " + Strings.RepeatName(item.Repeat) : "");
                    c.Muted(meta, x, y, small, overdue ? Color.FromArgb(240, 230, 80, 70) : null);
                    y += small.GetHeight(c.G);
                }
                y += c.Px(6);
                rows.Add((RectangleF.Inflate(circleRect, c.Px(4), c.Px(4)), new RectangleF(c.Area.X, top, c.Area.Width, y - top), item));
            }
            c.G.Restore(state);
            maxScroll = Math.Max(0, y + scroll - c.Area.Bottom);
            // Grown fence: everything fits now, so don't stay scrolled down (the top would stay hidden)
            if (scroll > maxScroll)
            {
                scroll = maxScroll;
                RequestRedraw();
            }
        }

        private int IndexOf(TodoItem item, List<TodoItem> all) =>
            all.FindIndex(i => i.Text == item.Text && i.Due == item.Due && i.Done == item.Done);

        public override bool IsClickable(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Row.Contains(p));
            if (row.Item != hovered)
                hovered = row.Item;
            return rows.Any(r => r.Circle.Contains(p));
        }

        public override bool Click(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Circle.Contains(p));
            if (row.Item == null)
                return false;
            var items = Items;
            var index = IndexOf(row.Item, items);
            if (index < 0)
                return false;
            items[index].Toggle(DateTime.Now);
            Save(items);
            return true;
        }

        public override void DoubleClick(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Row.Contains(p));
            if (row.Item != null)
                Edit(row.Item, null);
            else
                Add(null);
        }

        public override bool Wheel(int delta)
        {
            if (maxScroll <= 0 && scroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 50, 0, maxScroll);
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.TodoAdd, null, (_, _) => Add(owner));
            if (hovered is { } item)
            {
                menu.Add(Strings.TodoEdit(item.Text), null, (_, _) => Edit(item, owner));
                menu.Add(Strings.TodoDelete, null, (_, _) =>
                {
                    var items = Items;
                    var index = IndexOf(item, items);
                    if (index >= 0)
                    {
                        items.RemoveAt(index);
                        Save(items);
                    }
                });
            }
            if (Items.Any(i => i.Done))
                menu.Add(Strings.TodoClearDone, null, (_, _) => Save(Items.Where(i => !i.Done).ToList()));
        }

        private void Add(IWin32Window? owner)
        {
            using var dialog = new TodoDialog(null);
            if (dialog.ShowDialog(owner) != DialogResult.OK || dialog.Item.Text.Length == 0)
                return;
            var items = Items;
            items.Add(dialog.Item);
            Save(items);
            RequestRedraw();
        }

        private void Edit(TodoItem item, IWin32Window? owner)
        {
            using var dialog = new TodoDialog(item);
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            var items = Items;
            var index = IndexOf(item, items);
            if (index < 0)
                return;
            if (dialog.Item.Text.Length == 0)
                items.RemoveAt(index);
            else
                items[index] = dialog.Item;
            Save(items);
            RequestRedraw();
        }
    }

    /// <summary>Text, optional due date/time and repetition of a to-do.</summary>
    internal sealed class TodoDialog : Form
    {
        private readonly TextBox text = new() { Width = 300 };
        private readonly CheckBox hasDue = new() { AutoSize = true };
        private readonly DateTimePicker due = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy   HH:mm", Width = 180 };
        private readonly ComboBox repeat = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        private readonly TodoItem? original;

        public TodoItem Item => new()
        {
            Text = text.Text.Trim(),
            Due = hasDue.Checked ? due.Value : null,
            Repeat = hasDue.Checked ? (Repeat)repeat.SelectedIndex : Repeat.None,
            Done = original?.Done ?? false,
            // A changed due time may notify again
            Notified = original != null && original.Due == (hasDue.Checked ? due.Value : null) && original.Notified
        };

        public TodoDialog(TodoItem? item)
        {
            original = item;
            Text = item == null ? Strings.TodoAdd.TrimEnd('…') : Strings.WidgetTodo;
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

            text.Text = item?.Text ?? "";
            hasDue.Text = Strings.TodoDueLabel;
            hasDue.Checked = item?.Due != null;
            due.Value = item?.Due ?? DateTime.Today.AddDays(1).AddHours(9);
            repeat.Items.AddRange(Enum.GetValues<Repeat>().Select(r => (object)Strings.RepeatName(r)).ToArray());
            repeat.SelectedIndex = (int)(item?.Repeat ?? Repeat.None);
            void Update() => due.Enabled = repeat.Enabled = hasDue.Checked;
            hasDue.CheckedChanged += (_, _) => Update();
            Update();

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            SettingsKit.Row(grid, Strings.TodoTextLabel, text);
            SettingsKit.Row(grid, "", hasDue);
            SettingsKit.Row(grid, Strings.CountdownDateLabel, due);
            SettingsKit.Row(grid, Strings.RepeatLabel, repeat);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(grid);
            Controls.Add(buttons);
            Shown += (_, _) => text.Focus();
        }
    }
}
