using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text.Json;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Make your own style by clicking instead of editing JSON: colors, fonts, border and corners with a
    /// live preview. Saved as a JSON style in the styles folder, so it can still be shared or fine-tuned.
    /// </summary>
    public sealed class StyleDesignerDialog : Form
    {
        private readonly string themesFolder;
        private readonly Action afterSave;
        private readonly FenceInfo? target;
        private readonly List<string> sampleFiles;
        private JsonTheme.Definition def = new();
        private string? editingFile;
        private bool loading;

        private readonly ComboBox styleBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TextBox nameBox = new() { Width = 220 };
        private readonly Button backgroundButton = ColorButton();
        private readonly TrackBar backgroundAlpha = Slider();
        private readonly CheckBox glassBox = new() { Text = Strings.DesignerGlass, AutoSize = true };
        private readonly Button titleBackButton = ColorButton();
        private readonly TrackBar titleBackAlpha = Slider();
        private readonly Button titleColorButton = ColorButton();
        private readonly ComboBox titleFontBox = FontBox();
        private readonly ComboBox alignBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        private readonly CheckBox upperBox = new() { Text = Strings.DesignerUppercase, AutoSize = true };
        private readonly Button labelColorButton = ColorButton();
        private readonly ComboBox labelFontBox = FontBox();
        private readonly CheckBox shadowBox = new() { Text = Strings.DesignerShadow, AutoSize = true };
        private readonly Button accentButton = ColorButton();
        private readonly Button borderButton = ColorButton();
        private readonly NumericUpDown borderWidth = new() { Minimum = 0, Maximum = 8, Width = 60 };
        private readonly NumericUpDown corners = new() { Minimum = 0, Maximum = 24, Width = 60 };
        private readonly PictureBox preview = new() { Size = new Size(320, 300), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
        private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 120 };

        /// <param name="target">The fence the designer was opened from: "Save and use" applies the style to it.</param>
        /// <param name="afterSave">Reloads the styles and repaints the fences.</param>
        public StyleDesignerDialog(string themesFolder, FenceInfo? target, Action afterSave)
        {
            this.themesFolder = themesFolder;
            this.afterSave = afterSave;
            this.target = target;
            sampleFiles = target?.Files.Where(File.Exists).Take(8).ToList() is { Count: > 0 } own ? own : PreviewRenderer.SampleFiles();
            Text = Strings.DesignerTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 20, 0) };
            void Row(string label, params Control[] controls)
            {
                grid.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 7, 10, 0) });
                var flow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 2, 0, 2) };
                flow.Controls.AddRange(controls);
                grid.Controls.Add(flow);
            }
            Row(Strings.DesignerStart, styleBox);
            Row(Strings.Name, nameBox);
            Row(Strings.Background.TrimEnd(':') + ":", backgroundButton, backgroundAlpha, glassBox);
            Row(Strings.DesignerTitleBar, titleBackButton, titleBackAlpha);
            Row(Strings.DesignerTitleText, titleColorButton, titleFontBox);
            Row("", alignBox, upperBox);
            Row(Strings.DesignerLabels, labelColorButton, labelFontBox);
            Row("", shadowBox);
            Row(Strings.DesignerAccent, accentButton);
            Row(Strings.DesignerBorder, borderButton, borderWidth, new Label { Text = "px", AutoSize = true, Margin = new Padding(2, 7, 0, 0) });
            Row(Strings.DesignerCorners, corners, new Label { Text = "px", AutoSize = true, Margin = new Padding(2, 7, 0, 0) });

            alignBox.Items.AddRange(new object[] { Strings.DesignerAlignLeft, Strings.DesignerAlignCenter });

            var right = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            right.Controls.Add(new Label { Text = Strings.Preview, AutoSize = true, Font = SettingsKit.HeaderFont(Font), Margin = new Padding(0, 0, 0, 8) });
            right.Controls.Add(preview);
            var columns = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
            columns.Controls.Add(grid);
            columns.Controls.Add(right);

            var save = new Button { Text = Strings.DesignerSave, AutoSize = true };
            var apply = new Button { Text = Strings.DesignerSaveApply, AutoSize = true, Visible = target != null };
            var folder = new Button { Text = Strings.DesignerFolder, AutoSize = true };
            var cancel = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = cancel;
            save.Click += (_, _) => Save(applyToFence: false);
            apply.Click += (_, _) => Save(applyToFence: true);
            folder.Click += (_, _) =>
            {
                Directory.CreateDirectory(themesFolder);
                NoFencesApp.OpenUrl(themesFolder);
            };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 14, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, apply, save, folder });
            Controls.Add(columns);
            Controls.Add(buttons);

            FillStyleList();
            styleBox.SelectedIndexChanged += (_, _) => { if (!loading) LoadSelected(); };
            foreach (var (button, get, set) in new (Button, Func<Color>, Action<Color>)[]
                     {
                         (backgroundButton, () => Opaque(def.Background), c => def.Background = JsonTheme.Hex(c)),
                         (titleBackButton, () => Opaque(def.TitleBackground), c => def.TitleBackground = JsonTheme.Hex(Color.FromArgb(titleBackAlpha.Value, c))),
                         (titleColorButton, () => Opaque(def.TitleColor), c => def.TitleColor = JsonTheme.Hex(c)),
                         (labelColorButton, () => Opaque(def.LabelColor), c => def.LabelColor = JsonTheme.Hex(c)),
                         (accentButton, () => Opaque(def.Accent), c => def.Accent = JsonTheme.Hex(c)),
                         (borderButton, () => Opaque(def.Border), c => def.Border = JsonTheme.Hex(c)),
                     })
            {
                button.Click += (_, _) =>
                {
                    using var dialog = new ColorDialog { Color = get(), FullOpen = true };
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;
                    set(dialog.Color);
                    ShowValues();
                };
            }
            nameBox.TextChanged += (_, _) => Changed(() => def.Name = nameBox.Text.Trim());
            backgroundAlpha.ValueChanged += (_, _) => Changed(() => def.BackgroundAlpha = backgroundAlpha.Value);
            glassBox.CheckedChanged += (_, _) => Changed(() => def.Glass = glassBox.Checked);
            titleBackAlpha.ValueChanged += (_, _) => Changed(() => def.TitleBackground = JsonTheme.Hex(Color.FromArgb(titleBackAlpha.Value, Opaque(def.TitleBackground))));
            titleFontBox.SelectedIndexChanged += (_, _) => Changed(() => def.TitleFont = titleFontBox.Text);
            alignBox.SelectedIndexChanged += (_, _) => Changed(() => def.TitleAlign = alignBox.SelectedIndex == 0 ? "left" : "center");
            upperBox.CheckedChanged += (_, _) => Changed(() => def.TitleUppercase = upperBox.Checked);
            labelFontBox.SelectedIndexChanged += (_, _) => Changed(() => def.LabelFont = labelFontBox.Text);
            shadowBox.CheckedChanged += (_, _) => Changed(() => def.LabelShadow = shadowBox.Checked ? "#000000B0" : "#00000000");
            borderWidth.ValueChanged += (_, _) => Changed(() => def.BorderWidth = (float)borderWidth.Value);
            corners.ValueChanged += (_, _) => Changed(() => def.CornerRadius = (int)corners.Value);
            previewTimer.Tick += (_, _) =>
            {
                previewTimer.Stop();
                RenderPreview();
            };
            styleBox.SelectedIndex = 0;
        }

        private static Button ColorButton() => new() { Width = 40, Height = 24, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 2, 6, 0) };

        private static TrackBar Slider() => new() { Minimum = 0, Maximum = 255, TickStyle = TickStyle.None, Width = 110, AutoSize = false, Height = 26 };

        private static ComboBox FontBox()
        {
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, MaxDropDownItems = 18 };
            using var fonts = new InstalledFontCollection();
            box.Items.AddRange(fonts.Families.Select(f => (object)f.Name).ToArray());
            return box;
        }

        private static Color Opaque(string hex)
        {
            try
            {
                return Color.FromArgb(255, JsonTheme.ParseColor(hex));
            }
            catch (FormatException)
            {
                return Color.Gray;
            }
        }

        private static int AlphaOf(string hex)
        {
            try
            {
                return JsonTheme.ParseColor(hex).A;
            }
            catch (FormatException)
            {
                return 255;
            }
        }

        /// <summary>"New style" plus the own styles from the styles folder.</summary>
        private void FillStyleList()
        {
            styleBox.Items.Clear();
            styleBox.Items.Add(Strings.DesignerNew);
            if (Directory.Exists(themesFolder))
                styleBox.Items.AddRange(Directory.EnumerateFiles(themesFolder, "*.json").Select(f => (object)Path.GetFileName(f)).ToArray());
        }

        private void LoadSelected()
        {
            editingFile = null;
            def = new JsonTheme.Definition { Name = Strings.DesignerNewName, Id = "" };
            if (styleBox.SelectedIndex > 0)
            {
                var file = Path.Combine(themesFolder, (string)styleBox.SelectedItem!);
                try
                {
                    def = JsonTheme.Parse(File.ReadAllText(file), "custom-" + Path.GetFileNameWithoutExtension(file).ToLowerInvariant()).Source;
                    editingFile = file;
                }
                catch (Exception e) when (e is IOException or JsonException or FormatException)
                {
                    MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            ShowValues();
        }

        /// <summary>Puts the definition into the controls (without triggering changes).</summary>
        private void ShowValues()
        {
            loading = true;
            nameBox.Text = def.Name;
            backgroundButton.BackColor = Opaque(def.Background);
            backgroundAlpha.Value = Math.Clamp(def.BackgroundAlpha, 0, 255);
            glassBox.Checked = def.Glass;
            titleBackButton.BackColor = Opaque(def.TitleBackground);
            titleBackAlpha.Value = AlphaOf(def.TitleBackground);
            titleColorButton.BackColor = Opaque(def.TitleColor);
            titleFontBox.SelectedItem = titleFontBox.Items.Contains(def.TitleFont) ? def.TitleFont : "Segoe UI";
            alignBox.SelectedIndex = def.TitleAlign.Equals("left", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            upperBox.Checked = def.TitleUppercase;
            labelColorButton.BackColor = Opaque(def.LabelColor);
            labelFontBox.SelectedItem = labelFontBox.Items.Contains(def.LabelFont) ? def.LabelFont : "Segoe UI";
            shadowBox.Checked = AlphaOf(def.LabelShadow) > 0;
            accentButton.BackColor = Opaque(def.Accent);
            borderButton.BackColor = Opaque(def.Border);
            borderWidth.Value = (decimal)Math.Clamp(def.BorderWidth, 0, 8);
            corners.Value = Math.Clamp(def.CornerRadius, 0, 24);
            loading = false;
            SchedulePreview();
        }

        private void Changed(Action change)
        {
            if (loading)
                return;
            change();
            SchedulePreview();
        }

        private void SchedulePreview()
        {
            previewTimer.Stop();
            previewTimer.Start();
        }

        private void RenderPreview()
        {
            try
            {
                var theme = JsonTheme.FromDefinition(def);
                var info = new FenceInfo { Name = def.Name.Length > 0 ? def.Name : Strings.DesignerNewName, Files = sampleFiles, Theme = "designer-preview" };
                var host = new PreviewRenderer.Host { ThemeOverride = _ => theme };
                var size = new Size(300, 240);
                using var window = new FenceWindow(host, info) { Size = size };
                window.ApplySettings();
                window.ReloadEntries();
                const int margin = 20;
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
                System.Diagnostics.Debug.WriteLine($"Designer preview: {e.Message}");
            }
        }

        private void Save(bool applyToFence)
        {
            if (def.Name.Length == 0)
                def.Name = Strings.DesignerNewName;
            if (string.IsNullOrWhiteSpace(def.Id) || editingFile == null)
                def.Id = UniqueId(JsonTheme.IdFor(def.Name));
            var file = editingFile ?? Path.Combine(themesFolder, def.Id + ".json");
            try
            {
                Directory.CreateDirectory(themesFolder);
                File.WriteAllText(file, JsonTheme.ToJson(def));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            editingFile = file;
            if (applyToFence && target != null)
                target.Theme = def.Id;
            // Fences already using this style pick up the changes too
            afterSave();
            // Keep the list in sync, with this style selected
            var name = Path.GetFileName(file);
            loading = true;
            FillStyleList();
            styleBox.SelectedItem = name;
            loading = false;
            Text = $"{Strings.DesignerTitle} – {Strings.DesignerSaved(def.Name)}";
        }

        /// <summary>Not the id of a built-in style or of another file.</summary>
        private string UniqueId(string id)
        {
            bool Taken(string c) => ThemeRegistry.All.Any(t => t.Id.Equals(c, StringComparison.OrdinalIgnoreCase))
                                    || File.Exists(Path.Combine(themesFolder, c + ".json"));
            var candidate = id;
            for (var i = 2; Taken(candidate); i++)
                candidate = $"{id}-{i}";
            return candidate;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                previewTimer.Dispose();
                preview.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
