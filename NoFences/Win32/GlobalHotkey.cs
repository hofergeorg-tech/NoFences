namespace NoFences.Win32
{
    /// <summary>A system-wide keyboard shortcut, delivered to a hidden message window.</summary>
    internal sealed class GlobalHotkey : NativeWindow, IDisposable
    {
        private const int Id = 0x4E46; // "NF"

        public event EventHandler? Pressed;

        /// <summary>False if another program already owns the shortcut.</summary>
        public bool Registered { get; }

        public GlobalHotkey(uint modifiers, Keys key)
        {
            CreateHandle(new CreateParams());
            Registered = Native.RegisterHotKey(Handle, Id, modifiers | Native.MOD_NOREPEAT, (uint)key);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && m.WParam.ToInt32() == Id)
                Pressed?.Invoke(this, EventArgs.Empty);
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.UnregisterHotKey(Handle, Id);
                DestroyHandle();
            }
        }
    }
}
