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

        public static bool Move(IWin32Window owner, IEnumerable<string> sources, string targetDir) => Run(owner, FO_MOVE, sources, targetDir);

        public static bool Copy(IWin32Window owner, IEnumerable<string> sources, string targetDir) => Run(owner, FO_COPY, sources, targetDir);

        private static bool Run(IWin32Window owner, uint func, IEnumerable<string> sources, string targetDir)
        {
            var from = string.Join("\0", sources) + "\0\0";
            var op = new SHFILEOPSTRUCT
            {
                hwnd = owner.Handle,
                wFunc = func,
                pFrom = from,
                pTo = targetDir + "\0\0",
                fFlags = FOF_ALLOWUNDO
            };
            return SHFileOperation(ref op) == 0 && !op.fAnyOperationsAborted;
        }
    }
}
