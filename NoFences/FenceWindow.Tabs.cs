using System.Drawing.Drawing2D;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Tabs in a links fence: several groups in one box, shown as chips in the title bar.
    /// Click switches, double-click renames, "+" adds, items dropped on a chip move to that tab.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private readonly List<(RectangleF Rect, int Index)> tabChips = new(); // index -1 = "+"
        private int dropTab = -1;

        private bool HasTabs => Info.Kind == FenceKind.Links && Info.Tabs.Count > 0;

        private int TabAt(Point p)
        {
            foreach (var (rect, index) in tabChips)
            {
                if (rect.Contains(p))
                    return index;
            }
            return -2; // none
        }

        private void DrawTabs(Graphics g)
        {
            tabChips.Clear();
            if (!HasTabs || labelFont == null)
                return;
            var ins = theme.SurfaceInsets;
            var top = Px(ins.Top);
            var height = Math.Max(Px(18), titleHeight - top - Px(10));
            var y = top + (titleHeight - top - height) / 2f;
            float x = Px(ins.Left) + Px(10);
            var maxX = ClientSize.Width - Px(ins.Right) - Px(10);

            for (var i = 0; i <= Info.Tabs.Count; i++)
            {
                var plus = i == Info.Tabs.Count;
                var text = plus ? "+" : Info.Tabs[i].Name;
                var width = g.MeasureString(text, labelFont).Width + Px(plus ? 10 : 16);
                if (x + width > maxX)
                    break;
                var rect = new RectangleF(x, y, width, height);
                var active = !plus && i == Info.ActiveTab;
                var alpha = active ? 150 : i == dropTab ? 110 : 45;
                using (var path = RoundedRectPath(rect, height / 2.5f))
                using (var brush = new SolidBrush(Color.FromArgb(alpha, theme.Accent)))
                    g.FillPath(brush, path);
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                    theme.DrawLabel(g, text, rect, labelFont, format, scale);
                tabChips.Add((rect, plus ? -1 : i));
                x += width + Px(6);
            }
        }

        private static GraphicsPath RoundedRectPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Saves the current links into the active tab and loads another tab's links.</summary>
        private void SwitchTab(int index)
        {
            if (!HasTabs || index < 0 || index >= Info.Tabs.Count || index == Info.ActiveTab)
                return;
            Info.Tabs[Info.ActiveTab].Files = Info.Files.ToList();
            Info.ActiveTab = index;
            Info.Files = Info.Tabs[index].Files.ToList();
            selection.Clear();
            scrollOffset = 0;
            app.RequestSave();
            ReloadEntries();
        }

        /// <summary>Adds a tab; the first call turns the current links into tab 1.</summary>
        private void AddTab()
        {
            if (Info.Kind != FenceKind.Links)
                return;
            if (Info.Tabs.Count == 0)
                Info.Tabs.Add(new FenceTab { Name = Info.Name, Files = Info.Files.ToList() });
            else
                Info.Tabs[Info.ActiveTab].Files = Info.Files.ToList();
            Info.Tabs.Add(new FenceTab { Name = Strings.TabDefaultName(Info.Tabs.Count + 1) });
            Info.ActiveTab = Info.Tabs.Count - 1;
            Info.Files = new List<string>();
            app.RequestSave();
            ReloadEntries();
            RenameTab(Info.ActiveTab);
        }

        private void RenameTab(int index)
        {
            if (index < 0 || index >= Info.Tabs.Count)
                return;
            using var dialog = new InputDialog(Strings.RenameTab, Strings.NewName, Info.Tabs[index].Name);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Value.Trim().Length == 0)
                return;
            Info.Tabs[index].Name = dialog.Value.Trim();
            app.RequestSave();
            Invalidate();
        }

        /// <summary>Removes a tab; its links move to the neighboring tab so nothing is lost. The last tab ends tab mode.</summary>
        private void RemoveTab(int index)
        {
            if (index < 0 || index >= Info.Tabs.Count)
                return;
            app.RecordUndo(Strings.UndoTabs(Info.Name), new[] { Info.Id });
            Info.Tabs[Info.ActiveTab].Files = Info.Files.ToList();
            var removed = Info.Tabs[index];
            Info.Tabs.RemoveAt(index);
            if (Info.Tabs.Count <= 1)
            {
                // Back to a plain fence with everything in it
                Info.Files = Info.Tabs.SelectMany(t => t.Files).Concat(removed.Files).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                Info.Tabs.Clear();
                Info.ActiveTab = 0;
            }
            else
            {
                var target = Math.Min(index, Info.Tabs.Count - 1);
                Info.Tabs[target].Files = Info.Tabs[target].Files.Concat(removed.Files).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                Info.ActiveTab = Math.Clamp(Info.ActiveTab > index ? Info.ActiveTab - 1 : Info.ActiveTab, 0, Info.Tabs.Count - 1);
                Info.Files = Info.Tabs[Info.ActiveTab].Files.ToList();
            }
            app.RequestSave();
            ReloadEntries();
        }

        /// <summary>Moves links onto another tab (items dropped on its chip).</summary>
        private void MoveToTab(IEnumerable<string> paths, int tab, bool removeFromCurrent)
        {
            var list = paths.ToList();
            if (tab == Info.ActiveTab || tab < 0 || tab >= Info.Tabs.Count)
                return;
            var target = Info.Tabs[tab].Files;
            foreach (var p in list.Where(p => !target.Contains(p, StringComparer.OrdinalIgnoreCase)))
                target.Add(p);
            if (removeFromCurrent)
                Info.Files.RemoveAll(f => list.Contains(f, StringComparer.OrdinalIgnoreCase));
            app.RequestSave();
            ReloadEntries();
        }

        /// <summary>Clicks on chips (they are in the title bar, which otherwise drags the fence).</summary>
        private bool TabMouseUp(MouseEventArgs e)
        {
            var tab = TabAt(e.Location);
            if (tab == -2)
                return false;
            if (e.Button == MouseButtons.Left)
            {
                if (tab == -1)
                    AddTab();
                else
                    SwitchTab(tab);
            }
            else if (e.Button == MouseButtons.Right && tab >= 0)
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add(Strings.RenameTab, null, (_, _) => RenameTab(tab));
                menu.Items.Add(Strings.RemoveTab, null, (_, _) => RemoveTab(tab));
                menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
                menu.Show(this, e.Location);
            }
            return true;
        }
    }
}
