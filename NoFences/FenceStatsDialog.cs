using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// How often each item of a fence was opened (NoFences counts since 2.5). Items never opened are
    /// suggested for tidying up: removed from a links fence (Ctrl+Z brings them back) or, in a folder
    /// fence, moved to the recycle bin.
    /// </summary>
    internal sealed class FenceStatsDialog : Form
    {
        private readonly ListView list = new()
        {
            View = View.Details, CheckBoxes = true, FullRowSelect = true, Width = 460, Height = 320, HeaderStyle = ColumnHeaderStyle.Nonclickable
        };

        public static void Show(FenceWindow window, FenceInfo info, IFenceHost host)
        {
            using var dialog = new FenceStatsDialog(window, info, host);
            dialog.ShowDialog(window);
        }

        /// <summary>The fence's items with their open counts, least used first.</summary>
        public static List<(string Path, int Count)> Usage(FenceInfo info, IEnumerable<string> paths) =>
            paths.Select(p => (p, info.OpenCounts?.GetValueOrDefault(p) ?? 0))
                .OrderBy(x => x.Item2)
                .ThenBy(x => Path.GetFileName(x.p.TrimEnd('\\')), StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        private FenceStatsDialog(FenceWindow window, FenceInfo info, IFenceHost host)
        {
            Text = Strings.FenceStatsTitle(info.Name);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            list.Columns.Add(Strings.FenceStatsName, 340);
            list.Columns.Add(Strings.FenceStatsOpened, 100, HorizontalAlignment.Right);
            var usage = Usage(info, info.Files.Where(p => File.Exists(p) || Directory.Exists(p)));
            foreach (var (path, count) in usage)
            {
                var name = FenceEntry.FromPath(path)?.GetDisplayName(true) ?? path;
                list.Items.Add(new ListViewItem(new[] { name, count == 0 ? Strings.FenceStatsNever : count.ToString() }) { Tag = path, Checked = count == 0 });
            }
            var never = usage.Count(u => u.Count == 0);

            var isFolder = info.Kind == FenceKind.Folder;
            var remove = new Button { Text = isFolder ? Strings.FenceStatsRecycle : Strings.FenceStatsRemove, AutoSize = true, Enabled = !info.ReadOnly && !info.Locked };
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;
            remove.Click += (_, _) =>
            {
                var paths = list.CheckedItems.Cast<ListViewItem>().Select(i => (string)i.Tag!).ToList();
                if (paths.Count == 0)
                    return;
                if (isFolder)
                {
                    ShellFileOps.Recycle(this, paths);
                }
                else
                {
                    host.RecordUndo(Strings.UndoRemoveItems(paths.Count, info.Name), new[] { info.Id });
                    info.Files.RemoveAll(f => paths.Contains(f, StringComparer.OrdinalIgnoreCase));
                    host.RequestSave();
                }
                window.ReloadEntries();
                Close();
            };

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            layout.Controls.Add(new Label { Text = Strings.FenceStatsSummary(usage.Count, never), AutoSize = true, MaximumSize = new Size(460, 0) });
            layout.Controls.Add(list);
            layout.Controls.Add(new Label { Text = Strings.FenceStatsHint, AutoSize = true, MaximumSize = new Size(460, 0), ForeColor = SystemColors.GrayText });
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            buttons.Controls.AddRange(new Control[] { remove, close });
            layout.Controls.Add(buttons);
            Controls.Add(layout);
        }
    }
}
