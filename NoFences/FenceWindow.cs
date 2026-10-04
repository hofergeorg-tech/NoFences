using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;
using NoFences.Win32;
using Peter;

namespace NoFences
{
    public sealed partial class FenceWindow : Form
    {
        private const string InternalDragFormat = "NoFences.Item";
        private const int ResizeBorder = 8;

        // Set by a links fence that accepted an item moved out of another fence, so the source can drop it.
        private static bool lastDropWasFenceMove;

        private readonly IFenceHost app;
        private readonly ShellContextMenu shellContextMenu = new();
        private readonly System.Windows.Forms.Timer collapseTimer = new() { Interval = 200 };
        private readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 300 };
        private readonly System.Windows.Forms.Timer linkPollTimer = new() { Interval = 5000 };
        private readonly StringFormat labelFormat = new() { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat labelFormatSingleLine = new() { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        private FenceTheme theme = ThemeRegistry.All[0];
        private Font? titleFont;
        private Font? labelFont;
        private float scale = 1;
        private int titleHeight;

        private List<FenceEntry> entries = new();
        private readonly List<Rectangle> itemRects = new(); // content coordinates (before scrolling)
        private int itemWidth, itemHeight, iconPx;
        private int contentHeight;
        private int scrollOffset;

        private string? hoverPath;
        private Point? mouseDownAt;
        private string? mouseDownPath;
        private int insertIndex = -1;
        private bool collapsed;
        private bool suppressBoundsSave;

        private FileSystemWatcher? watcher;

        public FenceInfo Info { get; }

        public FenceWindow(IFenceHost app, FenceInfo info)
        {
            this.app = app;
            Info = info;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            DoubleBuffered = true;
            AllowDrop = true;
            Text = info.Name;

            suppressBoundsSave = true;
            Bounds = new Rectangle(info.PosX, info.PosY, Math.Max(80, info.Width), Math.Max(60, info.Height));
            suppressBoundsSave = false;

            collapseTimer.Tick += (_, _) => CollapseIfMouseAway();
            refreshTimer.Tick += (_, _) => { refreshTimer.Stop(); ReloadEntries(); };
            linkPollTimer.Tick += (_, _) => { if (Info.Kind == FenceKind.Links) ReloadEntries(); };
            IconCache.Shared.ImageLoaded += IconCache_ImageLoaded;
            InitAnimations();
            InitHoverPreview();
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.EnableShadow(Handle);
            Native.EnableBlur(Handle);
            Native.HideFromAltTab(Handle);
            Native.GlueToDesktop(Handle);
            ApplyZOrder();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplySettings();
            ApplyLayoutForCurrentScreens();
            ReloadEntries();
            linkPollTimer.Start();
            if (Info.CanMinify || Info.Folded)
                Collapse();
        }

        /// <summary>Folds the fence to its title bar (with its group) or unfolds it.</summary>
        public void SetFolded(bool folded)
        {
            Info.Folded = folded;
            if (folded)
                Collapse();
            else if (!Info.CanMinify)
                Expand();
        }

        /// <summary>After undo: shows the fence as <see cref="Info"/> says (position, size, name, items).</summary>
        public void RefreshFromInfo()
        {
            selection.Clear();
            ApplySettings();
            suppressBoundsSave = true;
            Bounds = new Rectangle(Info.PosX, Info.PosY, Math.Max(80, Info.Width), collapsed ? CollapsedHeight : Math.Max(60, Info.Height));
            suppressBoundsSave = false;
            if (Info.Folded && !collapsed)
                Collapse();
            else if (!Info.Folded && !Info.CanMinify && collapsed)
                Expand();
            ApplyZOrder();
            ReloadEntries();
        }

        /// <summary>Re-reads everything derived from <see cref="Info"/> and the global config.</summary>
        public void ApplySettings()
        {
            scale = DeviceDpi / 96f;
            theme = app.ThemeFor(Info);
            Text = Info.Name;
            titleHeight = (int)Math.Round(Math.Clamp(Info.TitleHeight, 16, 100) * scale);

            titleFont?.Dispose();
            labelFont?.Dispose();
            titleFont = theme.CreateTitleFont(titleHeight);
            labelFont = theme.CreateLabelFont(scale);
            ApplyNoteSettings();
            ApplyWidgetSettings();

            if (IsHandleCreated)
            {
                Native.SetCornerPreference(Handle, theme.CornerPreference);
                if (theme.Glass)
                    Native.EnableBlur(Handle);
                else
                    Native.EnableClearBackground(Handle);
                Native.SetWindowShadow(Handle, theme.WindowShadow);
            }

            SetupWatcher();
            if (collapsed)
                SetHeightSilently(CollapsedHeight);
            Relayout();
            Invalidate();
        }

        #region Entries

        public void ReloadEntries()
        {
            if (Info.Kind == FenceKind.Folder)
                LoadFolderEntries();
            else
                entries = Info.Files.Select(FenceEntry.FromPath).OfType<FenceEntry>().ToList();
            entries = ApplySearch(FenceEntry.Sort(entries, Info.SortMode, Info.OpenCounts));
            if (Info.MaxItems > 0 && entries.Count > Info.MaxItems)
                entries = entries.Take(Info.MaxItems).ToList();
            AddExpandedChildren();
            selection.RemoveWhere(p => !entries.Any(e => e.Path.Equals(p, StringComparison.OrdinalIgnoreCase)));

            if (hoverPath != null && !entries.Any(x => x.Path == hoverPath))
                hoverPath = null;
            Relayout();
            Invalidate();
        }

        private void LoadFolderEntries()
        {
            var folder = Info.FolderPath;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                entries = new();
                return;
            }

            List<string> found;
            try
            {
                var dir = new DirectoryInfo(folder);
                found = dir.EnumerateFileSystemInfos()
                    .Where(f => (f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    .OrderBy(f => f is FileInfo)
                    .ThenBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(f => f.FullName)
                    .ToList();
            }
            catch (Exception)
            {
                entries = new();
                return;
            }

            // Keep the user's order for known items, append new ones in folder order.
            var foundSet = new HashSet<string>(found, StringComparer.OrdinalIgnoreCase);
            var ordered = Info.Files.Where(foundSet.Contains).ToList();
            var orderedSet = new HashSet<string>(ordered, StringComparer.OrdinalIgnoreCase);
            ordered.AddRange(found.Where(f => !orderedSet.Contains(f)));

            if (!ordered.SequenceEqual(Info.Files, StringComparer.OrdinalIgnoreCase))
            {
                Info.Files = ordered;
                app.RequestSave();
            }
            entries = ordered.Select(FenceEntry.FromPath).OfType<FenceEntry>().ToList();
        }

        private void SetupWatcher()
        {
            watcher?.Dispose();
            watcher = null;
            if (Info.Kind != FenceKind.Folder || string.IsNullOrEmpty(Info.FolderPath) || !Directory.Exists(Info.FolderPath))
                return;

            watcher = new FileSystemWatcher(Info.FolderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Attributes,
                SynchronizingObject = this,
                EnableRaisingEvents = true
            };
            FileSystemEventHandler changed = (_, _) => { refreshTimer.Stop(); refreshTimer.Start(); };
            watcher.Created += changed;
            watcher.Deleted += changed;
            watcher.Changed += changed;
            watcher.Renamed += (_, e) =>
            {
                // Keep the position of a renamed item.
                var i = Info.Files.FindIndex(f => f.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase));
                if (i >= 0)
                    Info.Files[i] = e.FullPath;
                refreshTimer.Stop();
                refreshTimer.Start();
            };
        }

        #endregion

        #region Layout

        private int Px(float logical) => (int)Math.Round(logical * scale);

        // Without the theme's bottom margin (e.g. the Post-it's shadow), so items stay on the paper.
        private int ViewHeight => Math.Max(0, ClientSize.Height - titleHeight - Px(theme.BottomInset));

        private int MaxScroll => Math.Max(0, contentHeight - ViewHeight);

        private void Relayout()
        {
            iconPx = Px(Info.IconSize);
            if (Info.Compact)
            {
                // Quick-launch bar: icons only, names as tooltips
                itemWidth = iconPx + Px(12);
                itemHeight = iconPx + Px(12);
            }
            else
            {
                itemWidth = Math.Max(Px(75), iconPx + Px(28));
                itemHeight = Px(4) + iconPx + Px(4) + (labelFont?.Height ?? Px(16)) * 2 + Px(4);
            }

            var pad = Px(10 + theme.ContentInset);
            var gap = Px(8);
            var usable = ClientSize.Width - 2 * pad - Px(6); // leave room for the scrollbar
            var columns = Math.Max(1, (usable + gap) / (itemWidth + gap));

            itemRects.Clear();
            // The contents of an opened folder start on a new row, indented; the fence continues on a new row after them.
            var indent = Px(18);
            int col = 0, y = pad, previousDepth = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var depth = Depth(i);
                if (depth != previousDepth)
                {
                    if (col > 0)
                        y += itemHeight + gap;
                    col = 0;
                    y += Px(4);
                }
                var x0 = pad + (depth > 0 ? indent : 0);
                var rowColumns = depth > 0 ? Math.Max(1, (usable - indent + gap) / (itemWidth + gap)) : columns;
                itemRects.Add(new Rectangle(x0 + col * (itemWidth + gap), y, itemWidth, itemHeight));
                if (++col >= rowColumns)
                {
                    col = 0;
                    y += itemHeight + gap;
                }
                previousDepth = depth;
            }

            // Notes measure their text while painting and set contentHeight there.
            if (!IsNote)
                contentHeight = itemRects.Count == 0 ? 0 : itemRects[^1].Bottom + pad;
            scrollOffset = Math.Clamp(scrollOffset, 0, MaxScroll);
            LayoutEditor();
        }

        private Rectangle ToClient(Rectangle contentRect) => new(contentRect.X, contentRect.Y + titleHeight - scrollOffset, contentRect.Width, contentRect.Height);

        public static Color MarkToColor(MarkColor mark) => mark switch
        {
            MarkColor.Red => Color.FromArgb(232, 64, 64),
            MarkColor.Orange => Color.FromArgb(245, 150, 40),
            MarkColor.Yellow => Color.FromArgb(240, 210, 40),
            MarkColor.Green => Color.FromArgb(70, 190, 90),
            MarkColor.Blue => Color.FromArgb(60, 140, 240),
            MarkColor.Purple => Color.FromArgb(160, 90, 220),
            _ => Color.Transparent
        };

        private void MarkSelection(MarkColor mark) => MarkItems(selection.ToList(), mark);

        /// <summary>A small colored circle for the menu.</summary>
        private static Bitmap? MarkSwatch(MarkColor mark)
        {
            if (mark == MarkColor.None)
                return null;
            var bitmap = new Bitmap(14, 14);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(MarkToColor(mark));
            g.FillEllipse(brush, 1, 1, 12, 12);
            return bitmap;
        }

        private void MarkItems(IEnumerable<string> paths, MarkColor mark)
        {
            Info.Marks ??= new Dictionary<string, MarkColor>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                if (mark == MarkColor.None)
                    Info.Marks.Remove(path);
                else
                    Info.Marks[path] = mark;
            }
            if (Info.Marks.Count == 0)
                Info.Marks = null;
            app.RequestSave();
            Invalidate();
        }

