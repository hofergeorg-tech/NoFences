using System.Diagnostics;
using System.Drawing.Drawing2D;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>Owns the config, all fence windows and the tray icon. The app lives until "Exit".</summary>
    public sealed partial class NoFencesApp : ApplicationContext, IFenceHost
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
            var firstStart = Store.Config.Fences.Count == 0;
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
            tray.BalloonTipClicked += (_, _) => balloonAction?.Invoke();
            InitPeek();
            InitUpdates();
            InitReminders();
            InitBackupsAndScreens();

            if (firstStart)
                ShowBalloon(Strings.FirstStartHint, timeout: 8000);
            ShowChangelogAfterUpdate(firstStart);
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

        private Action? balloonAction;

        /// <summary>Tray notification; <paramref name="onClick"/> runs if the user clicks it.</summary>
        private void ShowBalloon(string text, Action? onClick = null, int timeout = 4000)
        {
            balloonAction = onClick;
            tray.ShowBalloonTip(timeout, "NoFences", text, ToolTipIcon.Info);
        }

        private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        public static void AddDocumentItems(ToolStripItemCollection items)
        {
            items.Add(Strings.Help, null, (_, _) => DocumentViewer.ShowDocument(Strings.HelpDocument, Strings.Help));
            items.Add(Strings.WhatsNew, null, (_, _) => DocumentViewer.ShowDocument(Strings.ChangelogDocument, Strings.WhatsNew));
        }

        /// <summary>Shows the changelog once after an update (not on the very first start).</summary>
        private void ShowChangelogAfterUpdate(bool firstStart)
        {
            var version = typeof(NoFencesApp).Assembly.GetName().Version?.ToString(3) ?? "";
            if (Store.Config.LastSeenVersion == version)
                return;
            var show = !firstStart;
            Store.Config.LastSeenVersion = version;
            Store.RequestSave();
            if (show)
                DocumentViewer.ShowDocument(Strings.ChangelogDocument, Strings.WhatsNew);
        }

        public static void ToggleAutostart()
        {
            try
            {
                SystemSettings.AutostartEnabled = !SystemSettings.AutostartEnabled;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NoFences");
            }
        }

        private void SortDesktopNow()
        {
            if (!Store.Config.Fences.Any(f => !string.IsNullOrWhiteSpace(f.AutoSortPatterns)))
            {
                ShowBalloon(Strings.SortNowNoRules);
                return;
            }
            var count = sorter.SortDesktopNow();
            ShowBalloon(Strings.SortNowDone(count));
        }

        public FenceTheme ThemeFor(FenceInfo info) => ThemeRegistry.Get(info.Theme ?? Store.Config.Theme);

        public void RequestSave() => Store.RequestSave();

        public IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except) =>
            windows.Where(w => w != except && w.Visible).Select(w => w.SurfaceOnScreen).ToList();

        public void CreateFence(FenceKind kind, string? name = null)
        {
            var info = new FenceInfo
            {
                Name = name ?? Strings.NewFence,
                Kind = kind
            };

            if (kind == FenceKind.Note)
            {
                info.Name = name ?? Strings.NoteName;
                info.Theme = "postit";
                info.Width = 260;
                info.Height = 240;
                info.TitleHeight = 30;
            }

            if (kind == FenceKind.Folder)
            {
                using var dlg = new FolderBrowserDialog { Description = Strings.ChooseFolder, UseDescriptionForTitle = true };
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;
                info.FolderPath = dlg.SelectedPath;
                info.Name = name ?? Path.GetFileName(dlg.SelectedPath.TrimEnd('\\'));
            }

            PlaceNearCursor(info);
            Store.Config.Fences.Add(info);
            Store.RequestSave();
            OpenWindow(info);
        }

        /// <summary>
        /// New fences appear where the user is working (the monitor under the mouse), not always on
        /// the primary monitor, which may be covered by a full-screen game.
        /// </summary>
        private static void PlaceNearCursor(FenceInfo info)
        {
            var cursor = Cursor.Position;
            var area = Screen.FromPoint(cursor).WorkingArea;
            var x = cursor.X - info.Width / 2;
            var y = cursor.Y - 20;
            info.PosX = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - info.Width));
            info.PosY = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - info.Height));
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
            if (availableUpdate != null)
            {
                var update = new ToolStripMenuItem(Strings.InstallUpdate(availableUpdate.Version), null, (_, _) => InstallUpdate());
                update.Font = new Font(update.Font, FontStyle.Bold);
                menu.Items.Add(update);
                menu.Items.Add(new ToolStripSeparator());
            }
            menu.Items.Add(Strings.NewFence, null, (_, _) => CreateFence(FenceKind.Links));
            menu.Items.Add(Strings.NewFolderFence, null, (_, _) => CreateFence(FenceKind.Folder));
            menu.Items.Add(Strings.NewNote, null, (_, _) => CreateFence(FenceKind.Note));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.ShowFences, null, (_, _) => ToggleVisible()) { Checked = fencesVisible });
            menu.Items.Add(new ToolStripMenuItem(Strings.DoubleClickToggle, null, (_, _) =>
            {
                Store.Config.DesktopDoubleClickToggle = !Store.Config.DesktopDoubleClickToggle;
                Store.RequestSave();
                UpdateDesktopHook();
            }) { Checked = Store.Config.DesktopDoubleClickToggle });
            AddPeekItems(menu.Items);
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

            menu.Items.Add(new ToolStripMenuItem(Strings.Autostart, null, (_, _) => ToggleAutostart()) { Checked = SystemSettings.AutostartEnabled });

            menu.Items.Add(Strings.OpenDataFolder, null, (_, _) =>
                Process.Start(new ProcessStartInfo(Store.DataDirectory) { UseShellExecute = true }));
            menu.Items.Add(new ToolStripSeparator());
            AddUpdateItems(menu.Items);
            AddBackupItems(menu.Items);
            AddDocumentItems(menu.Items);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.Exit, null, (_, _) => ExitThread());
        }

        protected override void ExitThreadCore()
        {
            if (!skipSaveOnExit)
                Store.SaveNow();
            desktopHook?.Dispose();
            DisposePeek();
            DisposeUpdates();
            DisposeReminders();
            DisposeBackupsAndScreens();
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
