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
    public sealed class FenceWindow : Form
    {
        private const string InternalDragFormat = "NoFences.Item";
        private const int ResizeBorder = 8;

        // Set by a links fence that accepted an item moved out of another fence, so the source can drop it.
        private static bool lastDropWasFenceMove;

        private readonly NoFencesApp app;
        private readonly ShellContextMenu shellContextMenu = new();
        private readonly System.Windows.Forms.Timer collapseTimer = new() { Interval = 200 };
        private readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 300 };
        private readonly System.Windows.Forms.Timer linkPollTimer = new() { Interval = 5000 };
        private readonly StringFormat labelFormat = new() { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

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
        private string? selectedPath;
        private Point? mouseDownAt;
        private string? mouseDownPath;
        private int insertIndex = -1;
        private bool collapsed;
        private bool suppressBoundsSave;

        private FileSystemWatcher? watcher;

        public FenceInfo Info { get; }

        public FenceWindow(NoFencesApp app, FenceInfo info)
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
            Native.SendToBottom(Handle);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplySettings();
            EnsureOnScreen();
            ReloadEntries();
            linkPollTimer.Start();
            if (Info.CanMinify)
                Collapse();
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

            if (IsHandleCreated)
                Native.SetCornerPreference(Handle, theme.CornerPreference);

            SetupWatcher();
            if (collapsed)
                SetHeightSilently(titleHeight);
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
                app.Store.RequestSave();
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

        private int ViewHeight => Math.Max(0, ClientSize.Height - titleHeight);

        private int MaxScroll => Math.Max(0, contentHeight - ViewHeight);

        private void Relayout()
        {
            iconPx = Px(Info.IconSize);
            itemWidth = Math.Max(Px(75), iconPx + Px(28));
            itemHeight = Px(4) + iconPx + Px(4) + (labelFont?.Height ?? Px(16)) * 2 + Px(4);

            var pad = Px(10 + theme.ContentInset);
            var gap = Px(8);
            var usable = ClientSize.Width - 2 * pad - Px(6); // leave room for the scrollbar
            var columns = Math.Max(1, (usable + gap) / (itemWidth + gap));

            itemRects.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                var col = i % columns;
                var row = i / columns;
                itemRects.Add(new Rectangle(pad + col * (itemWidth + gap), pad + row * (itemHeight + gap), itemWidth, itemHeight));
            }

            contentHeight = itemRects.Count == 0 ? 0 : itemRects[^1].Bottom + pad;
            scrollOffset = Math.Clamp(scrollOffset, 0, MaxScroll);
        }

        private Rectangle ToClient(Rectangle contentRect) => new(contentRect.X, contentRect.Y + titleHeight - scrollOffset, contentRect.Width, contentRect.Height);

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

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var bounds = ClientRectangle;
            theme.DrawFrame(g, bounds, titleHeight, Info, scale);
            if (titleFont != null)
                theme.DrawTitle(g, new Rectangle(0, 0, bounds.Width, titleHeight), theme.FormatTitle(Text), titleFont, scale);

            if (collapsed || labelFont == null)
                return;

            var view = new Rectangle(0, titleHeight, bounds.Width, ViewHeight);
            g.SetClip(view);

            if (entries.Count == 0)
                DrawEmptyHint(g, view);

            for (var i = 0; i < entries.Count; i++)
            {
                var r = ToClient(itemRects[i]);
                if (!r.IntersectsWith(view))
                    continue;

                var entry = entries[i];
                theme.DrawItemBackground(g, r, entry.Path == hoverPath, entry.Path == selectedPath, scale);

                var icon = IconCache.Shared.Get(entry.Path, iconPx);
                if (icon != null)
                {
                    // Shell images can be smaller than requested (e.g. wide thumbnails); center them in the icon box.
                    var ix = r.X + (r.Width - icon.Width) / 2;
                    var iy = r.Y + Px(4) + (iconPx - icon.Height) / 2;
                    g.DrawImage(icon, ix, iy, icon.Width, icon.Height);
                }

                var labelTop = r.Y + Px(4) + iconPx + Px(4);
                var labelRect = new RectangleF(r.X + Px(2), labelTop, r.Width - Px(4), r.Bottom - labelTop);
                theme.DrawLabel(g, entry.GetDisplayName(app.ShowExtensions), labelRect, labelFont, labelFormat, scale);
            }

            if (insertIndex >= 0)
                DrawInsertMarker(g);

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
            using var brush = new SolidBrush(Color.FromArgb(150, Color.White));
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

                case Native.WM_MOUSEACTIVATE:
                    // Clicking a fence must not bring it in front of other windows.
                    m.Result = new IntPtr(Native.MA_NOACTIVATE);
                    return;

                case Native.WM_SETFOCUS:
                    Native.SendToBottom(Handle);
                    return;

                case Native.WM_WINDOWPOSCHANGING:
                    KeepAtBottom(m.LParam);
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

        private static void KeepAtBottom(IntPtr lParam)
        {
            var pos = Marshal.PtrToStructure<Native.WINDOWPOS>(lParam);
            if ((pos.flags & Native.SWP_NOZORDER) == 0 && pos.hwndInsertAfter != Native.HWND_BOTTOM)
            {
                pos.hwndInsertAfter = Native.HWND_BOTTOM;
                Marshal.StructureToPtr(pos, lParam, false);
            }
        }

        private int HitTest(Point pt, int current)
        {
            if (collapsed && Info.CanMinify)
                Expand();

            // Right clicks always go to the client area so our context menu shows instead of the system menu.
            if (current != Native.HTCLIENT || Info.Locked || MouseButtons == MouseButtons.Right)
                return current;

            var b = Px(ResizeBorder);
            var w = ClientSize.Width;
            var h = ClientSize.Height;
            bool left = pt.X < b, right = pt.X >= w - b, top = pt.Y < b / 2, bottom = pt.Y >= h - b;

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
            if (pt.Y < titleHeight) return Native.HTCAPTION;
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
            collapseTimer.Stop();
            SetHeightSilently(titleHeight);
            Invalidate();
        }

        private void Expand()
        {
            if (!collapsed)
                return;
            collapsed = false;
            SetHeightSilently(Math.Max(Info.Height, titleHeight + Px(40)));
            Relayout();
            Invalidate();
            collapseTimer.Start();
        }

        private void CollapseIfMouseAway()
        {
            if (!Info.CanMinify || collapsed)
            {
                collapseTimer.Stop();
                return;
            }
            // Don't collapse while the user is dragging, resizing or has a menu open.
            if (MouseButtons != MouseButtons.None || Capture || appMenuOpen)
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
            if (suppressBoundsSave || !IsHandleCreated)
                return;
            Info.PosX = Left;
            Info.PosY = Top;
            app.Store.RequestSave();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
            Invalidate();
            if (suppressBoundsSave || collapsed || !IsHandleCreated)
                return;
            Info.Width = Width;
            Info.Height = Height;
            app.Store.RequestSave();
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
            if (collapsed && Info.CanMinify)
                Expand();

            var index = HitTestItem(e.Location);
            var path = index >= 0 ? entries[index].Path : null;
            if (path != hoverPath)
            {
                hoverPath = path;
                Invalidate();
            }

            if (mouseDownAt is Point start && mouseDownPath != null && e.Button == MouseButtons.Left)
            {
                var drag = SystemInformation.DragSize;
                if (Math.Abs(e.X - start.X) > drag.Width || Math.Abs(e.Y - start.Y) > drag.Height)
                {
                    var p = mouseDownPath;
                    mouseDownAt = null;
                    mouseDownPath = null;
                    StartDrag(p);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverPath != null)
            {
                hoverPath = null;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            var index = HitTestItem(e.Location);
            selectedPath = index >= 0 ? entries[index].Path : null;
            mouseDownAt = e.Location;
            mouseDownPath = selectedPath;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            mouseDownAt = null;
            mouseDownPath = null;

            if (e.Button != MouseButtons.Right)
                return;

            var index = HitTestItem(e.Location);
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
            var index = HitTestItem(e.Location);
            if (index >= 0)
                entries[index].Open();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
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
                if (entry.IsFolder)
                    shellContextMenu.ShowContextMenu(new[] { new DirectoryInfo(entry.Path) }, Cursor.Position);
                else
                    shellContextMenu.ShowContextMenu(new[] { new FileInfo(entry.Path) }, Cursor.Position);
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

            if (entry != null && Info.Kind == FenceKind.Links)
                menu.Items.Add(Strings.RemoveItem, null, (_, _) => RemoveLink(entry.Path));
            if (Info.Kind == FenceKind.Folder && Directory.Exists(Info.FolderPath))
                menu.Items.Add(Strings.OpenFolder, null, (_, _) => FenceEntry.FromPath(Info.FolderPath!)?.Open());
            if (menu.Items.Count > 0)
                menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(Strings.Settings, null, (_, _) => OpenSettings());
            menu.Items.Add(new ToolStripMenuItem(Strings.Locked, null, (_, _) => { Info.Locked = !Info.Locked; app.Store.RequestSave(); }) { Checked = Info.Locked });
            menu.Items.Add(new ToolStripMenuItem(Strings.AutoCollapse, null, (_, _) => ToggleCollapse()) { Checked = Info.CanMinify });

            var style = new ToolStripMenuItem(Strings.Theme);
            style.DropDownItems.Add(new ToolStripMenuItem(Strings.ThemeInherit, null, (_, _) => SetTheme(null)) { Checked = Info.Theme == null });
            foreach (var t in ThemeRegistry.All)
                style.DropDownItems.Add(new ToolStripMenuItem(t.DisplayName, null, (_, _) => SetTheme(t.Id)) { Checked = Info.Theme == t.Id });
            menu.Items.Add(style);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.NewFence, null, (_, _) => app.CreateFence(FenceKind.Links));
            menu.Items.Add(Strings.NewFolderFence, null, (_, _) => app.CreateFence(FenceKind.Folder));
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

        private void SetTheme(string? id)
        {
            Info.Theme = id;
            app.Store.RequestSave();
            ApplySettings();
        }

        private void ToggleCollapse()
        {
            Info.CanMinify = !Info.CanMinify;
            app.Store.RequestSave();
            if (Info.CanMinify)
                collapseTimer.Start();
            else
                Expand();
        }

        private void RemoveLink(string path)
        {
            Info.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            app.Store.RequestSave();
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
            app.Store.RequestSave();
            ApplySettings();
            ReloadEntries();
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

        private void StartDrag(string path)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, new[] { path });
            data.SetData(InternalDragFormat, $"{Info.Id}|{path}");

            lastDropWasFenceMove = false;
            var effect = DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

            if (Info.Kind == FenceKind.Links && effect == DragDropEffects.Move && lastDropWasFenceMove)
                RemoveLink(path);
            lastDropWasFenceMove = false;
            ReloadEntries();
        }

        private static (Guid fence, string path)? GetInternal(IDataObject? data)
        {
            if (data?.GetData(InternalDragFormat) is not string s)
                return null;
            var sep = s.IndexOf('|');
            return sep > 0 && Guid.TryParse(s[..sep], out var id) ? (id, s[(sep + 1)..]) : null;
        }

        private DragDropEffects ComputeEffect(DragEventArgs e)
        {
            if (Info.Locked || e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return DragDropEffects.None;

            var internalItem = GetInternal(e.Data);
            if (internalItem?.fence == Info.Id)
                return DragDropEffects.Move; // reorder

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
            if (collapsed)
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
            e.Effect = ComputeEffect(e);
            var newIndex = e.Effect == DragDropEffects.None ? -1 : InsertIndexAt(PointToClient(new Point(e.X, e.Y)));
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
            Invalidate();
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            var effect = ComputeEffect(e);
            var index = InsertIndexAt(PointToClient(new Point(e.X, e.Y)));
            insertIndex = -1;
            if (effect == DragDropEffects.None)
            {
                Invalidate();
                return;
            }
            e.Effect = effect;

            var files = e.Data?.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
            var internalItem = GetInternal(e.Data);

            if (internalItem?.fence == Info.Id)
            {
                MoveInOrder(internalItem.Value.path, index);
            }
            else if (Info.Kind == FenceKind.Links)
            {
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

            app.Store.RequestSave();
            ReloadEntries();
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

        private void MoveInOrder(string path, int entryIndex)
        {
            var target = FilesIndexFor(entryIndex);
            var current = Info.Files.FindIndex(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (current < 0)
                return;
            Info.Files.RemoveAt(current);
            if (current < target)
                target--;
            Info.Files.Insert(Math.Clamp(target, 0, Info.Files.Count), path);
        }

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
                watcher?.Dispose();
                collapseTimer.Dispose();
                refreshTimer.Dispose();
                linkPollTimer.Dispose();
                titleFont?.Dispose();
                labelFont?.Dispose();
                labelFormat.Dispose();
                shellContextMenu.DestroyHandle();
            }
            base.Dispose(disposing);
        }
    }
}
