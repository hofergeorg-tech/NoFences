using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Fence groups: fences with the same group name move together and fold together.</summary>
    public sealed partial class NoFencesApp
    {
        private IEnumerable<string> GroupNames =>
            Store.Config.Fences.Select(f => f.Group).OfType<string>().Distinct(StringComparer.CurrentCultureIgnoreCase).Order(StringComparer.CurrentCultureIgnoreCase);

        private List<FenceWindow> WindowsInGroup(string group) =>
            windows.Where(w => string.Equals(w.Info.Group, group, StringComparison.CurrentCultureIgnoreCase)).ToList();

        public IReadOnlyList<FenceWindow> GroupMembers(FenceWindow window) =>
            window.Info.Group is { } group ? WindowsInGroup(group).Where(w => w != window).ToList() : Array.Empty<FenceWindow>();

        public void AddGroupItems(ToolStripItemCollection items, FenceWindow window)
        {
            var info = window.Info;
            var menu = new ToolStripMenuItem(info.Group is { } current ? $"{Strings.GroupMenu}: {current}" : Strings.GroupMenu) { ToolTipText = Strings.GroupHint };
            foreach (var name in GroupNames)
            {
                var group = name;
                menu.DropDownItems.Add(new ToolStripMenuItem(group, null, (_, _) => SetGroup(window, group))
                {
                    Checked = string.Equals(info.Group, group, StringComparison.CurrentCultureIgnoreCase)
                });
            }
            menu.DropDownItems.Add(Strings.NewGroup, null, (_, _) =>
            {
                using var dialog = new InputDialog(Strings.NewGroup.TrimEnd('…'), Strings.GroupPrompt, "");
                if (dialog.ShowDialog(window) == DialogResult.OK && dialog.Value.Trim().Length > 0)
                    SetGroup(window, dialog.Value.Trim());
            });
            if (info.Group != null)
            {
                menu.DropDownItems.Add(Strings.LeaveGroup, null, (_, _) => SetGroup(window, null));
                menu.DropDownItems.Add(new ToolStripSeparator());
                var group = info.Group;
                var folded = WindowsInGroup(group).All(w => w.Info.Folded);
                menu.DropDownItems.Add(folded ? Strings.UnfoldGroup : Strings.FoldGroup, null, (_, _) => FoldGroup(group, !folded));
            }
            items.Add(menu);
        }

        private void SetGroup(FenceWindow window, string? group)
        {
            RecordUndo(Strings.UndoGroup(window.Info.Name), new[] { window.Info.Id });
            window.Info.Group = group;
            if (group == null && window.Info.Folded)
                window.SetFolded(false);
            else if (group != null)
                window.SetFolded(WindowsInGroup(group).Where(w => w != window).Any(w => w.Info.Folded));
            Store.RequestSave();
        }

        internal void FoldGroup(string group, bool fold)
        {
            foreach (var w in WindowsInGroup(group))
                w.SetFolded(fold);
            Store.RequestSave();
        }
    }
}
