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
            using var back = new SolidBrush(selected ? Color.FromArgb(40, 0, 120, 212) : nav.BackColor);
            e.Graphics.FillRectangle(back, e.Bounds);
            if (selected)
            {
                using var bar = new SolidBrush(Color.FromArgb(0, 120, 212));
                e.Graphics.FillRectangle(bar, e.Bounds.X, e.Bounds.Y + 6, 4, e.Bounds.Height - 12);
            }
            TextRenderer.DrawText(e.Graphics, pages[e.Index].Title, selected ? new Font(Font, FontStyle.Bold) : Font,
                new Rectangle(e.Bounds.X + 16, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height), nav.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

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
            Row(grid, Strings.LanguageLabel, Choice(Strings.Languages.Select(Strings.LanguageName), IndexOf(Strings.Languages, Config.Language), i =>
            {
                Config.Language = Strings.Languages[i];
                Strings.Language = Config.Language;
                app.Store.RequestSave();
                app.ApplyToAll();
                // Rebuild this window in the new language
                var selected = nav.SelectedIndex;
                Text = Strings.SettingsTitle;
                for (var p = 0; p < pages.Count; p++)
                    pages[p] = (new[] { Strings.SectionGeneral, Strings.SectionDesktop, Strings.SectionUpdates, Strings.SectionFps, Strings.SectionData }[p], pages[p].Build);
                nav.Items.Clear();
                nav.Items.AddRange(pages.Select(x => (object)x.Title).ToArray());
                BeginInvoke(() => nav.SelectedIndex = selected);
            }));
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
            Wide(grid, Check(Strings.Animations, Config.Animations, v =>
            {
                Config.Animations = v;
                app.Store.RequestSave();
            }));
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

            var sort = Section(page, Strings.SectionAutoSort, ContentWidth);
            Wide(sort, Check(Strings.AutoSortEnabled, Config.AutoSortEnabled, v =>
            {
                Config.AutoSortEnabled = v;
                app.Store.RequestSave();
            }));
            Hint(sort, Strings.AutoSortHint, ContentWidth);
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
                Row(grid, Strings.RestoreBackup, pick, Action(Strings.RestoreBackup, () =>
                {
                    var b = backups[Math.Max(0, pick.SelectedIndex)];
                    app.RestoreBackup(b.Path, b.Time);
                }));
            }
            else
            {
                Hint(grid, Strings.NoBackups, ContentWidth);
            }

            var styles = Section(page, Strings.CustomThemes, ContentWidth);
            Wide(styles, Action(Strings.OpenThemesFolder, () => Open(app.ThemesFolder)));
            Wide(styles, Action(Strings.ReloadThemes, () =>
            {
                app.LoadCustomThemes(report: true);
                app.ApplyToAll();
            }));
            Wide(styles, Action(Strings.OpenDataFolder, () => Open(app.Store.DataDirectory)));
        }

        #endregion

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
