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

        public bool Animations => Store.Config.Animations;

        public bool HoverPreview => Store.Config.HoverPreview;

        public NoFencesApp()
        {
            Store.Load();
            Log.Folder = Store.LocalDirectory;
            AppData.Folder = Store.DataDirectory;
            Strings.Language = Store.Config.Language;
            appliedTheme = DefaultThemeId;
            var themeErrors = LoadCustomThemesQuiet().Errors;
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
            InitFps();
            InitVirtualDesktops();
            InitPlaytime();
            InitAutomation();
            InitSearch();
            InitSync();
            InitScreenTime();
            InitFenceTools();
            UpdateProfileHotkeys();
            UpdateQuickNoteHotkey();
            if (themeErrors.Count > 0)
                ShowBalloon(Strings.ThemeErrors(string.Join("\n", themeErrors)), timeout: 10_000);

            if (firstStart)
            {
                // Icons on the desktop: offer the assistant (a click on the notification starts it)
                var items = ScanDesktop().Sum(c => c.Value.Count);
                if (items >= 5)
                    ShowBalloon(Strings.AssistantFirstStart, RunDesktopAssistant, timeout: 15_000);
                else
                    ShowBalloon(Strings.FirstStartHint, timeout: 8000);
            }
            ShowChangelogAfterUpdate(firstStart);
        }

        internal void UpdateDesktopHook()
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

        public void Notify(string text) => ShowBalloon(text, timeout: 8000);

        internal static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        /// <summary>"Settings…" (bold) and "Language ▸" – in the tray and in every fence's menu.</summary>
        public void AddAppSettingsItems(ToolStripItemCollection items)
        {
            var settings = new ToolStripMenuItem(Strings.AppSettings, null, (_, _) => AppSettingsDialog.ShowSingle(this));
            settings.Font = new Font(settings.Font, FontStyle.Bold);
            items.Add(settings);

            // Labelled in several languages so it can be found whatever language is shown
            var language = new ToolStripMenuItem(Strings.LanguageMenu);
            foreach (var code in Strings.Languages)
                language.DropDownItems.Add(new ToolStripMenuItem(Strings.LanguageName(code), Flags.For(code), (_, _) => SetLanguage(code)) { Checked = Store.Config.Language == code });
            items.Add(language);
        }

        internal void SetLanguage(string code)
        {
            Store.Config.Language = code;
            Strings.Language = code;
            Store.RequestSave();
            ApplyToAll();
        }

        public static void AddDocumentItems(ToolStripItemCollection items)
        {
            items.Add(Strings.Help, null, (_, _) => DocumentViewer.ShowDocument(Strings.HelpDocument, Strings.Help));
            items.Add(Strings.WhatsNew, null, (_, _) => DocumentViewer.ShowDocument(Strings.ChangelogDocument, Strings.WhatsNew));
            items.Add(Strings.About, null, (_, _) => AboutDialog.ShowSingle());
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

        internal void SortDesktopNow()
        {
            if (!Store.Config.Fences.Any(f => !string.IsNullOrWhiteSpace(f.AutoSortPatterns)))
            {
                ShowBalloon(Strings.SortNowNoRules);
                return;
            }
            var count = sorter.SortDesktopNow();
            ShowBalloon(Strings.SortNowDone(count));
        }

        public FenceTheme ThemeFor(FenceInfo info) => ThemeRegistry.Get(info.Theme ?? appliedTheme ?? Store.Config.Theme);

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
            AssignActiveProfile(info);
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
            if (window.Info.Hotkey != null)
                UpdateFenceHotkeys();
            window.Close();
            window.Dispose();
        }

        private void OpenWindow(FenceInfo info)
        {
            var window = new FenceWindow(this, info);
            windows.Add(window);
            if (ShouldBeVisible(info))
                window.Show();
        }

        internal void ApplyToAll()
        {
            appliedTheme = DefaultThemeId;
            Store.RequestSave();
            foreach (var w in windows)
            {
                w.ApplySettings();
                w.ReloadEntries();
            }
        }

        internal void ToggleVisible()
        {
            fencesVisible = !fencesVisible;
            ApplyVisibility();
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
            var note = new ToolStripMenuItem(Strings.NewNote, null, (_, _) => QuickNote());
            if (Store.Config.QuickNoteHotkey != "Off")
                note.ShortcutKeyDisplayString = Strings.HotkeyName(Store.Config.QuickNoteHotkey);
            menu.Items.Add(note);
            AddCreateExtrasItems(menu.Items);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.ShowFences, null, (_, _) => ToggleVisible()) { Checked = fencesVisible });
            AddProfileItems(menu.Items);
            AddPeekItems(menu.Items);
            AddToolItems(menu.Items);
            menu.Items.Add(new ToolStripSeparator());
            // Everything else lives in the settings window
            AddAppSettingsItems(menu.Items);
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
            DisposeFps();
            virtualDesktopTimer.Dispose();
            DisposePlaytime();
            DisposeAutomation();
            DisposeSearch();
            DisposeSync();
            DisposeScreenTime();
            DisposeFenceTools();
            DisposeProfileHotkeys();
            DisposeQuickNote();
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
