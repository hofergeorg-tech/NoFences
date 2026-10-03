using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;

namespace NoFences
{
    /// <summary>All per-fence settings in one place (replaces the old rename and title-height dialogs).</summary>
    public sealed class FenceSettingsDialog : Form
    {
        private static readonly int[] IconSizes = { 24, 32, 48, 64, 96 };

        private readonly TextBox nameBox = new() { Dock = DockStyle.Fill };
        private readonly ComboBox kindBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox folderBox = new() { Dock = DockStyle.Fill };
        private readonly Button browseButton = new() { Text = Strings.Browse, AutoSize = true };
        private readonly ComboBox themeBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown titleHeight = new() { Minimum = 16, Maximum = 100, Width = 70 };
        private readonly ComboBox iconSizeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
        private readonly Button colorButton = new() { Width = 70, FlatStyle = FlatStyle.Flat };
        private readonly TrackBar opacity = new() { Minimum = 0, Maximum = 255, TickFrequency = 32, Dock = DockStyle.Fill };
        private readonly CheckBox lockedBox = new() { Text = Strings.Locked, AutoSize = true };
        private readonly CheckBox collapseBox = new() { Text = Strings.AutoCollapse, AutoSize = true };
        private readonly ComboBox sortBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox autoSortBox = new() { Dock = DockStyle.Fill };
        private readonly ComboBox presetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };

        private Color backgroundColor;

