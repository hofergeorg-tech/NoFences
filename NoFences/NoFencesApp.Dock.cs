using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Fence groups docked to a screen edge (see <see cref="DockBar"/>): stacked along the edge, either
    /// sliding in when the mouse touches the edge or reserving their space like the taskbar.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private const int DockGap = 6;

        /// <summary>Space around the fences when the bar has a background style (logical px).</summary>
        private const int BarPadding = 14;

        private readonly Dictionary<string, DockBackdrop> backdrops = new(StringComparer.CurrentCultureIgnoreCase);
        private readonly Dictionary<string, Rectangle> barStrips = new(StringComparer.CurrentCultureIgnoreCase);
        private const int HideDelayMs = 700;

        private readonly System.Windows.Forms.Timer dockTimer = new() { Interval = 120 };
        private readonly Dictionary<string, AppBar> appBars = new(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>Auto-hide bars that are out right now, and since when the mouse is away from them.</summary>
        private readonly Dictionary<string, DateTime?> shownBars = new(StringComparer.CurrentCultureIgnoreCase);

        private List<DockBar> Docks => Store.Config.Docks;

        private DockBar? DockOf(string? group) =>
            group == null ? null : Docks.FirstOrDefault(d => string.Equals(d.Group, group, StringComparison.CurrentCultureIgnoreCase));

        internal bool IsDocked(FenceInfo info) => DockOf(info.Group) != null;

        private void InitDocks()
        {
            Docks.RemoveAll(d => !Store.Config.Fences.Any(f => string.Equals(f.Group, d.Group, StringComparison.CurrentCultureIgnoreCase)));
            dockTimer.Tick += (_, _) => UpdateDocks();
            UpdateDocks();
        }

        /// <summary>After the screens changed (called on the UI thread): negotiate the reserved strips again.</summary>
        private void DocksAfterScreenChange()
        {
            foreach (var bar in appBars.Values)
                bar.Release();
            UpdateDocks();
        }

        private static Screen ScreenOf(DockBar dock) =>
            Screen.AllScreens.FirstOrDefault(s => s.DeviceName == dock.Screen) ?? Screen.PrimaryScreen ?? Screen.AllScreens[0];

        /// <summary>The fences of a bar that are shown in the current profile/desktop, in the bar's order.</summary>
        private List<FenceWindow> DockMembers(DockBar dock) =>
            DockLayout.InOrder(WindowsInGroup(dock.Group).Where(w => ShouldBeVisible(w.Info)), w => w.Info.Id, dock.Order);

        /// <summary>Hidden because it belongs to an auto-hide bar that is not out (and we're not peeking).</summary>
        private bool HiddenByDock(FenceWindow w) =>
            !peeking && DockOf(w.Info.Group) is { AutoHide: true } dock && !shownBars.ContainsKey(dock.Group);

        /// <summary>Places the bars' fences, reserves space, and lets auto-hide bars slide in and out.</summary>
        private void UpdateDocks()
        {
            // Groups whose fences were all deleted or left
            Docks.RemoveAll(d => !Store.Config.Fences.Any(f => string.Equals(f.Group, d.Group, StringComparison.CurrentCultureIgnoreCase)));
            foreach (var group in backdrops.Keys.Where(g => DockOf(g) is not { Theme: not null }).ToList())
                RemoveBackdrop(group);
            if (Docks.Count == 0)
            {
                dockTimer.Stop();
                foreach (var bar in appBars.Values)
                    bar.Dispose();
                appBars.Clear();
                return;
            }
            dockTimer.Start();

            foreach (var group in appBars.Keys.Where(g => DockOf(g) is not { AutoHide: false }).ToList())
            {
                appBars[group].Dispose();
                appBars.Remove(group);
            }

            var visibilityChanged = false;
            var cursor = Cursor.Position;
            var fullscreen = FullscreenScreen()?.DeviceName;
            foreach (var dock in Docks)
            {
                var members = DockMembers(dock);
                if (members.Count == 0)
                {
                    // Nothing to show in this profile: give the space back
                    if (appBars.Remove(dock.Group, out var unused))
                        unused.Dispose();
                    RemoveBackdrop(dock.Group);
                    continue;
                }
                var screen = ScreenOf(dock);
                var underGame = fullscreen == screen.DeviceName;
                var scale = members[0].DeviceDpi / 96f;
                var thickness = (int)Math.Round(Math.Clamp(dock.Thickness, DockBar.MinThickness, DockBar.MaxThickness) * scale);

                Rectangle strip;
                if (dock.AutoHide)
                {
                    strip = DockLayout.Strip(screen.WorkingArea, dock.Edge, thickness);
                }
                else
                {
                    if (!appBars.TryGetValue(dock.Group, out var bar))
                    {
                        bar = new AppBar();
                        bar.PositionChanged += () => BeginInvokeDock();
                        appBars[dock.Group] = bar;
                    }
                    strip = bar.Reserve(screen.Bounds, dock.Edge, thickness);
                }

                barStrips[dock.Group] = strip;
                var inner = strip;
                if (dock.Theme != null)
                {
                    var pad = (int)Math.Round(BarPadding * scale);
                    inner = Rectangle.Inflate(strip, -pad, -pad);
                }
                if (!members.Any(m => m.InSizeMove))
                    Arrange(dock, members, inner, scale);

                if (dock.AutoHide)
                    visibilityChanged |= UpdateAutoHide(dock, members, screen, cursor, underGame);

                // Above other windows while out (never above a full-screen game)
                var raised = !underGame && (!dock.AutoHide || shownBars.ContainsKey(dock.Group));
                foreach (var m in members)
                    m.SetDockRaised(raised);
            }
            if (visibilityChanged)
                ApplyVisibility();
            UpdateBackdrops();
        }

        #region Bar background

        /// <summary>Shows each styled bar's background where its fences are, below them (they are owned by it).</summary>
        private void UpdateBackdrops()
        {
            foreach (var dock in Docks.Where(d => d.Theme != null))
            {
                var members = DockMembers(dock);
                var shown = members.Where(m => m.Visible).ToList();
                if (shown.Count == 0 || !barStrips.TryGetValue(dock.Group, out var strip))
                {
                    if (backdrops.TryGetValue(dock.Group, out var hidden) && hidden.Visible)
                        hidden.Visible = false;
                    continue;
                }
                var theme = Themes.ThemeRegistry.Get(dock.Theme);
                var vertical = DockLayout.Vertical(dock.Edge);
                if (!backdrops.TryGetValue(dock.Group, out var backdrop))
                {
                    backdrop = new DockBackdrop(dock.Group, theme, vertical);
                    var group = dock.Group;
                    backdrop.MenuRequested += at => ShowBarMenu(group, at);
                    backdrops[dock.Group] = backdrop;
                }
                backdrop.SetLook(theme, vertical);
                if (backdrop.Bounds != strip)
                    backdrop.Bounds = strip;
                if (!backdrop.Visible)
                    backdrop.Visible = true;
                backdrop.SetRaised(shown[0].DockRaised);
                foreach (var m in shown)
                    m.SetOwner(backdrop.Handle);
            }
        }

        /// <summary>Gives the fences back to the desktop, then closes the background.</summary>
        private void RemoveBackdrop(string group)
        {
            if (!backdrops.Remove(group, out var backdrop))
                return;
            // Owned windows would be destroyed together with their owner
            foreach (var w in windows)
                w.SetOwner(null, ifOwner: backdrop.Handle);
            backdrop.Dispose();
        }

        /// <summary>Right-click on the bar's background: the group menu of its first fence.</summary>
        private void ShowBarMenu(string group, Point at)
        {
            var dock = DockOf(group);
            var first = dock == null ? null : DockMembers(dock).FirstOrDefault();
            if (first == null)
                return;
            var menu = new ContextMenuStrip();
            AddGroupItems(menu.Items, first);
            if (menu.Items[0] is ToolStripMenuItem groupMenu)
            {
                // Show the group's entries directly
                var entries = groupMenu.DropDownItems.Cast<ToolStripItem>().ToList();
                menu.Items.Clear();
                menu.Items.AddRange(entries.ToArray());
            }
            menu.Closed += (_, _) => menu.BeginInvoke(menu.Dispose);
            menu.Show(at);
        }

        private void AddBarStyleItems(ToolStripMenuItem menu, DockBar dock)
        {
            var styles = new ToolStripMenuItem(Strings.BarStyle);
            styles.DropDownItems.Add(new ToolStripMenuItem(Strings.BarStyleNone, null, (_, _) => SetBarStyle(dock, null)) { Checked = dock.Theme == null });
            foreach (var group in Themes.ThemeRegistry.All.GroupBy(Themes.ThemeRegistry.GroupOf))
            {
                var sub = new ToolStripMenuItem(Strings.ThemeGroupName(group.Key));
                foreach (var theme in group)
                {
                    var id = theme.Id;
                    sub.DropDownItems.Add(new ToolStripMenuItem(theme.DisplayName, null, (_, _) => SetBarStyle(dock, id)) { Checked = dock.Theme == id });
                }
                styles.DropDownItems.Add(sub);
            }
            menu.DropDownItems.Add(styles);
        }

        private void SetBarStyle(DockBar dock, string? theme)
        {
            dock.Theme = theme;
            Store.RequestSave();
            UpdateDocks();
        }

        #endregion

        private void BeginInvokeDock()
        {
            var wait = new System.Windows.Forms.Timer { Interval = 50 };
            wait.Tick += (_, _) =>
            {
                wait.Dispose();
                UpdateDocks();
            };
            wait.Start();
        }

        /// <summary>Puts each fence in its place along the strip (only if it isn't there already).</summary>
        private static void Arrange(DockBar dock, List<FenceWindow> members, Rectangle strip, float scale)
        {
            var vertical = DockLayout.Vertical(dock.Edge);
            var lengths = members.Select(m => vertical ? m.Height : m.Width).ToList();
            var places = DockLayout.Arrange(strip, dock.Edge, lengths, (int)Math.Round(DockGap * scale));
            for (var i = 0; i < members.Count; i++)
            {
                var place = places[i];
                // A collapsed or folded fence keeps its title-bar height (also in a top/bottom bar)
                if (members[i].IsCollapsed)
                    place.Height = members[i].Height;
                if (members[i].Bounds != place)
                    members[i].PlaceInDock(place);
            }
        }

        /// <summary>Out when the mouse touches the edge, back in when it has been away for a moment. Returns whether that changed.</summary>
        private bool UpdateAutoHide(DockBar dock, List<FenceWindow> members, Screen screen, Point cursor, bool underGame)
        {
            var shown = shownBars.TryGetValue(dock.Group, out var awaySince);
            if (!shown)
            {
                // Not while a game or video covers the monitor
                if (underGame || Control.MouseButtons != MouseButtons.None)
                    return false;
                if (!DockLayout.Trigger(screen.Bounds, dock.Edge).Contains(cursor))
                    return false;
                shownBars[dock.Group] = null;
                return true;
            }

            var area = members.Select(m => m.Bounds).Aggregate(Rectangle.Union);
            if (dock.Theme != null && barStrips.TryGetValue(dock.Group, out var strip))
                area = Rectangle.Union(area, strip);
            area.Inflate(24, 24);
            var busy = members.Any(m => m.Busy) || OwnDialogActive || Control.MouseButtons != MouseButtons.None;
            if (busy || area.Contains(cursor) || DockLayout.Trigger(screen.Bounds, dock.Edge).Contains(cursor))
            {
                shownBars[dock.Group] = null;
                return false;
            }
            if (awaySince == null)
            {
                shownBars[dock.Group] = DateTime.Now;
                return false;
            }
            if ((DateTime.Now - awaySince.Value).TotalMilliseconds < HideDelayMs)
                return false;
            shownBars.Remove(dock.Group);
            return true;
        }

        /// <summary>A docked fence was dragged or resized: new order, thickness or length.</summary>
        public void DockMemberChanged(FenceWindow window, Rectangle before)
        {
            if (DockOf(window.Info.Group) is not { } dock)
                return;
            var vertical = DockLayout.Vertical(dock.Edge);
            var scale = window.DeviceDpi / 96f;
            if (window.Size != before.Size)
            {
                var padding = dock.Theme != null ? 2 * (int)Math.Round(BarPadding * scale) : 0;
                var thickness = (vertical ? window.Width : window.Height) + padding;
                if (thickness - padding != (vertical ? before.Width : before.Height))
                    dock.Thickness = Math.Clamp((int)Math.Round(thickness / scale), DockBar.MinThickness, DockBar.MaxThickness);
            }
            else
            {
                dock.Order = DockLayout.OrderByPosition(DockMembers(dock).Select(m => (m.Info.Id, m.Bounds)), dock.Edge);
            }
            Store.RequestSave();
            UpdateDocks();
        }

        /// <summary>Docks the fence's group (a fence without group gets one) or undocks it.</summary>
        private void SetDock(FenceWindow window, DockEdge? edge)
        {
            var info = window.Info;
            if (edge == null)
            {
                if (DockOf(info.Group) is { } old)
                    Undock(old);
                return;
            }
            if (info.Group == null)
            {
                RecordUndo(Strings.UndoGroup(info.Name), new[] { info.Id });
                info.Group = GroupNames.FirstOrDefault(g => g.Equals(Strings.SidebarGroupName, StringComparison.CurrentCultureIgnoreCase)) ?? Strings.SidebarGroupName;
            }
            var dock = DockOf(info.Group);
            if (dock == null)
            {
                dock = new DockBar { Group = info.Group!, Screen = Screen.FromControl(window).DeviceName };
                Docks.Add(dock);
            }
            dock.Edge = edge.Value;
            RememberUndockedPlaces(dock);
            Store.RequestSave();
            UpdateDocks();
            ApplyVisibility();
        }

        /// <summary>Where the group's fences were before docking (each only the first time).</summary>
        private void RememberUndockedPlaces(DockBar dock)
        {
            foreach (var w in WindowsInGroup(dock.Group).Where(w => !dock.Saved.ContainsKey(w.Info.Id)))
                dock.Saved[w.Info.Id] = new[] { w.Info.PosX, w.Info.PosY, w.Info.Width, w.Info.Height };
        }

        private void Undock(DockBar dock)
        {
            RemoveBackdrop(dock.Group);
            Docks.Remove(dock);
            shownBars.Remove(dock.Group);
            if (appBars.Remove(dock.Group, out var bar))
                bar.Dispose();
            foreach (var w in WindowsInGroup(dock.Group))
            {
                w.SetDockRaised(false);
                if (dock.Saved.TryGetValue(w.Info.Id, out var p) && p.Length == 4)
                {
                    w.Info.PosX = p[0];
                    w.Info.PosY = p[1];
                    w.Info.Width = p[2];
                    w.Info.Height = p[3];
                    w.RefreshFromInfo();
                }
            }
            Store.RequestSave();
            UpdateDocks();
            ApplyVisibility();
        }

        /// <summary>A fence joined a docked group (it goes into the bar) or left one (back to its old place).</summary>
        private void DockGroupChanged(FenceWindow window, string? oldGroup)
        {
            if (oldGroup != null && backdrops.TryGetValue(oldGroup, out var oldBackdrop))
                window.SetOwner(null, ifOwner: oldBackdrop.Handle);
            if (DockOf(oldGroup) is { } left && left.Saved.Remove(window.Info.Id, out var p) && p.Length == 4)
            {
                window.SetDockRaised(false);
                window.Info.PosX = p[0];
                window.Info.PosY = p[1];
                window.Info.Width = p[2];
                window.Info.Height = p[3];
                window.RefreshFromInfo();
            }
            if (DockOf(oldGroup) is { } old && !Store.Config.Fences.Any(f => string.Equals(f.Group, old.Group, StringComparison.CurrentCultureIgnoreCase)))
                Undock(old);
            if (DockOf(window.Info.Group) is { } joined)
                RememberUndockedPlaces(joined);
            UpdateDocks();
            ApplyVisibility();
        }

        /// <summary>"Dock to edge ▸", "Hide automatically", "Monitor ▸" in the group menu.</summary>
        private void AddDockItems(ToolStripMenuItem menu, FenceWindow window)
        {
            var dock = DockOf(window.Info.Group);
            var edges = new ToolStripMenuItem(Strings.DockMenu) { ToolTipText = Strings.DockHint };
            edges.DropDownItems.Add(new ToolStripMenuItem(Strings.DockOff, null, (_, _) => SetDock(window, null)) { Checked = dock == null });
            foreach (var edge in Enum.GetValues<DockEdge>())
            {
                var e = edge;
                edges.DropDownItems.Add(new ToolStripMenuItem(Strings.DockEdgeName(edge), null, (_, _) => SetDock(window, e)) { Checked = dock?.Edge == edge });
            }
            if (dock != null)
            {
                edges.DropDownItems.Add(new ToolStripSeparator());
                edges.DropDownItems.Add(new ToolStripMenuItem(Strings.DockAutoHide, null, (_, _) =>
                {
                    dock.AutoHide = !dock.AutoHide;
                    shownBars.Remove(dock.Group);
                    Store.RequestSave();
                    UpdateDocks();
                    ApplyVisibility();
                }) { Checked = dock.AutoHide });
                AddBarStyleItems(edges, dock);
                if (Screen.AllScreens.Length > 1)
                {
                    var screens = new ToolStripMenuItem(Strings.DockScreen);
                    var current = ScreenOf(dock);
                    for (var i = 0; i < Screen.AllScreens.Length; i++)
                    {
                        var s = Screen.AllScreens[i];
                        screens.DropDownItems.Add(new ToolStripMenuItem(Strings.MonitorName(i + 1, s.Bounds.Width, s.Bounds.Height, s.Primary), null, (_, _) =>
                        {
                            dock.Screen = s.DeviceName;
                            Store.RequestSave();
                            UpdateDocks();
                            ApplyVisibility();
                        }) { Checked = s.DeviceName == current.DeviceName });
                    }
                    edges.DropDownItems.Add(screens);
                }
            }
            menu.DropDownItems.Add(edges);
        }

        private void DisposeDocks()
        {
            dockTimer.Dispose();
            foreach (var group in backdrops.Keys.ToList())
                RemoveBackdrop(group);
            foreach (var bar in appBars.Values)
                bar.Dispose();
            appBars.Clear();
        }
    }
}
