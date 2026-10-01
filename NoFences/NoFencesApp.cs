using System.Diagnostics;
using System.Drawing.Drawing2D;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>Owns the config, all fence windows and the tray icon. The app lives until "Exit".</summary>
    public sealed class NoFencesApp : ApplicationContext
    {
        private readonly List<FenceWindow> windows = new();
        private readonly NotifyIcon tray;
        private readonly AutoSorter sorter;
        private DesktopDoubleClickHook? desktopHook;
        private bool fencesVisible = true;

        public FenceStore Store { get; } = new();

        public bool ShowExtensions => Store.Config.ShowExtensions ?? SystemSettings.ExplorerShowsExtensions;

        public NoFencesApp()
        {
            Store.Load();
            foreach (var info in Store.Config.Fences)
                OpenWindow(info);
            if (windows.Count == 0)
                CreateFence(FenceKind.Links, Strings.FirstFence);

            tray = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                Text = "NoFences",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };
            tray.ContextMenuStrip.Opening += (_, _) => BuildTrayMenu(tray.ContextMenuStrip);
            BuildTrayMenu(tray.ContextMenuStrip);
            tray.MouseDoubleClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleVisible(); };

            sorter = new AutoSorter(() => Store.Config, OnAutoSorted);
            UpdateDesktopHook();
        }

        private void UpdateDesktopHook()
        {
            if (Store.Config.DesktopDoubleClickToggle && desktopHook == null)
            {
                desktopHook = new DesktopDoubleClickHook();
                desktopHook.DoubleClicked += (_, _) => ToggleVisible();
            }
            else if (!Store.Config.DesktopDoubleClickToggle && desktopHook != null)
            {
                desktopHook.Dispose();
                desktopHook = null;
            }
        }

        private void OnAutoSorted(FenceInfo info)
        {
            Store.RequestSave();
            windows.FirstOrDefault(w => w.Info == info)?.ReloadEntries();
        }

        private void SortDesktopNow()
        {
            if (!Store.Config.Fences.Any(f => !string.IsNullOrWhiteSpace(f.AutoSortPatterns)))
            {
                tray.ShowBalloonTip(4000, "NoFences", Strings.SortNowNoRules, ToolTipIcon.Info);
                return;
            }
            var count = sorter.SortDesktopNow();
            tray.ShowBalloonTip(3000, "NoFences", Strings.SortNowDone(count), ToolTipIcon.Info);
        }

        public FenceTheme ThemeFor(FenceInfo info) => ThemeRegistry.Get(info.Theme ?? Store.Config.Theme);

        public void CreateFence(FenceKind kind, string? name = null)
        {
            var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
            var offset = 30 * (windows.Count % 8);
            var info = new FenceInfo
            {
                Name = name ?? Strings.NewFence,
                Kind = kind,
                PosX = area.Left + 100 + offset,
                PosY = area.Top + 150 + offset
            };

            if (kind == FenceKind.Folder)
            {
                using var dlg = new FolderBrowserDialog { Description = Strings.ChooseFolder, UseDescriptionForTitle = true };
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;
                info.FolderPath = dlg.SelectedPath;
                info.Name = name ?? Path.GetFileName(dlg.SelectedPath.TrimEnd('\\'));
            }

            Store.Config.Fences.Add(info);
            Store.RequestSave();
            OpenWindow(info);
        }

        public void RemoveFence(FenceWindow window)
        {
            Store.Config.Fences.Remove(window.Info);
            Store.SaveNow();
            windows.Remove(window);
            window.Close();
            window.Dispose();
        }

        private void OpenWindow(FenceInfo info)
        {
            var window = new FenceWindow(this, info);
            windows.Add(window);
            if (fencesVisible)
                window.Show();
        }

        private void ApplyToAll()
        {
            Store.RequestSave();
            foreach (var w in windows)
            {
                w.ApplySettings();
                w.ReloadEntries();
            }
        }

        private void ToggleVisible()
        {
            fencesVisible = !fencesVisible;
            foreach (var w in windows)
                w.Visible = fencesVisible;
        }

        private void BuildTrayMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();
            menu.Items.Add(Strings.NewFence, null, (_, _) => CreateFence(FenceKind.Links));
            menu.Items.Add(Strings.NewFolderFence, null, (_, _) => CreateFence(FenceKind.Folder));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.ShowFences, null, (_, _) => ToggleVisible()) { Checked = fencesVisible });
            menu.Items.Add(new ToolStripMenuItem(Strings.DoubleClickToggle, null, (_, _) =>
            {
                Store.Config.DesktopDoubleClickToggle = !Store.Config.DesktopDoubleClickToggle;
                Store.RequestSave();
                UpdateDesktopHook();
            }) { Checked = Store.Config.DesktopDoubleClickToggle });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.SortNow, null, (_, _) => SortDesktopNow());
            menu.Items.Add(new ToolStripMenuItem(Strings.AutoSortEnabled, null, (_, _) =>
            {
                Store.Config.AutoSortEnabled = !Store.Config.AutoSortEnabled;
                Store.RequestSave();
            }) { Checked = Store.Config.AutoSortEnabled });
            menu.Items.Add(new ToolStripSeparator());

            var style = new ToolStripMenuItem(Strings.ThemeGlobal);
            foreach (var t in ThemeRegistry.All)
            {
                style.DropDownItems.Add(new ToolStripMenuItem(t.DisplayName, null, (_, _) =>
                {
                    Store.Config.Theme = t.Id;
                    ApplyToAll();
                }) { Checked = ThemeRegistry.Get(Store.Config.Theme) == t });
            }
            menu.Items.Add(style);

            var ext = new ToolStripMenuItem(Strings.ShowExtensions);
            void ExtOption(string text, bool? value) => ext.DropDownItems.Add(new ToolStripMenuItem(text, null, (_, _) =>
            {
                Store.Config.ShowExtensions = value;
                ApplyToAll();
            }) { Checked = Store.Config.ShowExtensions == value });
            ExtOption(Strings.ExtFollowExplorer, null);
            ExtOption(Strings.ExtAlways, true);
            ExtOption(Strings.ExtNever, false);
            menu.Items.Add(ext);

            menu.Items.Add(new ToolStripMenuItem(Strings.Autostart, null, (_, _) =>
            {
                try { SystemSettings.AutostartEnabled = !SystemSettings.AutostartEnabled; }
                catch (Exception e) { MessageBox.Show(e.Message, "NoFences"); }
            }) { Checked = SystemSettings.AutostartEnabled });

            menu.Items.Add(Strings.OpenDataFolder, null, (_, _) =>
                Process.Start(new ProcessStartInfo(Store.DataDirectory) { UseShellExecute = true }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.Exit, null, (_, _) => ExitThread());
        }

        protected override void ExitThreadCore()
        {
            Store.SaveNow();
            desktopHook?.Dispose();
            sorter.Dispose();
            tray.Visible = false;
            tray.Dispose();
            foreach (var w in windows.ToList())
                w.Dispose();
            base.ExitThreadCore();
        }

        /// <summary>The app icon in tray size, picked from the embedded multi-size .ico.</summary>
        private static Icon CreateTrayIcon()
        {
            try
            {
                using var stream = typeof(NoFencesApp).Assembly.GetManifestResourceStream("NoFences.ico");
                if (stream != null)
                    return new Icon(stream, SystemInformation.SmallIconSize);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Tray icon: {e.Message}");
            }
            return SystemIcons.Application;
        }
    }
}
