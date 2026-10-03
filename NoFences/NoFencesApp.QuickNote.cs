using NoFences.Model;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>Ctrl+Alt+N: a new note appears at the mouse, ready to type into.</summary>
    public sealed partial class NoFencesApp
    {
        private GlobalHotkey? quickNoteHotkey;

        internal void UpdateQuickNoteHotkey()
        {
            quickNoteHotkey?.Dispose();
            quickNoteHotkey = null;
            var (modifiers, key) = ParseHotkey(Store.Config.QuickNoteHotkey);
            if (key == Keys.None)
                return;
            var candidate = new GlobalHotkey(modifiers, key);
            if (!candidate.Registered)
            {
                candidate.Dispose();
                return;
            }
            candidate.Pressed += (_, _) => QuickNote();
            quickNoteHotkey = candidate;
        }

        public void QuickNote()
        {
            if (!fencesVisible)
                ToggleVisible();
            CreateFence(FenceKind.Note);
            var window = windows.LastOrDefault();
            if (window == null)
                return;
            // Above the open windows while typing; it goes back to the desktop when done
            Native.SetForegroundWindowSafe(window.Handle);
            window.StartEditNote();
        }

        private void DisposeQuickNote() => quickNoteHotkey?.Dispose();
    }
}
