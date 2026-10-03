using System.Drawing.Imaging;
using System.Text.Json;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;
using static NoFences.SettingsKit;

namespace NoFences
{
    /// <summary>
    /// All settings of one fence, grouped into sections, with a live preview of the fence on the right
    /// that follows every change.
    /// </summary>
    public sealed class FenceSettingsDialog : Form
    {
        private const int ColumnWidth = 430;
        private static readonly int[] IconSizes = { 24, 32, 48, 64, 96 };

        private readonly FenceInfo original;
        private readonly TextBox nameBox = new() { Width = 260 };
        private readonly ComboBox kindBox = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox folderBox = new() { Width = 170 };
        private readonly Button browseButton = new() { Text = Strings.Browse, AutoSize = true };
        private readonly ComboBox themeBox = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 16 };
        private readonly NumericUpDown titleHeight = new() { Minimum = 16, Maximum = 100, Width = 70 };
        private readonly ComboBox iconSizeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
        private readonly Button colorButton = new() { Width = 46, Height = 24, FlatStyle = FlatStyle.Flat };
        private readonly TrackBar opacity = new() { Minimum = 0, Maximum = 255, TickStyle = TickStyle.None, Width = 150, AutoSize = false, Height = 26 };
        private readonly Label opacityValue = new() { AutoSize = true, Margin = new Padding(4, 5, 0, 0) };
        private readonly CheckBox compactBox = new() { Text = Strings.CompactMode, AutoSize = true };
        private readonly ComboBox sortBox = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox lockedBox = new() { Text = Strings.Locked, AutoSize = true };
        private readonly CheckBox collapseBox = new() { Text = Strings.AutoCollapse, AutoSize = true };
        private readonly CheckBox onTopBox = new() { Text = Strings.AlwaysOnTop, AutoSize = true };
        private readonly TextBox autoSortBox = new() { Width = 200 };
        private readonly ComboBox presetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        private readonly PictureBox preview = new() { Size = new Size(300, 270), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
        private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 150 };
        private readonly System.Windows.Forms.Timer iconsTimer = new() { Interval = 700 };

        private Color backgroundColor;

