using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Selection (click, Ctrl/Shift-click, rubber band), keyboard shortcuts and type-to-search.
    /// A fence takes keyboard focus when clicked (like the desktop does) but stays at the bottom.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private readonly HashSet<string> selection = new(StringComparer.OrdinalIgnoreCase);
        private string? anchorPath;
        private string? shiftAnchor;
        private Point? bandStart;
        private Rectangle band;
        private string search = "";

        private bool IsSelected(string path) => selection.Contains(path);

        /// <summary>Selected paths in display order.</summary>
        private List<string> SelectedInOrder() => entries.Where(e => selection.Contains(e.Path)).Select(e => e.Path).ToList();

        private void SelectOnly(string? path)
        {
            selection.Clear();
            if (path != null)
                selection.Add(path);
            anchorPath = path;
        }

        private void SelectRange(int from, int to)
        {
            selection.Clear();
            for (var i = Math.Min(from, to); i <= Math.Max(from, to) && i < entries.Count; i++)
                selection.Add(entries[i].Path);
        }

        private int IndexOf(string? path) => path == null ? -1 : entries.FindIndex(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        private void SelectionMouseDown(MouseEventArgs e)
        {
            var index = HitTestItem(e.Location);
            var path = index >= 0 ? entries[index].Path : null;

            if (path == null)
            {
                // Empty space below the title starts a rubber band.
                if ((ModifierKeys & Keys.Control) == 0)
                    SelectOnly(null);
                if (e.Y >= titleHeight)
                {
                    bandStart = e.Location;
                    band = Rectangle.Empty;
                }
            }
            else if ((ModifierKeys & Keys.Control) != 0)
            {
                if (!selection.Remove(path))
                    selection.Add(path);
                anchorPath = path;
            }
            else if ((ModifierKeys & Keys.Shift) != 0 && IndexOf(anchorPath) >= 0)
            {
                SelectRange(IndexOf(anchorPath), index);
            }
            else if (!selection.Contains(path))
            {
                SelectOnly(path);
            }

            mouseDownAt = e.Location;
            mouseDownPath = path;
            Invalidate();
        }

        private void UpdateBand(Point location)
        {
            if (bandStart is not Point start)
                return;
            band = Rectangle.FromLTRB(Math.Min(start.X, location.X), Math.Min(start.Y, location.Y), Math.Max(start.X, location.X), Math.Max(start.Y, location.Y));
            if ((ModifierKeys & Keys.Control) == 0)
                selection.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                if (ToClient(itemRects[i]).IntersectsWith(band))
                    selection.Add(entries[i].Path);
            }
            Invalidate();
        }

        private void SelectionMouseUp(MouseEventArgs e)
        {
            if (bandStart != null)
            {
                bandStart = null;
                band = Rectangle.Empty;
                Invalidate();
                return;
            }
            // Plain click on an item of a multi-selection (no drag happened): keep just that one.
            if (e.Button == MouseButtons.Left && mouseDownPath != null && ModifierKeys == Keys.None && selection.Count > 1)
            {
                SelectOnly(mouseDownPath);
                Invalidate();
            }
        }

        private void DrawBand(Graphics g)
        {
            if (band.Width < 2 && band.Height < 2)
                return;
            using var fill = new SolidBrush(Color.FromArgb(50, SystemColors.Highlight));
            using var pen = new Pen(Color.FromArgb(180, SystemColors.Highlight));
            g.FillRectangle(fill, band);
            g.DrawRectangle(pen, band.X, band.Y, band.Width - 1, band.Height - 1);
        }

        #region Keyboard

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (IsNote || IsWidget || Editing)
                return base.ProcessCmdKey(ref msg, keyData);

            var key = keyData & Keys.KeyCode;
            var shift = (keyData & Keys.Shift) != 0;
            var ctrl = (keyData & Keys.Control) != 0;

            switch (key)
            {
                case Keys.Enter:
                    foreach (var path in SelectedInOrder().Take(15))
                        FenceEntry.FromPath(path)?.Open();
                    return true;
                case Keys.Delete:
                    DeleteSelection();
                    return true;
                case Keys.F2:
                    RenameSelection();
                    return true;
                case Keys.Escape:
                    if (search.Length > 0)
                        SetSearch("");
                    else
                        SelectOnly(null);
                    Invalidate();
                    return true;
                case Keys.A when ctrl:
                    foreach (var entry in entries)
                        selection.Add(entry.Path);
                    Invalidate();
                    return true;
                case Keys.C when ctrl:
                    CopySelection();
                    return true;
                case Keys.Left or Keys.Right or Keys.Up or Keys.Down:
                    MoveFocus(key, shift);
                    return true;
                case Keys.Back when search.Length > 0:
                    SetSearch(search[..^1]);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (IsNote || IsWidget || Editing || char.IsControl(e.KeyChar))
                return;
            SetSearch(search + e.KeyChar);
            e.Handled = true;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (search.Length > 0)
                SetSearch("");
        }

        private void SetSearch(string text)
        {
            search = text;
            scrollOffset = 0;
            ReloadEntries();
        }

        /// <summary>Applied by <see cref="ReloadEntries"/>: keeps only entries whose name contains the search text.</summary>
        private List<FenceEntry> ApplySearch(List<FenceEntry> list) =>
            search.Length == 0
                ? list
                : list.Where(e => e.GetDisplayName(true).Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToList();

        private void DrawSearch(Graphics g)
        {
            if (search.Length == 0 || labelFont == null)
                return;
            var text = "🔍 " + search;
            var size = g.MeasureString(text, labelFont);
            var inset = Px(10 + theme.ContentInset);
            var rect = new RectangleF(ClientSize.Width - inset - size.Width - Px(8), titleHeight + Px(4), size.Width + Px(8), size.Height + Px(2));
            using (var back = new SolidBrush(Color.FromArgb(200, 20, 20, 24)))
                g.FillRectangle(back, rect);
            g.DrawString(text, labelFont, Brushes.White, rect.X + Px(4), rect.Y + Px(1));
        }

        private void MoveFocus(Keys key, bool extend)
        {
            if (entries.Count == 0)
                return;
            var current = IndexOf(anchorPath);
            var columns = Math.Max(1, itemRects.Count(r => r.Top == itemRects[0].Top));
            var next = current < 0 ? 0 : key switch
            {
                Keys.Left => current - 1,
                Keys.Right => current + 1,
                Keys.Up => current - columns,
                _ => current + columns
            };
            next = Math.Clamp(next, 0, entries.Count - 1);

            if (extend)
            {
                // Shift+arrows grow a range from where the Shift sequence started.
                shiftAnchor ??= anchorPath ?? entries[next].Path;
                var start = IndexOf(shiftAnchor);
                SelectRange(start >= 0 ? start : next, next);
                anchorPath = entries[next].Path;
            }
            else
            {
                shiftAnchor = null;
                SelectOnly(entries[next].Path);
            }
            ScrollIntoView(next);
            Invalidate();
        }

        private void ScrollIntoView(int index)
        {
            var r = itemRects[index];
            if (r.Top < scrollOffset)
                scrollOffset = Math.Max(0, r.Top - Px(8));
            else if (r.Bottom > scrollOffset + ViewHeight)
                scrollOffset = Math.Min(MaxScroll, r.Bottom - ViewHeight + Px(8));
        }

        #endregion

        #region Actions

        private void CopySelection()
        {
            var paths = SelectedInOrder();
            if (paths.Count == 0)
                return;
            var files = new System.Collections.Specialized.StringCollection();
            files.AddRange(paths.ToArray());
            Clipboard.SetFileDropList(files);
        }

        /// <summary>Links fence: removes the links only. Folder fence: recycle bin (Explorer confirms).</summary>
        private void DeleteSelection()
        {
            var paths = SelectedInOrder();
            if (paths.Count == 0 || Info.Locked || Info.ReadOnly)
                return;
            if (Info.Kind == FenceKind.Links)
            {
                Info.Files.RemoveAll(f => paths.Contains(f, StringComparer.OrdinalIgnoreCase));
                app.RequestSave();
            }
            else if (Info.Kind == FenceKind.Folder)
            {
                ShellFileOps.Recycle(this, paths);
            }
            selection.Clear();
            ReloadEntries();
        }

        /// <summary>Renames the file or folder on disk (like F2 in Explorer) and keeps its place in the fence.</summary>
        private void RenameSelection()
        {
            var path = anchorPath ?? SelectedInOrder().FirstOrDefault();
            if (path == null || Info.Locked || Info.ReadOnly)
                return;
            var name = Path.GetFileName(path.TrimEnd('\\'));
            using var dialog = new InputDialog(Strings.Rename, Strings.NewName, name, selectStem: File.Exists(path));
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            var newName = dialog.Value.Trim();
            if (newName.Length == 0 || newName == name)
                return;

            var target = Path.Combine(Path.GetDirectoryName(path.TrimEnd('\\')) ?? "", newName);
            try
            {
                if (Directory.Exists(path))
                    Directory.Move(path, target);
                else
                    File.Move(path, target);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, Strings.RenameFailed(e.Message), "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var i = Info.Files.FindIndex(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (i >= 0)
                Info.Files[i] = target;
            app.RequestSave();
            SelectOnly(target);
            ReloadEntries();
        }

        #endregion
    }
}
