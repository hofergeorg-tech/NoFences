using System.Diagnostics;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;
using static NoFences.SettingsKit;

namespace NoFences
{
    /// <summary>
    /// App-wide settings in one window with a section list on the left. Changes apply immediately;
    /// there is nothing to confirm.
    /// </summary>
    internal sealed class AppSettingsDialog : Form
    {
        private const int ContentWidth = 470;

        private static AppSettingsDialog? open;
        private readonly NoFencesApp app;
        private readonly ListBox nav = new() { Dock = DockStyle.Left, Width = 170, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false };
        private readonly Panel content = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24, 16, 16, 16) };
        private readonly List<(string Title, Action<FlowLayoutPanel> Build)> pages;

        private AppConfig Config => app.Store.Config;

        public static void ShowSingle(NoFencesApp app)
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new AppSettingsDialog(app);
            open.Show();
            open.Activate();
        }

        private AppSettingsDialog(NoFencesApp app)
        {
            this.app = app;
            Text = Strings.SettingsTitle;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont ?? Font;
            ClientSize = new Size(700, 520);
            MinimumSize = new Size(560, 400);
            MaximizeBox = false;
            ShowInTaskbar = true;
            try
            {
                using var stream = typeof(AppSettingsDialog).Assembly.GetManifestResourceStream("NoFences.ico");
                if (stream != null)
                    Icon = new Icon(stream);
            }
            catch { }

            pages = new()
            {
                (Strings.SectionGeneral, BuildGeneral),
                (Strings.SectionDesktop, BuildDesktop),
                (Strings.SectionAutomation, BuildAutomation),
                (Strings.SectionUpdates, BuildUpdates),
                (Strings.SectionFps, BuildFps),
                (Strings.SectionData, BuildData),
            };

            nav.ItemHeight = (int)(Font.Height * 2.2f);
            nav.Items.AddRange(pages.Select(p => (object)p.Title).ToArray());
            nav.DrawItem += DrawNavItem;
            nav.SelectedIndexChanged += (_, _) => ShowPage(nav.SelectedIndex);

            var close = new Button { Text = Strings.Close, AutoSize = true, Padding = new Padding(12, 2, 12, 2) };
            close.Click += (_, _) => Close();
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(12) };
            footer.Controls.Add(close);
            CancelButton = close;

            Controls.Add(content);
            Controls.Add(new Panel { Dock = DockStyle.Left, Width = 1, BackColor = SystemColors.ControlDark });
            Controls.Add(nav);
            Controls.Add(footer);
            nav.SelectedIndex = 0;
        }

        private void DrawNavItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;
            var selected = (e.State & DrawItemState.Selected) != 0;
            var accent = Color.FromArgb(0, 120, 212);
            // Opaque colors only: a translucent fill would add up on every repaint until the text is unreadable
            var back = selected ? Blend(nav.BackColor, accent, 0.18) : nav.BackColor;
            using (var brush = new SolidBrush(back))
                e.Graphics.FillRectangle(brush, e.Bounds);
            if (selected)
            {
                using var bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, e.Bounds.X, e.Bounds.Y + 6, 4, e.Bounds.Height - 12);
            }
            using var font = selected ? new Font(Font, FontStyle.Bold) : null;
            TextRenderer.DrawText(e.Graphics, pages[e.Index].Title, font ?? Font,
                new Rectangle(e.Bounds.X + 16, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height), nav.ForeColor, back,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private static Color Blend(Color a, Color b, double amount) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * amount), (int)(a.G + (b.G - a.G) * amount), (int)(a.B + (b.B - a.B) * amount));

        private void ShowPage(int index)
        {
            if (index < 0)
                return;
            content.SuspendLayout();
            foreach (Control c in content.Controls.Cast<Control>().ToList())
                c.Dispose();
            content.Controls.Clear();
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top };
            pages[index].Build(page);
            content.Controls.Add(page);
            content.ResumeLayout();
        }

        #region Pages

        private void BuildGeneral(FlowLayoutPanel page)
        {
            var grid = Section(page, Strings.SectionGeneral, ContentWidth);
            Row(grid, Strings.LanguageLabel, WithFlags(Choice(Strings.Languages.Select(Strings.LanguageName), IndexOf(Strings.Languages, Config.Language), i =>
            {
                Config.Language = Strings.Languages[i];
                app.ReloadLanguages();
                Strings.Language = Config.Language;
                app.Store.RequestSave();
                app.ApplyToAll();
                // Rebuild this window in the new language
                var selected = nav.SelectedIndex;
                Text = Strings.SettingsTitle;
                for (var p = 0; p < pages.Count; p++)
                    pages[p] = (new[] { Strings.SectionGeneral, Strings.SectionDesktop, Strings.SectionAutomation, Strings.SectionUpdates, Strings.SectionFps, Strings.SectionData }[p], pages[p].Build);
                nav.Items.Clear();
                nav.Items.AddRange(pages.Select(x => (object)x.Title).ToArray());
                BeginInvoke(() => nav.SelectedIndex = selected);
            })));
            Wide(grid, Action(Strings.OwnTranslations, app.OpenLanguageFolder));
            Wide(grid, Check(Strings.Autostart, SystemSettings.AutostartEnabled, _ => NoFencesApp.ToggleAutostart()));

            var ext = new[] { (bool?)null, true, false };
            Row(grid, Strings.ShowExtensions, Choice(new[] { Strings.ExtFollowExplorer, Strings.ExtAlways, Strings.ExtNever }, Array.IndexOf(ext, Config.ShowExtensions), i =>
            {
                Config.ShowExtensions = ext[i];
                app.ApplyToAll();
            }));

            var themes = ThemeRegistry.All.ToList();
            Row(grid, Strings.ThemeGlobal, Choice(themes.Select(t => t.DisplayName), themes.FindIndex(t => t.Id == ThemeRegistry.Get(Config.Theme).Id), i =>
            {
                Config.Theme = themes[i].Id;
                app.ApplyToAll();
            }));
            if (Config.AutoTheme != AutoThemeMode.Off)
                Hint(grid, Strings.ThemeGlobalAutoHint, ContentWidth);
            Wide(grid, Check(Strings.Animations, Config.Animations, v =>
            {
                Config.Animations = v;
                app.Store.RequestSave();
            }));
        }

        private void BuildAutomation(FlowLayoutPanel page)
        {
            // Profile rules
            var rules = Section(page, Strings.SectionProfileRules, ContentWidth);
            Hint(rules, Strings.RulesHint, ContentWidth);
            var list = new ListBox { Width = ContentWidth, Height = 110, IntegralHeight = false };
            void Fill()
            {
                list.Items.Clear();
                list.Items.AddRange(Config.ProfileRules.Select(r => (object)ProfileRuleDialog.Describe(r)).ToArray());
            }
            Fill();
            Wide(rules, list);
            var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            buttons.Controls.Add(Action(Strings.RuleAdd, () =>
            {
                if (app.Profiles.Count == 0 && app.NewProfile(this) == null)
                    return;
                using var dialog = new ProfileRuleDialog(app.Profiles, null);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                Config.ProfileRules.Add(dialog.Rule);
                app.Store.RequestSave();
                Fill();
            }));
            buttons.Controls.Add(Action(Strings.RuleEdit, () =>
            {
                if (list.SelectedIndex < 0)
                    return;
                using var dialog = new ProfileRuleDialog(app.Profiles, Config.ProfileRules[list.SelectedIndex]);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                Config.ProfileRules[list.SelectedIndex] = dialog.Rule;
                app.Store.RequestSave();
                Fill();
            }));
            buttons.Controls.Add(Action(Strings.RuleRemove, () =>
            {
                if (list.SelectedIndex < 0)
                    return;
                Config.ProfileRules.RemoveAt(list.SelectedIndex);
                app.Store.RequestSave();
                Fill();
            }));
            list.DoubleClick += (_, _) => ((Button)buttons.Controls[1]).PerformClick();
            Wide(rules, buttons);

            // Full screen
            // Break reminder: interval and an optional own text
            var pause = Section(page, Strings.SectionBreaks, ContentWidth);
            var choices = AppConfig.BreakReminderChoices;
            Row(pause, Strings.BreakEvery, Choice(choices.Select(m => m == 0 ? Strings.HotkeyName("Off") : $"{m} min"), Array.IndexOf(choices, Config.BreakReminderMinutes), i =>
            {
                Config.BreakReminderMinutes = choices[i];
                app.Store.RequestSave();
            }, 180));
            var text = new TextBox { Width = 260, Text = Config.BreakReminderText ?? "", PlaceholderText = Strings.BreakDefaultText };
            text.TextChanged += (_, _) =>
            {
                Config.BreakReminderText = text.Text.Trim().Length > 0 ? text.Text.Trim() : null;
                app.Store.RequestSave();
            };
            Row(pause, Strings.BreakTextLabel, text);
            Hint(pause, Strings.BreakHint, ContentWidth);

            var fullscreen = Section(page, Strings.SectionFullscreen, ContentWidth);
            Wide(fullscreen, Check(Strings.HideOnFullscreen, Config.HideOnFullscreen, v =>
            {
                Config.HideOnFullscreen = v;
                app.Store.RequestSave();
            }));
            Hint(fullscreen, Strings.HideOnFullscreenHint, ContentWidth);

            // Light/dark style
            var style = Section(page, Strings.SectionAutoTheme, ContentWidth);
            var themes = ThemeRegistry.All.ToList();
            var modes = Enum.GetValues<AutoThemeMode>();
            var light = Choice(themes.Select(t => t.DisplayName), themes.FindIndex(t => t.Id == ThemeRegistry.Get(Config.LightTheme).Id), i =>
            {
                Config.LightTheme = themes[i].Id;
                app.ApplyToAll();
            });
            var dark = Choice(themes.Select(t => t.DisplayName), themes.FindIndex(t => t.Id == ThemeRegistry.Get(Config.DarkTheme).Id), i =>
            {
                Config.DarkTheme = themes[i].Id;
                app.ApplyToAll();
            });
            DateTimePicker Time(string value, Action<string> changed)
            {
                var picker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 80, Value = DateTime.Today + ProfileRule.ParseTime(value) };
                picker.ValueChanged += (_, _) =>
                {
                    changed(picker.Value.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture));
                    app.ApplyToAll();
                };
                return picker;
            }
            var from = Time(Config.DarkFrom, v => Config.DarkFrom = v);
            var to = Time(Config.DarkTo, v => Config.DarkTo = v);
            void UpdateEnabled()
            {
                light.Enabled = dark.Enabled = Config.AutoTheme != AutoThemeMode.Off;
                from.Enabled = to.Enabled = Config.AutoTheme == AutoThemeMode.Time;
            }
            Row(style, Strings.AutoThemeLabel, Choice(modes.Select(Strings.AutoThemeModeName), Array.IndexOf(modes, Config.AutoTheme), i =>
            {
                Config.AutoTheme = modes[i];
                app.ApplyToAll();
                UpdateEnabled();
            }));
            Row(style, Strings.LightThemeLabel, light);
            Row(style, Strings.DarkThemeLabel, dark);
            Row(style, Strings.DarkTimesLabel, from, to);
            UpdateEnabled();
            Hint(style, Strings.AutoThemeHint, ContentWidth);

            // Wallpaper by time of day
            var wallpapers = Section(page, Strings.SectionTimedWallpaper, ContentWidth);
            var plan = new ListBox { Width = ContentWidth, Height = 90, IntegralHeight = false };
            void FillPlan()
            {
                plan.Items.Clear();
                foreach (var w in Config.TimedWallpapers.OrderBy(w => ProfileRule.ParseTime(w.From)))
                    plan.Items.Add($"{Strings.FromTime(w.From)}   {Path.GetFileName(w.Image)}");
            }
            FillPlan();
            Wide(wallpapers, plan);
            var at = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 80, Value = DateTime.Today.AddHours(7) };
            var planButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            planButtons.Controls.Add(Action(Strings.TimedWallpaperAdd, () =>
                {
                    using var dialog = new OpenFileDialog { Filter = Strings.WallpaperFilter, InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) };
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;
                    var from = at.Value.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    Config.TimedWallpapers.RemoveAll(w => w.From == from);
                    Config.TimedWallpapers.Add(new TimedWallpaper { From = from, Image = dialog.FileName });
                    app.Store.RequestSave();
                    FillPlan();
                }));
            planButtons.Controls.Add(Action(Strings.RuleRemove, () =>
                {
                    if (plan.SelectedIndex < 0)
                        return;
                    var sorted = Config.TimedWallpapers.OrderBy(w => ProfileRule.ParseTime(w.From)).ToList();
                    Config.TimedWallpapers.Remove(sorted[plan.SelectedIndex]);
                    app.Store.RequestSave();
                    FillPlan();
                }));
            Row(wallpapers, Strings.TimedWallpaperFrom, at, planButtons);
            Hint(wallpapers, Strings.TimedWallpaperHint, ContentWidth);
        }

        private void BuildDesktop(FlowLayoutPanel page)
        {
            var grid = Section(page, Strings.SectionDesktop, ContentWidth);
            Wide(grid, Check(Strings.DoubleClickToggle, Config.DesktopDoubleClickToggle, v =>
            {
                Config.DesktopDoubleClickToggle = v;
                app.Store.RequestSave();
                app.UpdateDesktopHook();
            }));
            Row(grid, $"{Strings.PeekMenu}:", Choice(AppConfig.PeekHotkeys.Select(Strings.HotkeyName), IndexOf(AppConfig.PeekHotkeys, Config.PeekHotkey), i =>
            {
                Config.PeekHotkey = AppConfig.PeekHotkeys[i];
                app.Store.RequestSave();
                app.UpdateHotkey(notifyIfTaken: true);
            }, 180));
            Wide(grid, Check(Strings.FadeFences, Config.FadeFences, app.SetFadeFences));
            Hint(grid, Strings.FadeFencesHint, ContentWidth);
            Wide(grid, Check(Strings.HoverPreviewSetting, Config.HoverPreview, v =>
            {
                Config.HoverPreview = v;
                app.Store.RequestSave();
            }));

            var profiles = Section(page, Strings.SectionProfiles, ContentWidth);
            var names = new List<string?> { null };
            names.AddRange(app.Profiles);
            Row(profiles, Strings.ProfileLabel, Choice(names.Select(n => n ?? Strings.ProfileAll), names.IndexOf(app.ActiveProfile), i => app.SwitchProfile(names[i]), 180),
                Action(Strings.ProfileNew, () =>
                {
                    if (app.NewProfile(this) is { } name)
                    {
                        app.SwitchProfile(name);
                        ShowPage(nav.SelectedIndex);
                    }
                }));
            Hint(profiles, Strings.ProfileHowTo, ContentWidth);
            Wide(profiles, Check(Strings.ProfileHotkeysLabel, Config.ProfileHotkeys, v =>
            {
                Config.ProfileHotkeys = v;
                app.Store.RequestSave();
                app.UpdateProfileHotkeys();
            }));
            Hint(profiles, Strings.ProfileWallpaperHint, ContentWidth);

            var search = Section(page, Strings.SectionSearch, ContentWidth);
            Row(search, Strings.SearchHotkeyLabel, Choice(AppConfig.SearchHotkeys.Select(Strings.HotkeyName), IndexOf(AppConfig.SearchHotkeys, Config.SearchHotkey), i =>
            {
                Config.SearchHotkey = AppConfig.SearchHotkeys[i];
                app.Store.RequestSave();
                app.UpdateSearchHotkey(notifyIfTaken: true);
            }, 180));
            Hint(search, Strings.SearchHint, ContentWidth);
            Row(search, Strings.QuickNoteLabel, Choice(AppConfig.QuickNoteHotkeys.Select(Strings.HotkeyName), IndexOf(AppConfig.QuickNoteHotkeys, Config.QuickNoteHotkey), i =>
            {
                Config.QuickNoteHotkey = AppConfig.QuickNoteHotkeys[i];
                app.Store.RequestSave();
                app.UpdateQuickNoteHotkey();
            }, 180));

            var sort = Section(page, Strings.SectionAutoSort, ContentWidth);
            Wide(sort, Check(Strings.AutoSortEnabled, Config.AutoSortEnabled, v =>
            {
                Config.AutoSortEnabled = v;
                app.Store.RequestSave();
            }));
            Hint(sort, Strings.AutoSortHint, ContentWidth);
            Wide(sort, Action(Strings.AssistantMenu, app.RunDesktopAssistant));
            Wide(sort, Action(Strings.SortNow, app.SortDesktopNow));
        }

        private void BuildUpdates(FlowLayoutPanel page)
        {
            var grid = Section(page, Strings.SectionUpdates, ContentWidth);
            Hint(grid, Strings.VersionLabel(UpdateChecker.CurrentVersion), ContentWidth);
            Wide(grid, Check(Strings.CheckForUpdatesAuto, Config.CheckForUpdates, v =>
            {
                Config.CheckForUpdates = v;
                app.Store.RequestSave();
            }));
            Wide(grid, Action(Strings.CheckForUpdatesNow, async () => await app.CheckForUpdatesAsync(manual: true)));
            Wide(grid, Action(Strings.WhatsNew, () => DocumentViewer.ShowDocument(Strings.ChangelogDocument, Strings.WhatsNew)));
            if (AboutDialog.CanDonate)
            {
                Hint(grid, Strings.DonateHint, ContentWidth);
                Wide(grid, Action("♥ " + Strings.Donate, AboutDialog.OpenDonate));
            }
        }

        private void BuildFps(FlowLayoutPanel page)
        {
            var grid = Section(page, Strings.SectionFps, ContentWidth);
            Hint(grid, Strings.FpsShortHint, ContentWidth);
            CheckBox? box = null;
            box = Check(Strings.FpsEnabledLabel, app.FpsEnabled, _ =>
            {
                // ToggleFps explains the admin rights and may be declined; show the real state afterwards.
                if (box!.Checked != app.FpsEnabled)
                    app.ToggleFps();
                BeginInvoke(() => box.Checked = app.FpsEnabled);
            });
            Wide(grid, box);
        }

        private void BuildData(FlowLayoutPanel page)
        {
            var grid = Section(page, Strings.SectionData, ContentWidth);
            Wide(grid, Action(Strings.ExportFences, app.ExportFences));
            Wide(grid, Action(Strings.ImportFences, app.ImportFences));

            var backups = app.Store.ListBackups().Take(10).ToList();
            if (backups.Count > 0)
            {
                var times = backups.Select(b => b.Time.ToString("dd.MM.yyyy  HH:mm")).ToList();
                var pick = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
                pick.Items.AddRange(times.Cast<object>().ToArray());
                pick.SelectedIndex = 0;
                Row(grid, Strings.BackupLabel, pick, Action(Strings.RestoreShort, () =>
                {
                    var b = backups[Math.Max(0, pick.SelectedIndex)];
                    app.RestoreBackup(b.Path, b.Time);
                }));
            }
            else
            {
                Hint(grid, Strings.NoBackups, ContentWidth);
            }

            var sync = Section(page, Strings.SectionSync, ContentWidth);
            if (app.Store.SyncFolder is { } folder)
            {
                Hint(sync, Strings.SyncActive(folder), ContentWidth);
                Wide(sync, Action(Strings.SyncStop, () => app.StopSync(this)));
            }
            else
            {
                Hint(sync, Strings.SyncHint, ContentWidth);
                Wide(sync, Action(Strings.SyncChoose, () => app.ChooseSyncFolder(this)));
            }

            var styles = Section(page, Strings.CustomThemes, ContentWidth);
            Wide(styles, Action(Strings.DesignerMenu, () => app.OpenStyleDesigner(null)));
            Wide(styles, Action(Strings.OpenThemesFolder, () => Open(app.ThemesFolder)));
            Wide(styles, Action(Strings.ReloadThemes, () =>
            {
                app.LoadCustomThemes(report: true);
                app.ApplyToAll();
            }));
            Wide(styles, Action(Strings.OpenDataFolder, () => Open(app.Store.Folder.Root)));
        }

        #endregion

        /// <summary>Draws the language list with a flag in front of each name.</summary>
        private static ComboBox WithFlags(ComboBox box)
        {
            box.DrawMode = DrawMode.OwnerDrawFixed;
            box.ItemHeight = Math.Max(box.ItemHeight, box.Font.Height + 4);
            box.DrawItem += (_, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0)
                    return;
                var flagHeight = Math.Max(10, e.Bounds.Height - 8);
                var flag = Flags.For(Strings.Languages[e.Index], flagHeight);
                e.Graphics.DrawImage(flag, e.Bounds.X + 4, e.Bounds.Y + (e.Bounds.Height - flag.Height) / 2, flag.Width, flag.Height);
                var textX = e.Bounds.X + 4 + flagHeight * 3 / 2 + 8;
                TextRenderer.DrawText(e.Graphics, box.Items[e.Index]?.ToString(), e.Font ?? box.Font,
                    new Rectangle(textX, e.Bounds.Y, e.Bounds.Right - textX, e.Bounds.Height), e.ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                e.DrawFocusRectangle();
            };
            return box;
        }

        private static int IndexOf(IReadOnlyList<string> list, string value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                    return i;
            }
            return 0;
        }

        private static void Open(string folder)
        {
            try { Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true }); } catch { }
        }
    }
}
