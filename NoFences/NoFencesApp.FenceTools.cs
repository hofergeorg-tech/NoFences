using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Fading far from the mouse, per-fence shortcuts, the shelf, fences filled by NoFences (browser
    /// bookmarks, recent folders), templates, desktop icons and moving all fences to another monitor.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer fadeTimer = new() { Interval = 100 };
        private readonly System.Windows.Forms.Timer sourcesTimer = new() { Interval = 10 * 60 * 1000 };
        private readonly List<GlobalHotkey> fenceHotkeys = new();

        private void InitFenceTools()
        {
            fadeTimer.Tick += (_, _) => UpdateFading();
            UpdateFading();
            sourcesTimer.Tick += (_, _) =>
            {
                CleanShelves();
                RefreshAutoSources();
            };
            sourcesTimer.Start();
            CleanShelves();
            RefreshAutoSources();
            UpdateFenceHotkeys();
        }

        private void DisposeFenceTools()
        {
            fadeTimer.Dispose();
            sourcesTimer.Dispose();
            foreach (var h in fenceHotkeys)
                h.Dispose();
            fenceHotkeys.Clear();
        }

        /// <summary>A fence's settings were changed in its dialog.</summary>
        public void FenceSettingsChanged(FenceInfo info)
        {
            UpdateFenceHotkeys();
            CleanShelves();
            RefreshAutoSources();
        }

        #region Fading

        /// <summary>Fences grow transparent the further away the mouse is (opt-in).</summary>
        private void UpdateFading()
        {
            if (!Store.Config.FadeFences)
            {
                fadeTimer.Stop();
                foreach (var w in windows.Where(w => w.Opacity < 1))
                    w.Opacity = 1;
                return;
            }
            fadeTimer.Start();
            var cursor = Cursor.Position;
            foreach (var w in windows)
            {
                if (!w.Visible)
                    continue;
                var scale = w.DeviceDpi / 96.0;
                var target = w.Info.NoFade || w.Peeking || OwnDialogActive
                    ? 1
                    : FenceExtras.FadeOpacity(FenceExtras.Distance(w.Bounds, cursor) / scale);
                // Glide there instead of jumping
                var current = w.Opacity;
                var next = Math.Abs(target - current) < 0.06 ? target : current + Math.Sign(target - current) * 0.06;
                if (Math.Abs(next - current) > 0.001)
                    w.Opacity = next;
            }
        }

        // While a menu or dialog of ours is open the fences stay solid (the user is working with them)
        private static bool OwnDialogActive => Form.ActiveForm is not FenceWindow and not null;

        internal void SetFadeFences(bool on)
        {
            Store.Config.FadeFences = on;
            Store.RequestSave();
            UpdateFading();
        }

        #endregion

        #region Per-fence shortcuts

        internal void UpdateFenceHotkeys()
        {
            foreach (var h in fenceHotkeys)
                h.Dispose();
            fenceHotkeys.Clear();
            foreach (var info in Store.Config.Fences)
            {
                if (FenceExtras.FenceHotkeyKey(info.Hotkey) is not { } key)
                    continue;
                var hotkey = new GlobalHotkey(Native.MOD_CONTROL | Native.MOD_SHIFT, key);
                if (!hotkey.Registered)
                {
                    hotkey.Dispose();
                    ShowBalloon(Strings.HotkeyTaken(info.Hotkey!), timeout: 6000);
                    continue;
                }
                var fence = info;
                hotkey.Pressed += (_, _) =>
                {
                    if (windows.FirstOrDefault(w => w.Info == fence) is { } window)
                        StartPeek(window);
                };
                fenceHotkeys.Add(hotkey);
            }
        }

        #endregion

        #region Shelf

        /// <summary>A folder fence that empties itself: what lies there longer than a week goes to the recycle bin.</summary>
        public void CreateShelf()
        {
            var folder = Path.Combine(Store.LocalDirectory, "Shelf");
            Directory.CreateDirectory(folder);
            AddFence(new FenceInfo
            {
                Name = Strings.ShelfName,
                Kind = FenceKind.Folder,
                FolderPath = folder,
                AutoCleanDays = 7,
                SortMode = FenceSortMode.Modified,
                Width = 320,
                Height = 240
            });
        }

        private void CleanShelves()
        {
            foreach (var info in Store.Config.Fences.Where(f => f.Kind == FenceKind.Folder && f.AutoCleanDays > 0 && f.FolderPath != null))
            {
                var old = FenceExtras.ExpiredItems(info.FolderPath!, info.AutoCleanDays, DateTime.Now);
                if (old.Count == 0)
                    continue;
                if (ShellFileOps.RecycleSilently(old))
                    Log.Write("Shelf", $"{info.Name}: {old.Count} item(s) moved to the recycle bin");
            }
        }

        #endregion

        #region Fences filled by NoFences

        public async void CreateBookmarksFence(string browserId, string browserName)
        {
            var info = new FenceInfo
            {
                Name = browserName,
                Kind = FenceKind.Folder,
                FolderPath = Path.Combine(Store.LocalDirectory, "Bookmarks", browserId),
                AutoSource = "bookmarks:" + browserId,
                ReadOnly = true,
                Width = 340,
                Height = 300
            };
            Directory.CreateDirectory(info.FolderPath);
            // Fill it first, so the fence opens with the bookmarks in it
            await RefreshBookmarks(info);
            AddFence(info);
        }

        public void CreateRecentFoldersFence()
        {
            var info = new FenceInfo
            {
                Name = Strings.RecentFoldersName,
                Kind = FenceKind.Links,
                AutoSource = "recentfolders",
                ReadOnly = true,
                Width = 340,
                Height = 300
            };
            AddFence(info);
            RefreshAutoSources();
        }

        private bool refreshingSources;

        private async void RefreshAutoSources()
        {
            if (refreshingSources)
                return;
            refreshingSources = true;
            try
            {
                foreach (var info in Store.Config.Fences.Where(f => f.AutoSource != null).ToList())
                {
                    if (info.AutoSource!.StartsWith("bookmarks:", StringComparison.Ordinal))
                    {
                        await RefreshBookmarks(info);
                    }
                    else if (info.AutoSource == "recentfolders")
                    {
                        var folders = await Task.Run(ReadRecentFolders);
                        if (folders.Count > 0 && !folders.SequenceEqual(info.Files, StringComparer.OrdinalIgnoreCase))
                        {
                            info.Files = folders;
                            Store.RequestSave();
                            windows.FirstOrDefault(w => w.Info == info)?.ReloadEntries();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write("Fence sources", Log.Describe(e));
            }
            finally
            {
                refreshingSources = false;
            }
        }

        private static Task RefreshBookmarks(FenceInfo info) => Task.Run(() =>
        {
            var browser = info.AutoSource!["bookmarks:".Length..];
            var file = FenceExtras.BookmarksFile(browser);
            if (!File.Exists(file) || info.FolderPath == null)
                return;
            try
            {
                var bookmarks = FenceExtras.ParseChromiumBookmarks(File.ReadAllText(file));
                FenceExtras.WriteBookmarks(info.FolderPath, bookmarks);
            }
            catch (IOException e)
            {
                Log.Write("Bookmarks " + browser, Log.Describe(e));
            }
        });

        /// <summary>The 15 most recent folders from Windows' recent items.</summary>
        private static List<string> ReadRecentFolders()
        {
            var recent = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            if (!Directory.Exists(recent))
                return new();
            var links = new DirectoryInfo(recent).EnumerateFiles("*.lnk")
                .OrderByDescending(f => f.LastWriteTime)
                .Take(80) // resolving is slow; the newest are enough
                .Select(f => (f.FullName, f.LastWriteTime));
            return FenceExtras.RecentFolders(links, ResolveShortcut, Directory.Exists, 15);
        }

        private static string? ResolveShortcut(string lnk)
        {
            var target = ShortcutTarget(lnk)?.Trim();
            return string.IsNullOrEmpty(target) ? null : target;
        }

        #endregion

        /// <summary>"Templates ▸", "Shelf", "Browser bookmarks ▸", "Recent folders" in the create menus.</summary>
        private void AddMoreFenceItems(ToolStripItemCollection items)
        {
            var more = new ToolStripMenuItem(Strings.MoreFencesMenu);
            more.DropDownItems.Add(TemplateItems());
            more.DropDownItems.Add(new ToolStripSeparator());
            more.DropDownItems.Add(Strings.NewShelf, null, (_, _) => CreateShelf());
            more.DropDownItems.Add(Strings.NewRecentFolders, null, (_, _) => CreateRecentFoldersFence());
            var bookmarks = new ToolStripMenuItem(Strings.NewBookmarks);
            foreach (var (id, name) in FenceExtras.InstalledBrowsers())
                bookmarks.DropDownItems.Add(name, null, (_, _) => CreateBookmarksFence(id, name));
            if (bookmarks.DropDownItems.Count == 0)
                bookmarks.DropDownItems.Add(new ToolStripMenuItem(Strings.NoBrowserFound) { Enabled = false });
            more.DropDownItems.Add(bookmarks);
            items.Add(more);
        }

        #region Templates

        /// <summary>"Templates ▸ Gaming setup / Office / Minimal": several fences at once.</summary>
        private ToolStripMenuItem TemplateItems()
        {
            var menu = new ToolStripMenuItem(Strings.TemplatesMenu);
            foreach (var id in FenceExtras.TemplateIds)
                menu.DropDownItems.Add(Strings.TemplateName(id), null, (_, _) => CreateFromTemplate(id));
            return menu;
        }

        public void CreateFromTemplate(string id)
        {
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            foreach (var info in FenceExtras.Template(id, area))
            {
                AssignActiveProfile(info);
                Store.Config.Fences.Add(info);
                OpenWindow(info);
            }
            Store.RequestSave();
        }

        #endregion

        #region Desktop icons and monitors

        private void AddDesktopToolItems(ToolStripItemCollection items)
        {
            var iconsShown = Native.DesktopIconsVisible();
            if (iconsShown != null)
            {
                items.Add(new ToolStripMenuItem(Strings.DesktopIconsMenu, null, (_, _) =>
                {
                    if (!Native.ToggleDesktopIcons())
                        ShowBalloon(Strings.DesktopIconsFailed);
                }) { Checked = iconsShown.Value });
            }

            var screens = Screen.AllScreens;
            if (screens.Length > 1)
            {
                var move = new ToolStripMenuItem(Strings.MoveAllToMonitor);
                for (var i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    move.DropDownItems.Add(Strings.MonitorName(i + 1, screen.Bounds.Width, screen.Bounds.Height, screen.Primary), null, (_, _) => MoveAllToScreen(screen));
                }
                items.Add(move);
            }
        }

        /// <summary>Moves every shown fence to the same relative place on <paramref name="target"/>.</summary>
        private void MoveAllToScreen(Screen target)
        {
            foreach (var w in windows.Where(w => w.Visible))
            {
                var from = Screen.FromRectangle(w.Bounds);
                if (from.DeviceName == target.DeviceName)
                    continue;
                var expanded = new Rectangle(w.Left, w.Top, w.Info.Width, w.Info.Height);
                var moved = FenceExtras.MoveToScreen(expanded, from.WorkingArea, target.WorkingArea);
                w.Location = moved.Location;
            }
        }

        #endregion
    }
}