        public FenceSettingsDialog(FenceInfo info)
        {
            Text = $"{info.Name} – {Strings.Settings.TrimEnd('…')}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var grid = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Dock = DockStyle.Fill };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            void Row(string label, Control control, Control? extra = null)
            {
                grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 12, 6) });
                grid.Controls.Add(control);
                if (extra != null)
                    grid.Controls.Add(extra);
                else
                    grid.Controls.Add(new Label { AutoSize = true });
            }

            kindBox.Items.AddRange(new object[] { Strings.KindLinks, Strings.KindFolder, Strings.KindNote }); // index = (int)FenceKind
            // A widget can't be turned into another kind (or back); show it, but locked.
            if (info.Kind == FenceKind.Widget)
            {
                kindBox.Items.Add(Strings.KindWidget);
                kindBox.Enabled = false;
            }
            themeBox.Items.Add(Strings.ThemeInherit);
            foreach (var t in ThemeRegistry.All)
                themeBox.Items.Add(t.DisplayName);
            foreach (var size in IconSizes)
                iconSizeBox.Items.Add($"{size} px");

            Row(Strings.Name, nameBox);
            Row(Strings.Kind, kindBox);
            Row(Strings.Folder, folderBox, browseButton);
            Row(Strings.Theme, themeBox);
            foreach (var mode in Enum.GetValues<FenceSortMode>())
                sortBox.Items.Add(Strings.SortModeName(mode));
            Row(Strings.SortBy, sortBox);
            Row(Strings.TitleHeight, titleHeight);
            Row(Strings.IconSize, iconSizeBox);
            Row(Strings.Background, colorButton);
            Row(Strings.Opacity, opacity);
            presetBox.Items.Add(Strings.AddPreset);
            foreach (var preset in AutoSorter.Presets)
                presetBox.Items.Add(preset.Name());
            presetBox.SelectedIndex = 0;
            Row(Strings.AutoSort, autoSortBox, presetBox);
            grid.Controls.Add(new Label());
            grid.Controls.Add(new Label { Text = Strings.AutoSortHint, AutoSize = true, MaximumSize = new Size(280, 0), ForeColor = SystemColors.GrayText, Margin = new Padding(3, 0, 3, 8) });
            grid.Controls.Add(new Label());

            grid.Controls.Add(new Label());
            grid.Controls.Add(lockedBox);
            grid.Controls.Add(new Label());
            grid.Controls.Add(new Label());
            grid.Controls.Add(collapseBox);
            grid.Controls.Add(new Label());

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(grid);
            Controls.Add(buttons);

            // Values
            nameBox.Text = info.Name;
            kindBox.SelectedIndex = Math.Min((int)info.Kind, kindBox.Items.Count - 1);
            folderBox.Text = info.FolderPath ?? "";
            var themeIndex = info.Theme == null ? 0 : ThemeRegistry.All.ToList().FindIndex(t => t.Id == info.Theme) + 1;
            themeBox.SelectedIndex = Math.Max(0, themeIndex);
            titleHeight.Value = Math.Clamp(info.TitleHeight, 16, 100);
            var iconIndex = Array.IndexOf(IconSizes, info.IconSize);
            iconSizeBox.SelectedIndex = iconIndex >= 0 ? iconIndex : 1;
            backgroundColor = Color.FromArgb(255, Color.FromArgb(info.BackgroundColor));
            opacity.Value = Math.Clamp(info.BackgroundAlpha, 0, 255);
            lockedBox.Checked = info.Locked;
            collapseBox.Checked = info.CanMinify;
            autoSortBox.Text = info.AutoSortPatterns ?? "";
            sortBox.SelectedIndex = (int)info.SortMode;
            UpdateColorButton();
            UpdateEnabled();

            kindBox.SelectedIndexChanged += (_, _) => UpdateEnabled();
            themeBox.SelectedIndexChanged += (_, _) => UpdateEnabled();
            browseButton.Click += (_, _) => BrowseFolder();
            presetBox.SelectedIndexChanged += (_, _) =>
            {
                if (presetBox.SelectedIndex <= 0)
                    return;
                var add = AutoSorter.Presets[presetBox.SelectedIndex - 1].Patterns;
                var current = autoSortBox.Text.Trim().TrimEnd(';');
                autoSortBox.Text = current.Length == 0 ? add : current + "; " + add;
                presetBox.SelectedIndex = 0;
            };
            colorButton.Click += (_, _) =>
            {
                using var dlg = new ColorDialog { Color = backgroundColor, FullOpen = true };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    backgroundColor = dlg.Color;
                    UpdateColorButton();
                }
            };
            ok.Click += (_, _) =>
            {
                if (kindBox.SelectedIndex == 1 && !Directory.Exists(folderBox.Text))
                {
                    MessageBox.Show(this, Strings.FolderMissing(folderBox.Text), "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
        }

        private FenceTheme? SelectedTheme => themeBox.SelectedIndex > 0 ? ThemeRegistry.All[themeBox.SelectedIndex - 1] : null;

        private void UpdateEnabled()
        {
            var folder = kindBox.SelectedIndex == 1;
            folderBox.Enabled = browseButton.Enabled = folder;
            // Only the glass style is tinted with a custom color; the others have fixed palettes.
            colorButton.Enabled = SelectedTheme?.UsesCustomColor ?? true;
        }

        private void UpdateColorButton()
        {
            colorButton.BackColor = backgroundColor;
            colorButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
        }

        private void BrowseFolder()
        {
            using var dlg = new FolderBrowserDialog { Description = Strings.ChooseFolder, UseDescriptionForTitle = true, SelectedPath = folderBox.Text };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                folderBox.Text = dlg.SelectedPath;
                if (string.IsNullOrWhiteSpace(nameBox.Text) || nameBox.Text == Strings.NewFence)
                    nameBox.Text = Path.GetFileName(dlg.SelectedPath.TrimEnd('\\'));
            }
        }

        public void ApplyTo(FenceInfo info)
        {
            info.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? info.Name : nameBox.Text.Trim();
            var kind = (FenceKind)Math.Max(0, kindBox.SelectedIndex);
            if (kind != info.Kind || !string.Equals(info.FolderPath, folderBox.Text, StringComparison.OrdinalIgnoreCase))
                info.Files.Clear(); // the stored order belongs to the old content
            info.Kind = kind;
            info.FolderPath = kind == FenceKind.Folder ? folderBox.Text : null;
            info.Theme = SelectedTheme?.Id;
            info.TitleHeight = (int)titleHeight.Value;
            info.IconSize = IconSizes[Math.Max(0, iconSizeBox.SelectedIndex)];
            info.BackgroundColor = backgroundColor.ToArgb() & 0xFFFFFF;
            info.BackgroundAlpha = opacity.Value;
            info.Locked = lockedBox.Checked;
            info.CanMinify = collapseBox.Checked;
            info.AutoSortPatterns = string.IsNullOrWhiteSpace(autoSortBox.Text) ? null : autoSortBox.Text.Trim();
            info.SortMode = (FenceSortMode)Math.Max(0, sortBox.SelectedIndex);
        }
    }
}
