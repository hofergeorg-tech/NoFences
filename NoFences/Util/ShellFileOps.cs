using System.Runtime.InteropServices;

namespace NoFences.Util
{
    /// <summary>
    /// Moves/copies through the shell, so the user gets Explorer's progress, conflict and undo handling.
    /// </summary>
    public static class ShellFileOps
    {
        private const uint FO_MOVE = 0x0001;
        private const uint FO_COPY = 0x0002;
        private const ushort FOF_ALLOWUNDO = 0x0040;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string? pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string? lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

        public static bool Move(IWin32Window? owner, IEnumerable<string> sources, string targetDir) => Run(owner, FO_MOVE, sources, targetDir);

        public static bool Copy(IWin32Window? owner, IEnumerable<string> sources, string targetDir) => Run(owner, FO_COPY, sources, targetDir);

        /// <summary>
        /// Runs a shell operation on its own STA thread and reports the result on the UI thread.
        /// Copying big files in a drop handler would freeze the fences – and the Explorer window the
        /// files came from, which waits until the drop returns. No owner window: a window of the UI
        /// thread as owner would tie both threads' input together again.
        /// </summary>
        public static void InBackground(Func<bool> operation, Action<bool> done)
        {
            var ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            var thread = new Thread(() =>
            {
                bool ok;
                try { ok = operation(); }
                catch (Exception) { ok = false; }
                ui.Post(_ => done(ok), null);
            }) { Name = "Shell file operation" }; // not a background thread: exiting waits for a running move
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>Moves files to the recycle bin (Explorer asks for confirmation as usual).</summary>
        public static bool Recycle(IWin32Window owner, IEnumerable<string> paths) => Run(owner, FO_DELETE, paths, null);

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_SILENT = 0x0004, FOF_NOCONFIRMATION = 0x0010, FOF_NOERRORUI = 0x0400;

        /// <summary>Recycle bin without any questions or progress (the shelf's clean-up).</summary>
        public static bool RecycleSilently(IEnumerable<string> paths) =>
            Run(null, FO_DELETE, paths, null, FOF_ALLOWUNDO | FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI);

        private static bool Run(IWin32Window? owner, uint func, IEnumerable<string> sources, string? targetDir, ushort flags = FOF_ALLOWUNDO)
        {
            var from = string.Join("\0", sources) + "\0\0";
            var op = new SHFILEOPSTRUCT
            {
                hwnd = owner?.Handle ?? IntPtr.Zero,
                wFunc = func,
                pFrom = from,
                pTo = targetDir == null ? null : targetDir + "\0\0",
                fFlags = flags
            };
            return SHFileOperation(ref op) == 0 && !op.fAnyOperationsAborted;
        }
    }
}