        /// <summary>Opens an item and counts it (for "most used first").</summary>
        private void OpenEntry(string path)
        {
            if (!OpenWithFenceProgram(path))
                FenceEntry.FromPath(path)?.Open();
            Info.CountOpen(path);
            app.RequestSave();
            if (Info.SortMode == FenceSortMode.MostUsed)
                ReloadEntries();
        }

        private int HitTestItem(Point client)
        {
            if (collapsed || client.Y < titleHeight)
                return -1;
            for (var i = 0; i < itemRects.Count; i++)
            {
                if (ToClient(itemRects[i]).Contains(client))
                    return i;
            }
            return -1;
        }

        /// <summary>Index before which a dropped item would be inserted.</summary>
        private int InsertIndexAt(Point client)
        {
            var p = new Point(client.X, client.Y - titleHeight + scrollOffset);
            for (var i = 0; i < itemRects.Count; i++)
            {
                var r = itemRects[i];
                if (p.Y < r.Top)
                    return i;
                if (p.Y <= r.Bottom && p.X < r.Left + r.Width / 2)
                    return i;
            }
            return itemRects.Count;
        }

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e) => PaintFence(e.Graphics);

        /// <summary>Paints the whole fence; also used by the <c>--preview</c> renderer.</summary>
        public void PaintFence(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var bounds = ClientRectangle;
            g.SetClip(bounds);
            theme.DrawFrame(g, bounds, titleHeight, Info, scale);
            DrawBackgroundPicture(g);
            if (HasTabs)
                DrawTabs(g);
            else if (titleFont != null)
                theme.DrawTitle(g, new Rectangle(0, 0, bounds.Width, titleHeight), theme.FormatTitle(Text), titleFont, scale);
            if (IsNote)
                DrawReminderBadge(g);

            if (collapsed || labelFont == null)
                return;

            var view = new Rectangle(0, titleHeight, bounds.Width, ViewHeight);
            g.SetClip(view);

            if (IsNote)
                DrawNote(g, view);
            else if (IsWidget)
                DrawWidget(g);
            else if (entries.Count == 0)
                DrawEmptyHint(g, view);
            DrawChildBands(g, view);

            for (var i = 0; i < entries.Count; i++)
            {
                var r = ToClient(itemRects[i]);
                if (!r.IntersectsWith(view))
                    continue;

                var entry = entries[i];
                theme.DrawItemBackground(g, r, entry.Path == hoverPath, IsSelected(entry.Path), scale);

                var icon = IconCache.Shared.Get(entry.Path, iconPx);
                if (icon != null)
                {
                    // Shell images can be smaller than requested (e.g. wide thumbnails); center them in the icon box.
                    var ix = r.X + (r.Width - icon.Width) / 2;
                    var iy = Info.Compact ? r.Y + (r.Height - icon.Height) / 2 : r.Y + Px(4) + (iconPx - icon.Height) / 2;
                    g.DrawImage(icon, ix, iy, icon.Width, icon.Height);
                }

                // Color mark: a dot in the top right corner of the item
                if (Info.Marks != null && Info.Marks.TryGetValue(entry.Path, out var mark) && mark != MarkColor.None)
                {
                    var d = Px(11);
                    using var markBrush = new SolidBrush(MarkToColor(mark));
                    using var ring = new Pen(Color.FromArgb(220, 255, 255, 255), Math.Max(1, scale * 1.2f));
                    g.FillEllipse(markBrush, r.Right - d - Px(3), r.Y + Px(3), d, d);
                    g.DrawEllipse(ring, r.Right - d - Px(3), r.Y + Px(3), d, d);
                }
                DrawItemNoteMark(g, entry.Path, r);
                DrawChevron(g, i, r);

                if (Info.Compact)
                    continue;
                var labelTop = r.Y + Px(4) + iconPx + Px(4);
                var labelRect = new RectangleF(r.X + Px(2), labelTop, r.Width - Px(4), r.Bottom - labelTop);
                var name = entry.GetDisplayName(app.ShowExtensions);
                // A single word that is too wide would be broken mid-word; shorten it with "…" instead.
                var format = !name.Contains(' ') && g.MeasureString(name, labelFont).Width > labelRect.Width ? labelFormatSingleLine : labelFormat;
                theme.DrawLabel(g, name, labelRect, labelFont, format, scale);
            }

            if (insertIndex >= 0)
                DrawInsertMarker(g);
            DrawBand(g);
            DrawSearch(g);
            g.SetClip(ClientRectangle);
            DrawHoverAnimation(g);

            if (MaxScroll > 0)
            {
                var track = new Rectangle(bounds.Width - Px(8), titleHeight + Px(4), Px(6), ViewHeight - Px(8));
                var thumbHeight = Math.Max(Px(20), track.Height * ViewHeight / contentHeight);
                var thumbY = track.Y + (int)((long)(track.Height - thumbHeight) * scrollOffset / MaxScroll);
                theme.DrawScrollbar(g, track, new Rectangle(track.X, thumbY, track.Width, thumbHeight), scale);
            }
        }

        private void DrawEmptyHint(Graphics g, Rectangle view)
        {
            var text = Info.Kind == FenceKind.Folder && !Directory.Exists(Info.FolderPath ?? "")
                ? Strings.FolderMissing(Info.FolderPath ?? "")
                : Strings.DropHint;
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var brush = new SolidBrush(theme.HintColor);
            g.DrawString(text, labelFont!, brush, RectangleF.Inflate(view, -Px(12), -Px(12)), format);
        }

        private void DrawInsertMarker(Graphics g)
        {
            int x, top;
            if (itemRects.Count == 0)
                return;
            if (insertIndex < itemRects.Count)
            {
                var r = ToClient(itemRects[insertIndex]);
                x = r.Left - Px(4);
                top = r.Top;
            }
            else
            {
                var r = ToClient(itemRects[^1]);
                x = r.Right + Px(4);
                top = r.Top;
            }
            theme.DrawInsertMarker(g, x, top, itemHeight, scale);
        }

        private void IconCache_ImageLoaded(object? sender, EventArgs e)
        {
            if (!collapsed && Visible)
                Invalidate();
        }

        #endregion

        #region Window messages

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Native.WM_NCCALCSIZE:
                    // No non-client area at all.
                    m.Result = IntPtr.Zero;
                    return;

                case Native.WM_SETFOCUS:
                    // A clicked fence takes keyboard focus (selection, search, shortcuts) like the desktop
                    // does, but must not come in front of other windows.
                    base.WndProc(ref m);
                    if (!OnTop)
                        Native.SendToBottom(Handle);
                    return;

                case Native.WM_WINDOWPOSCHANGING:
                    if (!OnTop)
                        KeepAtBottom(m.LParam);
                    break;

                case Native.WM_NCLBUTTONDBLCLK when m.WParam.ToInt32() == Native.HTCAPTION:
                    // Double-click on the title renames the fence in place (instead of Windows' maximize).
                    if (!collapsed && !Info.Locked)
                        StartEditTitle();
                    return;

                case Native.WM_ENTERSIZEMOVE:
                    BeginSizeMove();
                    break;

                case Native.WM_EXITSIZEMOVE:
                    EndSizeMove();
                    break;

                case Native.WM_MOVING:
                    SnapMoving(m.LParam);
                    break;

                case Native.WM_SIZING:
                    SnapSizing(m.WParam.ToInt32(), m.LParam);
                    break;

                case Native.WM_SYSCOMMAND:
                    var cmd = m.WParam.ToInt32() & 0xFFF0;
                    if (cmd == Native.SC_MAXIMIZE)
                        return;
                    break;
            }

            base.WndProc(ref m);

            if (m.Msg == Native.WM_NCHITTEST)
                m.Result = new IntPtr(HitTest(PointToClient(new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)))), (int)m.Result));
        }

        /// <summary>True while the fences are shown above all windows (peek shortcut).</summary>
        public bool Peeking { get; private set; }

        /// <summary>Above other windows: during peek, in a bar that is out, or permanently with "Always on top".</summary>
        private bool OnTop => Peeking || DockRaised || Info.AlwaysOnTop;

        /// <summary>Part of a docked bar that is shown (or reserves its space): above other windows.</summary>
        public bool DockRaised { get; private set; }

        public void SetDockRaised(bool on)
        {
            if (DockRaised == on)
                return;
            DockRaised = on;
            ApplyZOrder();
        }

        /// <summary>
        /// Owned by a bar's background while docked (Windows keeps owned windows above their owner);
        /// null = back to the desktop. With <paramref name="ifOwner"/> only if that window is the owner now.
        /// </summary>
        public void SetOwner(IntPtr? owner, IntPtr? ifOwner = null)
        {
            if (!IsHandleCreated)
                return;
            var current = Native.GetWindowLongPtr(Handle, Native.GWLP_HWNDPARENT);
            if (ifOwner != null && current != ifOwner.Value)
                return;
            if (owner == null)
            {
                if (ifOwner != null || current != Native.FindWindow("Progman", null))
                    Native.GlueToDesktop(Handle);
                return;
            }
            if (current != owner.Value)
                Native.SetWindowLongPtr(Handle, Native.GWLP_HWNDPARENT, owner.Value);
        }

        /// <summary>Moved or resized by the user right now.</summary>
        public bool InSizeMove => inSizeMove;

        public bool IsCollapsed => collapsed;

        /// <summary>The user is working with it (menu, editor, drag): a bar must not slide away.</summary>
        public bool Busy => appMenuOpen || Editing || Capture || inSizeMove;

        /// <summary>Put in place by its docked bar.</summary>
        public void PlaceInDock(Rectangle place)
        {
            suppressBoundsSave = true;
            Bounds = place;
            suppressBoundsSave = false;
            Info.PosX = Left;
            Info.PosY = Top;
            Info.Width = Width;
            if (!collapsed)
                Info.Height = Height;
            RememberLayout();
            app.RequestSave();
        }

        public void SetPeek(bool on)
        {
            if (Peeking == on)
                return;
            Peeking = on;
            ApplyZOrder();
        }

        private void ApplyZOrder()
        {
            if (!IsHandleCreated)
                return;
            const uint flags = Native.SWP_NOSIZE | Native.SWP_NOMOVE | Native.SWP_NOACTIVATE;
            if (OnTop)
            {
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0, flags);
            }
            else
            {
                Native.SetWindowPos(Handle, Native.HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                Native.SendToBottom(Handle);
            }
        }

        private void ToggleAlwaysOnTop()
        {
            Info.AlwaysOnTop = !Info.AlwaysOnTop;
            app.RequestSave();
            ApplyZOrder();
        }

        private static void KeepAtBottom(IntPtr lParam)
        {
            var pos = Marshal.PtrToStructure<Native.WINDOWPOS>(lParam);
            if ((pos.flags & Native.SWP_NOZORDER) == 0 && pos.hwndInsertAfter != Native.HWND_BOTTOM)
            {
                pos.hwndInsertAfter = Native.HWND_BOTTOM;
                Marshal.StructureToPtr(pos, lParam, false);
            }
        }

        /// <summary>Used by the preview renderer to show the collapsed look.</summary>
        internal void CollapseForPreview() => Collapse();

        /// <summary>Height of a collapsed fence: the title, plus whatever margin the theme draws around it.</summary>
        private int CollapsedHeight => titleHeight + Px(theme.CollapsedExtra);

        private int HitTest(Point pt, int current)
        {
            if (collapsed && Info.CanMinify && !Info.Folded)
                Expand();

            // Right clicks always go to the client area so our context menu shows instead of the system menu.
            if (current != Native.HTCLIENT || Info.Locked || MouseButtons == MouseButtons.Right)
                return current;

            var b = Px(ResizeBorder);
            var w = ClientSize.Width;
            var h = ClientSize.Height;
            // Resize zones sit on the visible surface's edges (themes like Post-it have a clear margin).
            var ins = theme.SurfaceInsets;
            var surface = Rectangle.FromLTRB(Px(ins.Left), Px(ins.Top), w - Px(ins.Right), h - Px(ins.Bottom));
            bool left = pt.X < surface.Left + b, right = pt.X >= surface.Right - b, top = pt.Y < surface.Top + b / 2, bottom = pt.Y >= surface.Bottom - b;

            if (!collapsed)
            {
                if (top && left) return Native.HTTOPLEFT;
                if (top && right) return Native.HTTOPRIGHT;
                if (bottom && left) return Native.HTBOTTOMLEFT;
                if (bottom && right) return Native.HTBOTTOMRIGHT;
                if (bottom) return Native.HTBOTTOM;
                if (top) return Native.HTTOP;
            }
            if (left) return Native.HTLEFT;
            if (right) return Native.HTRIGHT;
            // Tab chips sit in the title bar but must get normal clicks.
            if (HasTabs && !collapsed && TabAt(pt) != -2) return Native.HTCLIENT;
            if (pt.Y < titleHeight || collapsed) return Native.HTCAPTION;
            return Native.HTCLIENT;
        }

        #endregion

        #region Collapse

        private void Collapse()
        {
            if (collapsed)
                return;
            collapsed = true;
            hoverPath = null;
            HideHoverPreview();
            collapseTimer.Stop();
            AnimateHeight(CollapsedHeight);
            Invalidate();
        }

        private void Expand()
        {
            if (!collapsed)
                return;
            collapsed = false;
            AnimateHeight(Math.Max(Info.Height, titleHeight + Px(40)));
            Relayout();
            Invalidate();
            collapseTimer.Start();
        }

        private void CollapseIfMouseAway()
        {
            if (!Info.CanMinify || collapsed || Info.Folded)
            {
                collapseTimer.Stop();
                return;
            }
            // Don't collapse while the user is dragging, resizing or has a menu open.
            if (MouseButtons != MouseButtons.None || Capture || appMenuOpen || Editing)
                return;
            if (!Bounds.Contains(Cursor.Position))
                Collapse();
        }

        private void SetHeightSilently(int height)
        {
            suppressBoundsSave = true;
            Height = height;
            suppressBoundsSave = false;
        }

        #endregion

        #region Position / size persistence

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            LayoutEditor();
            MoveGroupAlong();
            // While dragging, save once at the end (EndSizeMove) instead of on every pixel.
            if (suppressBoundsSave || !IsHandleCreated || inSizeMove)
                return;
            Info.PosX = Left;
            Info.PosY = Top;
            RememberLayout();
            app.RequestSave();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
            Invalidate();
            if (suppressBoundsSave || collapsed || !IsHandleCreated || inSizeMove)
                return;
            Info.Width = Width;
            Info.Height = Height;
            RememberLayout();
            app.RequestSave();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            ApplySettings();
        }

        private void EnsureOnScreen()
        {
            var titleBar = new Rectangle(Left, Top, Width, titleHeight);
            if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(titleBar)))
                return;
            var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 800, 600);
            Location = new Point(area.Left + Px(100), area.Top + Px(100));
        }

        #endregion

        #region Mouse

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HoverStarted();
            if (collapsed && Info.CanMinify && !Info.Folded)
                Expand();
            if (IsNote)
            {
                UpdateNoteCursor(e.Location);
                return;
            }
            if (IsWidget)
            {
                Cursor = widget?.IsClickable(e.Location) == true ? Cursors.Hand : Cursors.Default;
                var tip = widget?.TooltipAt(e.Location) ?? "";
                if (toolTip.GetToolTip(this) != tip)
                    toolTip.SetToolTip(this, tip);
                return;
            }

            var index = HitTestItem(e.Location);
            var path = index >= 0 ? entries[index].Path : null;
            if (path != hoverPath)
            {
                hoverPath = path;
                RestartHoverPreview();
                UpdateItemTooltip(path);
                Invalidate();
            }

            if (bandStart != null && e.Button == MouseButtons.Left)
            {
                UpdateBand(e.Location);
                return;
            }

            if (mouseDownAt is Point start && mouseDownPath != null && e.Button == MouseButtons.Left)
            {
                var drag = SystemInformation.DragSize;
                if (Math.Abs(e.X - start.X) > drag.Width || Math.Abs(e.Y - start.Y) > drag.Height)
                {
                    var paths = IsSelected(mouseDownPath) ? SelectedInOrder() : new List<string> { mouseDownPath };
                    mouseDownAt = null;
                    mouseDownPath = null;
                    StartDrag(paths);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HideHoverPreview();
            if (hoverPath != null)
            {
                hoverPath = null;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            HideHoverPreview();
            if (e.Button != MouseButtons.Left)
                return;
            if (IsNote)
            {
                if (e.Clicks == 1)
                    NoteClick(e.Location);
                return;
            }
            if (IsWidget)
            {
                if (e.Clicks == 1 && widget?.Click(e.Location) == true)
                    Invalidate();
                return;
            }
            if (e.Clicks == 1 && ChevronClick(e.Location))
                return;
            SelectionMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (HasTabs && TabMouseUp(e))
            {
                mouseDownAt = null;
                mouseDownPath = null;
                return;
            }
            if (!IsNote && !IsWidget)
                SelectionMouseUp(e);
            mouseDownAt = null;
            mouseDownPath = null;

            if (e.Button != MouseButtons.Right)
                return;

            var index = HitTestItem(e.Location);
            if (index >= 0 && !IsSelected(entries[index].Path))
            {
                SelectOnly(entries[index].Path);
                Invalidate();
            }
            if (index >= 0 && (ModifierKeys & Keys.Shift) == 0)
                ShowShellMenu(entries[index]);
            else
                ShowAppMenu(index >= 0 ? entries[index] : null, e.Location);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left)
                return;
            if (HasTabs && TabAt(e.Location) >= 0)
            {
                RenameTab(TabAt(e.Location));
                return;
            }
            if (IsWidget)
            {
                widget?.DoubleClick(e.Location);
                return;
            }
            if (IsNote)
            {
                // A fast double click on a checkbox toggles twice; don't also open the editor then.
                var p = new PointF(e.X, e.Y - titleHeight + scrollOffset);
                if (e.Y >= titleHeight && !checkboxes.Any(c => RectangleF.Inflate(c.box, Px(3), Px(3)).Contains(p)))
                    StartEditNote();
                return;
            }
            var index = HitTestItem(e.Location);
            if (index >= 0)
                OpenEntry(entries[index].Path);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            HideHoverPreview();
            if (IsWidget)
            {
                if (widget?.Wheel(e.Delta) == true)
                    Invalidate();
                return;
            }
            if (MaxScroll == 0)
                return;
            var step = (itemHeight + Px(8)) / 2;
            scrollOffset = Math.Clamp(scrollOffset - Math.Sign(e.Delta) * step, 0, MaxScroll);
            Invalidate();
        }

        #endregion

        #region Context menus

        private bool appMenuOpen;

        private void ShowShellMenu(FenceEntry entry)
        {
            try
            {
                // The shell menu can only cover several items that share a folder (and are all files or all folders).
                var selected = SelectedInOrder().Select(FenceEntry.FromPath).OfType<FenceEntry>().ToList();
                var group = selected.Count > 1
                            && selected.All(s => s.IsFolder == entry.IsFolder)
                            && selected.Select(s => Path.GetDirectoryName(s.Path.TrimEnd('\\'))).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
                    ? selected
                    : new List<FenceEntry> { entry };

                if (entry.IsFolder)
                    shellContextMenu.ShowContextMenu(group.Select(g => new DirectoryInfo(g.Path)).ToArray(), Cursor.Position);
                else
                    shellContextMenu.ShowContextMenu(group.Select(g => new FileInfo(g.Path)).ToArray(), Cursor.Position);
            }
            catch (Exception)
            {
                // Some shell extensions throw; fall back to our own menu.
                ShowAppMenu(entry, PointToClient(Cursor.Position));
            }
            ReloadEntries();
        }

        private void ShowAppMenu(FenceEntry? entry, Point location)
        {
            var menu = new ContextMenuStrip();

            if (IsNote)
            {
                menu.Items.Add(Strings.EditNote, null, (_, _) => StartEditNote());
                menu.Items.Add(new ToolStripMenuItem(Strings.Reminder, null, (_, _) => EditReminder()) { Checked = Info.ReminderAt != null });
                AddAppointmentItems(menu.Items);
                menu.Items.Add(Strings.VoiceNoteRecord, null, (_, _) => RecordVoiceNote());
                AddNoteProtectionItems(menu.Items);
            }
            if (IsWidget)
                widget?.AddMenuItems(menu.Items, this);
            if (Info.Kind == FenceKind.Links && !Info.ReadOnly)
                menu.Items.Add(new ToolStripMenuItem(Strings.CompactMode, null, (_, _) => { Info.Compact = !Info.Compact; app.RequestSave(); ReloadEntries(); }) { Checked = Info.Compact });
            if (entry != null && Info.Kind == FenceKind.Links && !Info.ReadOnly)
                menu.Items.Add(Strings.RemoveItem, null, (_, _) => RemoveLink(entry.Path));
            if (entry != null)
            {
                // Mark the item (or all selected ones) in a color
                var targets = IsSelected(entry.Path) ? selection.ToList() : new List<string> { entry.Path };
                var current = Info.Marks != null && Info.Marks.TryGetValue(entry.Path, out var m) ? m : MarkColor.None;
                var markMenu = new ToolStripMenuItem(Strings.MarkMenu);
                foreach (var color in Enum.GetValues<MarkColor>())
                {
                    var item = new ToolStripMenuItem(Strings.MarkName(color), MarkSwatch(color), (_, _) => MarkItems(targets, color)) { Checked = current == color };
                    if (color != MarkColor.None)
                        item.ShortcutKeyDisplayString = $"{Strings.HotkeyName("Ctrl")}+{(int)color}";
                    markMenu.DropDownItems.Add(item);
                }
                menu.Items.Add(markMenu);
                var notePath = entry.Path;
                menu.Items.Add(new ToolStripMenuItem(Strings.ItemNoteMenu, null, (_, _) => EditItemNote(notePath)) { Checked = NoteOf(notePath) != null });
                if (entry.IsFolder && !Info.Compact && !IsChildPath(entry.Path))
                    menu.Items.Add(new ToolStripMenuItem(Strings.ShowFolderInFence, null, (_, _) => ToggleExpanded(notePath)) { Checked = IsExpanded(notePath) });
            }
            if (Info.Kind is FenceKind.Links or FenceKind.Folder)
                menu.Items.Add(Strings.FenceStatsMenu, null, (_, _) => FenceStatsDialog.Show(this, Info, app));
            if (Info.Kind == FenceKind.Folder && Directory.Exists(Info.FolderPath))
                menu.Items.Add(Strings.OpenFolder, null, (_, _) => FenceEntry.FromPath(Info.FolderPath!)?.Open());
            if (menu.Items.Count > 0)
                menu.Items.Add(new ToolStripSeparator());

            app.AddUndoItem(menu.Items);
            menu.Items.Add(Strings.Rename, null, (_, _) => StartEditTitle());
            if (Info.Kind == FenceKind.Links && !Info.ReadOnly)
                menu.Items.Add(Strings.AddTab, null, (_, _) => AddTab());
            menu.Items.Add(Strings.Settings, null, (_, _) => OpenSettings());
            menu.Items.Add(new ToolStripMenuItem(Strings.Locked, null, (_, _) => { Info.Locked = !Info.Locked; app.RequestSave(); }) { Checked = Info.Locked });
            menu.Items.Add(new ToolStripMenuItem(Strings.AutoCollapse, null, (_, _) => ToggleCollapse()) { Checked = Info.CanMinify });
            menu.Items.Add(new ToolStripMenuItem(Strings.AlwaysOnTop, null, (_, _) => ToggleAlwaysOnTop()) { Checked = Info.AlwaysOnTop });
            if (app.CurrentVirtualDesktop != null)
                menu.Items.Add(new ToolStripMenuItem(Strings.OnlyThisDesktop, null, (_, _) => app.TogglePinToDesktop(Info)) { Checked = Info.VirtualDesktop != null });
            app.AddFenceProfileItems(menu.Items, Info, this);
            app.AddGroupItems(menu.Items, this);

            var style = new ToolStripMenuItem(Strings.Theme);
            style.DropDownItems.Add(new ToolStripMenuItem(Strings.ThemeInherit, null, (_, _) => SetTheme(null)) { Checked = Info.Theme == null });
            // Grouped (Basic, Gaming & tech, …); the group holding the current style is checked too
            foreach (var group in ThemeRegistry.All.GroupBy(ThemeRegistry.GroupOf).OrderBy(g => g.Key))
            {
                var sub = new ToolStripMenuItem(Strings.ThemeGroupName(group.Key)) { Checked = group.Any(t => t.Id == Info.Theme) };
                foreach (var t in group)
                    sub.DropDownItems.Add(new ToolStripMenuItem(t.DisplayName, null, (_, _) => SetTheme(t.Id)) { Checked = Info.Theme == t.Id });
                style.DropDownItems.Add(sub);
            }
            style.DropDownItems.Add(new ToolStripSeparator());
            style.DropDownItems.Add(Strings.DesignerMenu, null, (_, _) => app.OpenStyleDesigner(Info));
            menu.Items.Add(style);

            var sort = new ToolStripMenuItem(Strings.SortBy);
            foreach (var mode in Enum.GetValues<FenceSortMode>())
                sort.DropDownItems.Add(new ToolStripMenuItem(Strings.SortModeName(mode), null, (_, _) => SetSortMode(mode)) { Checked = Info.SortMode == mode });
            if (!IsNote)
                menu.Items.Add(sort);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.NewFence, null, (_, _) => app.CreateFence(FenceKind.Links));
            menu.Items.Add(Strings.NewFolderFence, null, (_, _) => app.CreateFence(FenceKind.Folder));
            menu.Items.Add(Strings.NewNote, null, (_, _) => app.CreateFence(FenceKind.Note));
            app.AddCreateExtrasItems(menu.Items);
            menu.Items.Add(new ToolStripSeparator());
            app.AddToolItems(menu.Items);
            app.AddAppSettingsItems(menu.Items);
            menu.Items.Add(new ToolStripMenuItem(Strings.Autostart, null, (_, _) => NoFencesApp.ToggleAutostart()) { Checked = SystemSettings.AutostartEnabled });
            NoFencesApp.AddDocumentItems(menu.Items);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.DeleteFence, null, (_, _) => ConfirmDelete());

            appMenuOpen = true;
            menu.Closed += (_, _) =>
            {
                appMenuOpen = false;
                BeginInvoke(menu.Dispose);
            };
            menu.Show(this, location);
        }

        private void SetSortMode(FenceSortMode mode)
        {
            if (mode == FenceSortMode.Manual && Info.SortMode != FenceSortMode.Manual)
            {
                // Start manual ordering from what the user currently sees.
                var visible = entries.Select(e => e.Path).ToList();
                Info.Files = visible.Concat(Info.Files.Where(f => !visible.Contains(f, StringComparer.OrdinalIgnoreCase))).ToList();
            }
            Info.SortMode = mode;
            app.RequestSave();
            ReloadEntries();
        }

        private void SetTheme(string? id)
        {
            Info.Theme = id;
            app.RequestSave();
            ApplySettings();
        }

        private void ToggleCollapse()
        {
            Info.CanMinify = !Info.CanMinify;
            app.RequestSave();
            if (Info.CanMinify)
                collapseTimer.Start();
            else
                Expand();
        }

        private void RemoveLink(string path)
        {
            app.RecordUndo(Strings.UndoRemoveItems(1, Info.Name), new[] { Info.Id });
            Info.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            app.RequestSave();
            ReloadEntries();
        }

        private void OpenSettings()
        {
            using var dialog = new FenceSettingsDialog(Info);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            dialog.ApplyTo(Info);
            if (!collapsed)
            {
                suppressBoundsSave = true;
                Size = new Size(Info.Width, Info.Height);
                suppressBoundsSave = false;
            }
            app.RequestSave();
            ApplySettings();
            ApplyZOrder(); // "Always on top" may have changed
            ReloadEntries();
            app.FenceSettingsChanged(Info);
        }

        private void ConfirmDelete()
        {
            var text = Strings.ReallyDelete(Info.Name);
            if (Info.Kind == FenceKind.Folder)
                text += "\n\n" + Strings.ReallyDeleteFolderNote;
            if (MessageBox.Show(this, text, "NoFences", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                app.RemoveFence(this);
        }

        #endregion

        #region Drag & drop

        private void StartDrag(List<string> paths)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, paths.ToArray());
            data.SetData(InternalDragFormat, $"{Info.Id}|{string.Join('\n', paths)}");

            lastDropWasFenceMove = false;
            var effect = DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

            if (Info.Kind == FenceKind.Links && effect == DragDropEffects.Move && lastDropWasFenceMove)
            {
                Info.Files.RemoveAll(f => paths.Contains(f, StringComparer.OrdinalIgnoreCase));
                app.RequestSave();
            }
            lastDropWasFenceMove = false;
            ReloadEntries();
        }

        private static (Guid fence, string[] paths)? GetInternal(IDataObject? data)
        {
            if (data?.GetData(InternalDragFormat) is not string s)
                return null;
            var sep = s.IndexOf('|');
            return sep > 0 && Guid.TryParse(s[..sep], out var id) ? (id, s[(sep + 1)..].Split('\n')) : null;
        }

        private DragDropEffects ComputeEffect(DragEventArgs e)
        {
            if (IsNote)
                return !Info.Locked && !Editing && e.Data?.GetDataPresent(DataFormats.UnicodeText) == true ? DragDropEffects.Copy : DragDropEffects.None;
            if (IsWidget)
                return !Info.Locked && e.Data != null && widget?.AcceptsDrop(e.Data) == true ? DragDropEffects.Move : DragDropEffects.None;
            if (Info.Locked || Info.ReadOnly || e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return DragDropEffects.None;

            var internalItem = GetInternal(e.Data);
            if (internalItem?.fence == Info.Id)
            {
                // Reorder; items inside an opened folder keep the folder's order
                return Info.SortMode == FenceSortMode.Manual && !internalItem.Value.paths.Any(IsChildPath) ? DragDropEffects.Move : DragDropEffects.None;
            }

            var allowed = e.AllowedEffect;
            if (Info.Kind == FenceKind.Links)
            {
                // From another fence: move between fences. From Explorer: just link.
                if (internalItem != null && allowed.HasFlag(DragDropEffects.Move))
                    return DragDropEffects.Move;
                return allowed.HasFlag(DragDropEffects.Link) ? DragDropEffects.Link : allowed & DragDropEffects.Copy;
            }

            // Folder fence: Explorer semantics (same drive = move, Ctrl = copy, Shift = move).
            if (string.IsNullOrEmpty(Info.FolderPath) || !Directory.Exists(Info.FolderPath))
                return DragDropEffects.None;
            var files = e.Data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
            if (files.All(f => string.Equals(Path.GetDirectoryName(f), Info.FolderPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                return DragDropEffects.None;

            bool ctrl = (e.KeyState & 8) != 0, shift = (e.KeyState & 4) != 0;
            var sameDrive = files.All(f => string.Equals(Path.GetPathRoot(f), Path.GetPathRoot(Info.FolderPath), StringComparison.OrdinalIgnoreCase));
            var wantMove = shift || (!ctrl && sameDrive);
            if (wantMove && allowed.HasFlag(DragDropEffects.Move))
                return DragDropEffects.Move;
            return allowed & DragDropEffects.Copy;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            if (collapsed && !Info.Folded)
                Expand();
            UpdateDrag(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            UpdateDrag(e);
        }

        private void UpdateDrag(DragEventArgs e)
        {
            // Dragging onto another tab's chip moves the links there.
            var tabUnder = HasTabs ? TabAt(PointToClient(new Point(e.X, e.Y))) : -2;
            if (tabUnder >= 0 && tabUnder != Info.ActiveTab && !Info.Locked && e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = GetInternal(e.Data)?.fence == Info.Id ? DragDropEffects.Move : (e.AllowedEffect & (DragDropEffects.Link | DragDropEffects.Copy | DragDropEffects.Move));
                if (dropTab != tabUnder || insertIndex != -1)
                {
                    dropTab = tabUnder;
                    insertIndex = -1;
                    Invalidate();
                }
                return;
            }
            if (dropTab != -1)
            {
                dropTab = -1;
                Invalidate();
            }
            e.Effect = ComputeEffect(e);
            // With automatic sorting the drop position doesn't matter, so don't show a marker.
            var newIndex = e.Effect == DragDropEffects.None || Info.SortMode != FenceSortMode.Manual
                ? -1
                : InsertIndexAt(PointToClient(new Point(e.X, e.Y)));
            if (newIndex != insertIndex)
            {
                insertIndex = newIndex;
                Invalidate();
            }
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            insertIndex = -1;
            dropTab = -1;
            Invalidate();
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            if (HasTabs && dropTab >= 0)
            {
                var tab = dropTab;
                dropTab = -1;
                var dropped = e.Data?.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
                var fromHere = GetInternal(e.Data)?.fence == Info.Id;
                RecordDropUndo(e.Data, Strings.UndoTabs(Info.Name));
                MoveToTab(dropped, tab, removeFromCurrent: fromHere);
                // From another links fence with Move: that fence removes them
                lastDropWasFenceMove = !fromHere && GetInternal(e.Data) != null && e.Effect == DragDropEffects.Move;
                return;
            }
            var effect = ComputeEffect(e);
            var index = InsertIndexAt(PointToClient(new Point(e.X, e.Y)));
            insertIndex = -1;
            if (effect == DragDropEffects.None)
            {
                Invalidate();
                return;
            }
            e.Effect = effect;

            if (IsNote)
            {
                AppendDroppedText(e.Data?.GetData(DataFormats.UnicodeText) as string ?? "");
                return;
            }

            if (IsWidget)
            {
                if (e.Data != null)
                    widget?.Drop(e.Data, this);
                Invalidate();
                return;
            }

            var files = e.Data?.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
            var internalItem = GetInternal(e.Data);

            if (internalItem?.fence == Info.Id)
            {
                RecordDropUndo(e.Data, Strings.UndoMoveItems(Info.Name));
                MoveInOrder(internalItem.Value.paths, index);
            }
            else if (Info.Kind == FenceKind.Links)
            {
                RecordDropUndo(e.Data, internalItem != null && effect == DragDropEffects.Move ? Strings.UndoMoveItems(Info.Name) : Strings.UndoAddItems(Info.Name));
                InsertInOrder(files.Where(f => File.Exists(f) || Directory.Exists(f)), index);
                lastDropWasFenceMove = internalItem != null && effect == DragDropEffects.Move;
            }
            else
            {
                var ok = effect == DragDropEffects.Move
                    ? ShellFileOps.Move(this, files, Info.FolderPath!)
                    : ShellFileOps.Copy(this, files, Info.FolderPath!);
                if (ok)
                    InsertInOrder(files.Select(f => Path.Combine(Info.FolderPath!, Path.GetFileName(f.TrimEnd('\\')))), index);
            }

            app.RequestSave();
            ReloadEntries();
        }

        /// <summary>Remembers this fence and, for links dragged from another fence, that one too (it loses them).</summary>
        private void RecordDropUndo(IDataObject? data, string description)
        {
            var source = GetInternal(data)?.fence;
            app.RecordUndo(description, source is { } other && other != Info.Id ? new[] { Info.Id, other } : new[] { Info.Id });
        }

        /// <summary>Converts an index into the visible entry list into an index into <see cref="FenceInfo.Files"/>.</summary>
        private int FilesIndexFor(int entryIndex)
        {
            if (entryIndex >= entries.Count)
                return Info.Files.Count;
            var path = entries[entryIndex].Path;
            var i = Info.Files.FindIndex(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            return i < 0 ? Info.Files.Count : i;
        }

        /// <summary>Moves items (keeping their order) in front of the entry at <paramref name="entryIndex"/>.</summary>
        private void MoveInOrder(IEnumerable<string> paths, int entryIndex) => InsertInOrder(paths, entryIndex);

        private void InsertInOrder(IEnumerable<string> paths, int entryIndex)
        {
            var target = FilesIndexFor(entryIndex);
            foreach (var path in paths)
            {
                var existing = Info.Files.FindIndex(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (existing >= 0)
                {
                    Info.Files.RemoveAt(existing);
                    if (existing < target)
                        target--;
                }
                Info.Files.Insert(Math.Clamp(target, 0, Info.Files.Count), path);
                target++;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IconCache.Shared.ImageLoaded -= IconCache_ImageLoaded;
                DisposeBackgroundPicture();
                watcher?.Dispose();
                collapseTimer.Dispose();
                HideHoverPreview();
                hoverPreviewTimer.Dispose();
                refreshTimer.Dispose();
                linkPollTimer.Dispose();
                titleFont?.Dispose();
                labelFont?.Dispose();
                labelFormat.Dispose();
                labelFormatSingleLine.Dispose();
                DisposeNote();
                DisposeWidget();
                widgetTimer.Dispose();
                toolTip.Dispose();
                DisposeAnimations();
                shellContextMenu.DestroyHandle();
            }
            base.Dispose(disposing);
        }
    }
}
