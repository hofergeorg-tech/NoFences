using System.Diagnostics;
using System.Drawing.Drawing2D;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Folders that open inside the fence (their contents indented below them, one level), notes on
    /// single items (tooltip and a small mark) and a program that opens the fence's files.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private const int MaxChildren = 60;

        /// <summary>Per entry: 0 = in the fence itself, 1 = inside an opened folder.</summary>
        private List<int> entryDepth = new();

        private int Depth(int index) => index < entryDepth.Count ? entryDepth[index] : 0;

        private bool IsExpanded(string path) => Info.ExpandedFolders?.Contains(path, StringComparer.OrdinalIgnoreCase) == true;

        private bool IsChildPath(string path)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (Depth(i) > 0 && entries[i].Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Called by ReloadEntries: puts the contents of opened folders after them.</summary>
        private void AddExpandedChildren()
        {
            entryDepth = entries.Select(_ => 0).ToList();
            if (Info.ExpandedFolders is not { Count: > 0 } || Info.Compact)
                return;
            var result = new List<FenceEntry>();
            var depth = new List<int>();
            foreach (var entry in entries)
            {
                result.Add(entry);
                depth.Add(0);
                if (!entry.IsFolder || !IsExpanded(entry.Path))
                    continue;
                foreach (var child in FolderChildren(entry.Path))
                {
                    result.Add(child);
                    depth.Add(1);
                }
            }
            entries = result;
            entryDepth = depth;
        }

        /// <summary>The visible contents of a folder: folders first, then by name, at most 60.</summary>
        internal static List<FenceEntry> FolderChildren(string folder)
        {
            try
            {
                return new DirectoryInfo(folder).EnumerateFileSystemInfos()
                    .Where(f => (f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    .OrderBy(f => f is FileInfo)
                    .ThenBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Take(MaxChildren)
                    .Select(f => FenceEntry.FromPath(f.FullName))
                    .OfType<FenceEntry>()
                    .ToList();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                return new List<FenceEntry>();
            }
        }

        private void ToggleExpanded(string path)
        {
            Info.ExpandedFolders ??= new List<string>();
            if (IsExpanded(path))
                Info.ExpandedFolders.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
            else
                Info.ExpandedFolders.Add(path);
            if (Info.ExpandedFolders.Count == 0)
                Info.ExpandedFolders = null;
            app.RequestSave();
            ReloadEntries();
        }

        /// <summary>The small arrow in a folder's top left corner (client coordinates).</summary>
        private Rectangle ChevronRect(Rectangle item) => new(item.X + Px(2), item.Y + Px(2), Px(16), Px(16));

        /// <summary>A click on a folder's arrow opens/closes it in the fence.</summary>
        private bool ChevronClick(Point client)
        {
            if (Info.Compact)
                return false;
            var index = HitTestItem(client);
            if (index < 0 || Depth(index) > 0 || !entries[index].IsFolder)
                return false;
            if (!ChevronRect(ToClient(itemRects[index])).Contains(client))
                return false;
            ToggleExpanded(entries[index].Path);
            return true;
        }

        /// <summary>Arrow on folders: always on opened ones, on the others while hovered.</summary>
        private void DrawChevron(Graphics g, int index, Rectangle item)
        {
            if (Info.Compact || Depth(index) > 0 || !entries[index].IsFolder)
                return;
            var expanded = IsExpanded(entries[index].Path);
            if (!expanded && entries[index].Path != hoverPath)
                return;
            var r = ChevronRect(item);
            using (var back = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                g.FillEllipse(back, r);
            using var pen = new Pen(Color.White, Math.Max(1.5f, 1.6f * scale)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, s = r.Width * 0.18f;
            if (expanded)
                g.DrawLines(pen, new[] { new PointF(cx - s * 1.3f, cy - s * 0.6f), new PointF(cx, cy + s * 0.7f), new PointF(cx + s * 1.3f, cy - s * 0.6f) });
            else
                g.DrawLines(pen, new[] { new PointF(cx - s * 0.6f, cy - s * 1.3f), new PointF(cx + s * 0.7f, cy), new PointF(cx - s * 0.6f, cy + s * 1.3f) });
        }

        /// <summary>A soft band behind the contents of each opened folder.</summary>
        private void DrawChildBands(Graphics g, Rectangle view)
        {
            var start = -1;
            for (var i = 0; i <= entries.Count; i++)
            {
                var child = i < entries.Count && Depth(i) > 0;
                if (child && start < 0)
                    start = i;
                if (child || start < 0)
                    continue;
                var band = Rectangle.Union(ToClient(itemRects[start]), ToClient(itemRects[i - 1]));
                for (var j = start; j < i; j++)
                    band = Rectangle.Union(band, ToClient(itemRects[j]));
                band.Inflate(Px(5), Px(3));
                start = -1;
                if (!band.IntersectsWith(view))
                    continue;
                using var brush = new SolidBrush(Color.FromArgb(40, theme.Accent));
                using var line = new Pen(Color.FromArgb(120, theme.Accent), Math.Max(2, Px(2)));
                g.FillRectangle(brush, band);
                g.DrawLine(line, band.Left, band.Top, band.Left, band.Bottom);
            }
        }

        #region Item notes

        private string? NoteOf(string path) => Info.ItemNotes != null && Info.ItemNotes.TryGetValue(path, out var note) ? note : null;

        private void EditItemNote(string path)
        {
            var name = FenceEntry.FromPath(path)?.GetDisplayName(app.ShowExtensions) ?? Path.GetFileName(path);
            using var dialog = new InputDialog(Strings.ItemNoteTitle(name), Strings.ItemNotePrompt, NoteOf(path) ?? "");
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            var text = dialog.Value.Trim();
            Info.ItemNotes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (text.Length == 0)
                Info.ItemNotes.Remove(path);
            else
                Info.ItemNotes[path] = text;
            if (Info.ItemNotes.Count == 0)
                Info.ItemNotes = null;
            app.RequestSave();
            Invalidate();
        }

        /// <summary>A small note sheet in the item's bottom right corner of the icon.</summary>
        private void DrawItemNoteMark(Graphics g, string path, Rectangle item)
        {
            if (NoteOf(path) == null)
                return;
            var size = Px(12);
            var x = item.X + (item.Width + iconPx) / 2 - size + Px(2);
            var y = Info.Compact ? item.Bottom - size - Px(2) : item.Y + Px(4) + iconPx - size + Px(2);
            var r = new Rectangle(x, y, size, size);
            using (var paper = new SolidBrush(Color.FromArgb(255, 250, 225, 110)))
                g.FillRectangle(paper, r);
            using var pen = new Pen(Color.FromArgb(150, 90, 70, 20), Math.Max(1, scale));
            g.DrawRectangle(pen, r);
            for (var k = 1; k <= 3; k++)
                g.DrawLine(pen, r.X + Px(3), r.Y + k * size / 4, r.Right - Px(3), r.Y + k * size / 4);
        }

        /// <summary>Tooltip of the hovered item: its name in compact fences, and its note.</summary>
        private void UpdateItemTooltip(string? path)
        {
            if (path == null)
            {
                toolTip.SetToolTip(this, "");
                return;
            }
            var name = Info.Compact ? FenceEntry.FromPath(path)?.GetDisplayName(app.ShowExtensions) : null;
            var note = NoteOf(path);
            toolTip.SetToolTip(this, string.Join("\n", new[] { name, note }.Where(s => !string.IsNullOrEmpty(s))));
        }

        #endregion

        /// <summary>Files open with the fence's own program, if one is chosen; folders always in Explorer.</summary>
        private bool OpenWithFenceProgram(string path)
        {
            if (Info.OpenWith is not { Length: > 0 } program || !File.Exists(path))
                return false;
            try
            {
                Process.Start(new ProcessStartInfo(program) { ArgumentList = { path }, UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path) ?? "" });
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, Strings.OpenWithFailed(Path.GetFileName(program), e.Message), "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
        }
    }
}
