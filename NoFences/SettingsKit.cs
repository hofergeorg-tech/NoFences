namespace NoFences
{
    /// <summary>Building blocks for the settings windows: section headers, label/control rows, hints.</summary>
    internal static class SettingsKit
    {
        public static Font HeaderFont(Font baseFont) => new(baseFont.FontFamily, baseFont.Size * 1.35f, FontStyle.Bold);

        /// <summary>A section: bold title with a thin accent line under it, then a two-column grid for rows.</summary>
        public static TableLayoutPanel Section(Control parent, string title, int width)
        {
            var header = new Label
            {
                Text = title,
                AutoSize = true,
                Font = HeaderFont(parent.Font),
                Margin = new Padding(0, parent.Controls.Count == 0 ? 0 : 18, 0, 2)
            };
            var line = new Panel { Height = 2, Width = width, BackColor = Color.FromArgb(0, 120, 212), Margin = new Padding(0, 0, 0, 8) };
            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Width = width, Margin = new Padding(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Math.Min(170, width / 3)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            parent.Controls.Add(header);
            parent.Controls.Add(line);
            parent.Controls.Add(grid);
            return grid;
        }

        /// <summary>A label and its control; <paramref name="extra"/> goes next to the control.</summary>
        public static void Row(TableLayoutPanel grid, string label, Control control, Control? extra = null)
        {
            grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 12, 7) });
            if (extra == null)
            {
                control.Margin = new Padding(0, 3, 0, 3);
                grid.Controls.Add(control);
                return;
            }
            var pair = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            control.Margin = new Padding(0, 3, 8, 3);
            extra.Margin = new Padding(0, 3, 0, 3);
            pair.Controls.Add(control);
            pair.Controls.Add(extra);
            grid.Controls.Add(pair);
        }

        /// <summary>A control spanning both columns (checkboxes, buttons).</summary>
        public static void Wide(TableLayoutPanel grid, Control control)
        {
            control.Margin = new Padding(0, 4, 0, 4);
            grid.Controls.Add(control);
            grid.SetColumnSpan(control, 2);
        }

        /// <summary>Grey explanatory text under a row.</summary>
        public static void Hint(TableLayoutPanel grid, string text, int width)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(width, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 6)
            };
            grid.Controls.Add(label);
            grid.SetColumnSpan(label, 2);
        }

        public static CheckBox Check(string text, bool value, Action<bool> changed)
        {
            var box = new CheckBox { Text = text, Checked = value, AutoSize = true };
            box.CheckedChanged += (_, _) => changed(box.Checked);
            return box;
        }

        public static ComboBox Choice(IEnumerable<string> items, int selected, Action<int> changed, int width = 240)
        {
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width };
            box.Items.AddRange(items.Cast<object>().ToArray());
            box.SelectedIndex = Math.Clamp(selected, 0, box.Items.Count - 1);
            box.SelectedIndexChanged += (_, _) => changed(box.SelectedIndex);
            return box;
        }

        public static Button Action(string text, Action click)
        {
            var button = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
            button.Click += (_, _) => click();
            return button;
        }
    }
}