        public FenceSettingsDialog(FenceInfo info)
        {
            original = info;
            Text = $"{info.Name} – {Strings.Settings.TrimEnd('…')}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(20, 16, 20, 12);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var isWidget = info.Kind == FenceKind.Widget;
            var hasItems = info.Kind is FenceKind.Links or FenceKind.Folder;

            // ── left column: sections ──
            var left = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 24, 0) };

            var general = Section(left, Strings.SectionGeneral, ColumnWidth);
            Row(general, Strings.Name, nameBox);
            kindBox.Items.AddRange(new object[] { Strings.KindLinks, Strings.KindFolder, Strings.KindNote }); // index = (int)FenceKind
            if (isWidget)
            {
                // A widget can't be turned into another kind (or back); show it, but locked.
                kindBox.Items.Add(Strings.KindWidget);
                kindBox.Enabled = false;
            }
            Row(general, Strings.Kind, kindBox);
            Row(general, Strings.Folder, folderBox, browseButton);

            var look = Section(left, Strings.SectionAppearance, ColumnWidth);
            themeBox.Items.Add(Strings.ThemeInherit);
            foreach (var t in ThemeRegistry.All)
                themeBox.Items.Add(t.DisplayName);
            Row(look, Strings.Theme, themeBox);
            foreach (var size in IconSizes)
                iconSizeBox.Items.Add($"{size} px");
            if (!isWidget)
                Row(look, Strings.IconSize, iconSizeBox);
            Row(look, Strings.TitleHeight, titleHeight);
            Row(look, Strings.Background, colorButton);
            var opacityRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            opacityRow.Controls.Add(opacity);
            opacityRow.Controls.Add(opacityValue);
            Row(look, Strings.Opacity, opacityRow);
            if (info.Kind == FenceKind.Links)
                Wide(look, compactBox);

            var behavior = Section(left, Strings.SectionBehavior, ColumnWidth);
            foreach (var mode in Enum.GetValues<FenceSortMode>())
                sortBox.Items.Add(Strings.SortModeName(mode));
            if (hasItems)
                Row(behavior, Strings.SortBy, sortBox);
            Wide(behavior, lockedBox);
            Wide(behavior, collapseBox);
            Wide(behavior, onTopBox);

            if (hasItems)
            {
                var sort = Section(left, Strings.SectionAutoSort, ColumnWidth);
                presetBox.Items.Add(Strings.AddPreset);
                foreach (var preset in AutoSorter.Presets)
                    presetBox.Items.Add(preset.Name());
                presetBox.SelectedIndex = 0;
                Row(sort, Strings.AutoSort, autoSortBox, presetBox);
                Hint(sort, Strings.AutoSortHint, ColumnWidth);
            }

            // ── right column: live preview ──
            var right = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
            right.Controls.Add(new Label { Text = Strings.Preview, AutoSize = true, Font = HeaderFont(Font), Margin = new Padding(0, 0, 0, 10) });
            right.Controls.Add(preview);

            var columns = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill };
            columns.Controls.Add(left);
            columns.Controls.Add(right);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(12, 2, 12, 2) };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 16, 0, 0) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(columns);
            Controls.Add(buttons);

            // ── values ──
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
            compactBox.Checked = info.Compact;
            lockedBox.Checked = info.Locked;
            collapseBox.Checked = info.CanMinify;
            onTopBox.Checked = info.AlwaysOnTop;
            autoSortBox.Text = info.AutoSortPatterns ?? "";
            sortBox.SelectedIndex = (int)info.SortMode;
            UpdateColorButton();
            UpdateOpacityLabel();
            UpdateEnabled();

            // ── events ──
            kindBox.SelectedIndexChanged += (_, _) => UpdateEnabled();
            themeBox.SelectedIndexChanged += (_, _) => UpdateEnabled();
            opacity.ValueChanged += (_, _) => UpdateOpacityLabel();
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
                    SchedulePreview();
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

            // Any change refreshes the preview (debounced)
            foreach (var c in new Control[] { nameBox, kindBox, folderBox, themeBox, titleHeight, iconSizeBox, opacity, compactBox, sortBox })
            {
                switch (c)
                {
                    case TextBox tb: tb.TextChanged += (_, _) => SchedulePreview(); break;
                    case ComboBox cb: cb.SelectedIndexChanged += (_, _) => SchedulePreview(); break;
                    case NumericUpDown nu: nu.ValueChanged += (_, _) => SchedulePreview(); break;
                    case TrackBar tr: tr.ValueChanged += (_, _) => SchedulePreview(); break;
                    case CheckBox ch: ch.CheckedChanged += (_, _) => SchedulePreview(); break;
                }
            }
            previewTimer.Tick += (_, _) =>
            {
                previewTimer.Stop();
                RenderPreview();
                iconsTimer.Start(); // icons load in the background; draw once more when they are there
            };
            iconsTimer.Tick += (_, _) =>
            {
                iconsTimer.Stop();
                RenderPreview();
            };
            Shown += (_, _) => SchedulePreview();
        }

        private FenceTheme? SelectedTheme => themeBox.SelectedIndex > 0 ? ThemeRegistry.All[themeBox.SelectedIndex - 1] : null;

        private void SchedulePreview()
        {
            previewTimer.Stop();
            previewTimer.Start();
        }

        /// <summary>Draws the fence as it would look with the current choices, on a neutral backdrop.</summary>
        private void RenderPreview()
        {
            try
            {
                var copy = JsonSerializer.Deserialize<FenceInfo>(JsonSerializer.Serialize(original, FenceStore.JsonOptions), FenceStore.JsonOptions)!;
                ApplyTo(copy);
                var size = new Size(Math.Clamp(copy.Width, 160, 420), Math.Clamp(copy.Height, 120, 380));
                using var window = new FenceWindow(new PreviewRenderer.Host(), copy) { Size = size };
                window.ApplySettings();
                window.ReloadEntries();

                const int margin = 16;
                var bmp = new Bitmap(size.Width + 2 * margin, size.Height + 2 * margin, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    PreviewRenderer.DrawBackdrop(g, new Rectangle(Point.Empty, bmp.Size));
                    g.TranslateTransform(margin, margin);
                    window.PaintFence(g);
                }
                var old = preview.Image;
                preview.Image = bmp;
                old?.Dispose();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Preview: {e.Message}");
            }
        }

        private void UpdateEnabled()
        {
            var folder = kindBox.SelectedIndex == 1;
            folderBox.Enabled = browseButton.Enabled = folder;
            // Only the glass style is tinted with a custom color; the others have fixed palettes.
            colorButton.Enabled = SelectedTheme?.UsesCustomColor ?? true;
            compactBox.Enabled = kindBox.SelectedIndex == 0;
        }

        private void UpdateColorButton()
        {
            colorButton.BackColor = backgroundColor;
            colorButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
        }

        private void UpdateOpacityLabel() => opacityValue.Text = $"{opacity.Value * 100 / 255} %";

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
            if (kind != info.Kind || !string.Equals(info.FolderPath, folderBox.Text, StringComparison.OrdinalIgnoreCase) && kind == FenceKind.Folder)
                info.Files.Clear(); // the stored order belongs to the old content
            info.Kind = kind;
            info.FolderPath = kind == FenceKind.Folder ? folderBox.Text : null;
            info.Theme = SelectedTheme?.Id;
            info.TitleHeight = (int)titleHeight.Value;
            info.IconSize = IconSizes[Math.Max(0, iconSizeBox.SelectedIndex)];
            info.BackgroundColor = backgroundColor.ToArgb() & 0xFFFFFF;
            info.BackgroundAlpha = opacity.Value;
            info.Compact = kind == FenceKind.Links && compactBox.Checked;
            info.Locked = lockedBox.Checked;
            info.CanMinify = collapseBox.Checked;
            info.AlwaysOnTop = onTopBox.Checked;
            info.AutoSortPatterns = string.IsNullOrWhiteSpace(autoSortBox.Text) ? null : autoSortBox.Text.Trim();
            info.SortMode = (FenceSortMode)Math.Max(0, sortBox.SelectedIndex);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                previewTimer.Dispose();
                iconsTimer.Dispose();
                preview.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
