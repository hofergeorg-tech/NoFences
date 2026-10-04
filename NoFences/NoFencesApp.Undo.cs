using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Ctrl+Z: undo deleting, moving and renaming fences and their items.</summary>
    public sealed partial class NoFencesApp
    {
        private readonly UndoStack undo = new();

        public string? UndoDescription => undo.NextDescription;

        public void RecordUndo(string description, IEnumerable<Guid> fences, Action? reverse = null)
        {
            var ids = fences.ToHashSet();
            undo.Record(description, Store.Config.Fences.Where(f => ids.Contains(f.Id)), reverse);
        }

        public void RecordUndo(string description, IReadOnlyList<string> snapshots) => undo.Record(description, snapshots);

        public void Undo()
        {
            var step = undo.Pop();
            if (step == null)
            {
                ShowBalloon(Strings.NothingToUndo);
                return;
            }
            string? error = null;
            try
            {
                step.Reverse?.Invoke();
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            foreach (var (info, added) in UndoStack.Restore(step, Store.Config.Fences))
            {
                if (added)
                    OpenWindow(info);
                else
                    windows.FirstOrDefault(w => w.Info == info)?.RefreshFromInfo();
            }
            UpdateFenceHotkeys();
            Store.RequestSave();
            ShowBalloon(error == null ? Strings.Undone(step.Description) : Strings.UndoFailed(error));
        }

        /// <summary>"Undo: …" in the tray and fence menus (only when there is something to undo).</summary>
        public void AddUndoItem(ToolStripItemCollection items)
        {
            if (UndoDescription is not { } what)
                return;
            items.Add(new ToolStripMenuItem(Strings.UndoMenu(what), null, (_, _) => Undo()) { ShortcutKeyDisplayString = Strings.HotkeyName("Ctrl+Z") });
        }
    }
}
