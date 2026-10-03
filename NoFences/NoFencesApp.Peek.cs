using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// "Peek": a global shortcut lifts all fences above the open windows. It ends with the shortcut
    /// again, Esc, a click outside the fences, or when another program comes to the front
    /// (e.g. after opening an item).
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer peekTimer = new() { Interval = 50 };
        private GlobalHotkey? hotkey;
        private bool peeking;
        private IntPtr peekForeground;

        private void InitPeek()
        {
            peekTimer.Tick += (_, _) => WatchPeek();
            UpdateHotkey(notifyIfTaken: true);
        }

        private static (uint modifiers, Keys key) ParseHotkey(string hotkey) => hotkey switch
        {
            "Ctrl+Alt+D" => (Native.MOD_CONTROL | Native.MOD_ALT, Keys.D),
            "Ctrl+Alt+Space" => (Native.MOD_CONTROL | Native.MOD_ALT, Keys.Space),
            "Ctrl+Shift+D" => (Native.MOD_CONTROL | Native.MOD_SHIFT, Keys.D),
            "Ctrl+Alt+F" => (Native.MOD_CONTROL | Native.MOD_ALT, Keys.F),
            "Ctrl+Shift+F" => (Native.MOD_CONTROL | Native.MOD_SHIFT, Keys.F),
            "Ctrl+Alt+S" => (Native.MOD_CONTROL | Native.MOD_ALT, Keys.S),
            _ => (0, Keys.None)
        };

        internal void UpdateHotkey(bool notifyIfTaken)
        {
            hotkey?.Dispose();
            hotkey = null;

            var (modifiers, key) = ParseHotkey(Store.Config.PeekHotkey);
            if (key == Keys.None)
                return;

            var candidate = new GlobalHotkey(modifiers, key);
            if (!candidate.Registered)
            {
                candidate.Dispose();
                if (notifyIfTaken)
                    ShowBalloon(Strings.HotkeyTaken(Store.Config.PeekHotkey), timeout: 6000);
                return;
            }
            candidate.Pressed += (_, _) => TogglePeek();
            hotkey = candidate;
        }

        public void TogglePeek()
        {
            if (peeking)
                EndPeek();
            else
                StartPeek();
        }

        internal void StartPeek()
        {
            if (!fencesVisible)
                ToggleVisible();
            peeking = true;
            ApplyVisibility(); // fences hidden by a full-screen program come back for the peek
            foreach (var w in windows)
                w.SetPeek(true);
            peekForeground = Native.GetForegroundWindow();
            peekTimer.Start();
        }

        private void EndPeek()
        {
            peekTimer.Stop();
            peeking = false;
            foreach (var w in windows)
                w.SetPeek(false);
            ApplyVisibility();
        }

        private void WatchPeek()
        {
            if (Native.IsKeyDown(Native.VK_ESCAPE))
            {
                EndPeek();
                return;
            }

            // Another program came to the front (e.g. an item was opened).
            var foreground = Native.GetForegroundWindow();
            if (foreground != peekForeground && !Native.IsOwnWindow(foreground))
            {
                EndPeek();
                return;
            }

            // Click outside the fences and outside our own menus/dialogs.
            if (Native.IsKeyDown(Native.VK_LBUTTON) || Native.IsKeyDown(Native.VK_RBUTTON))
            {
                var cursor = Cursor.Position;
                var onFence = windows.Any(w => w.Visible && w.Bounds.Contains(cursor));
                if (!onFence && !Native.IsOwnWindow(Native.WindowFromPoint(cursor)))
                    EndPeek();
            }
        }

        private void AddPeekItems(ToolStripItemCollection items)
        {
            // The shortcut goes into the item's own shortcut column (right-aligned, with spacing).
            var peek = new ToolStripMenuItem(Strings.PeekMenu, null, (_, _) => StartPeek());
            if (Store.Config.PeekHotkey != "Off")
                peek.ShortcutKeyDisplayString = Strings.HotkeyName(Store.Config.PeekHotkey);
            items.Add(peek);


        }

        private void DisposePeek()
        {
            EndPeek();
            peekTimer.Dispose();
            hotkey?.Dispose();
        }
    }
}
