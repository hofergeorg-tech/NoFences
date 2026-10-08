using System.Runtime.InteropServices;
using System.Text;

namespace NoFences.Win32
{
    /// <summary>
    /// Raises <see cref="DoubleClicked"/> when the user double-clicks empty space on the desktop
    /// (not an icon). Uses a low-level mouse hook, since the desktop belongs to Explorer.
    /// Windows calls a low-level hook for every mouse event of every program, in the thread that
    /// installed it – so it lives on its own thread: a busy UI thread must not make the mouse stutter
    /// system-wide (and Windows silently removes hooks that time out too often).
    /// </summary>
    internal sealed class DesktopDoubleClickHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETNEXTITEM = LVM_FIRST + 12;
        private const int LVM_GETHOTITEM = LVM_FIRST + 61;
        private const int LVNI_SELECTED = 0x0002;
        private const int SMTO_ABORTIFHUNG = 0x0002;
        private const int SM_CXDOUBLECLK = 36, SM_CYDOUBLECLK = 37;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc fn, IntPtr hMod, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? name);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, int flags, int timeout, out IntPtr result);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam, lParam;
            public uint time;
            public POINT pt;
            public uint lPrivate;
        }

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG msg, IntPtr hwnd, uint filterMin, uint filterMax);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private const uint WM_QUIT = 0x0012;

        private readonly LowLevelMouseProc proc; // must stay referenced while the hook is installed
        private readonly SynchronizationContext ui;
        private readonly Thread thread;
        private uint threadId;
        private IntPtr hook;

        // Only touched on the hook thread
        private uint lastTime;
        private POINT lastPoint;

        public event EventHandler? DoubleClicked;

        public DesktopDoubleClickHook()
        {
            ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            proc = HookProc;
            using var ready = new ManualResetEventSlim();
            thread = new Thread(() =>
            {
                threadId = GetCurrentThreadId();
                hook = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(null), 0);
                ready.Set();
                // The hook is called while this thread waits for messages; WM_QUIT ends it.
                while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
                {
                }
                if (hook != IntPtr.Zero)
                    UnhookWindowsHookEx(hook);
                hook = IntPtr.Zero;
            }) { IsBackground = true, Name = "Desktop mouse hook" };
            thread.Start();
            ready.Wait();
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == WM_LBUTTONDOWN)
            {
                // Keep this fast: Windows drops hooks that take too long.
                var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var desktop = FindDesktopTarget(WindowFromPoint(info.pt), out var listView);

                if (desktop && lastTime != 0 && IsDoubleClick(info))
                {
                    lastTime = 0; // a triple click must not toggle twice
                    ui.Post(_ => CheckEmptySpace(listView), null);
                }
                else
                {
                    lastTime = desktop ? info.time : 0;
                    lastPoint = info.pt;
                }
            }
            return CallNextHookEx(hook, nCode, wParam, lParam);
        }

        private bool IsDoubleClick(MSLLHOOKSTRUCT info) =>
            info.time - lastTime <= GetDoubleClickTime()
            && Math.Abs(info.pt.X - lastPoint.X) <= GetSystemMetrics(SM_CXDOUBLECLK) / 2
            && Math.Abs(info.pt.Y - lastPoint.Y) <= GetSystemMetrics(SM_CYDOUBLECLK) / 2;

        private void CheckEmptySpace(IntPtr listView)
        {
            // The first click has already been handled by Explorer: on empty space it cleared the
            // selection, on an icon it selected it (and the double click will open it).
            if (listView != IntPtr.Zero)
            {
                if (SendQuery(listView, LVM_GETHOTITEM, IntPtr.Zero) >= 0)
                    return;
                if (SendQuery(listView, LVM_GETNEXTITEM, new IntPtr(-1), new IntPtr(LVNI_SELECTED)) >= 0)
                    return;
            }
            DoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        private static long SendQuery(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam = default)
        {
            if (SendMessageTimeout(hwnd, msg, wParam, lParam, SMTO_ABORTIFHUNG, 100, out var result) == IntPtr.Zero)
                return 0; // Explorer hangs: treat as "item hit", do nothing
            return (int)result.ToInt64();
        }

        /// <summary>
        /// True for the desktop icon view (SysListView32 inside SHELLDLL_DefView) or the bare
        /// desktop background (Progman/WorkerW, e.g. when desktop icons are hidden).
        /// </summary>
        private static bool FindDesktopTarget(IntPtr hwnd, out IntPtr listView)
        {
            listView = IntPtr.Zero;
            var cls = ClassOf(hwnd);
            if (cls == "SysListView32" && ClassOf(GetParent(hwnd)) == "SHELLDLL_DefView")
            {
                var host = ClassOf(GetParent(GetParent(hwnd)));
                if (host is "Progman" or "WorkerW")
                {
                    listView = hwnd;
                    return true;
                }
                return false;
            }
            // Explorer folder windows also contain a SHELLDLL_DefView, so check where it lives.
            if (cls == "SHELLDLL_DefView")
                return ClassOf(GetParent(hwnd)) is "Progman" or "WorkerW";
            return cls is "Progman" or "WorkerW";
        }

        private static string ClassOf(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return "";
            var sb = new StringBuilder(64);
            GetClassName(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public void Dispose()
        {
            if (!thread.IsAlive)
                return;
            PostThreadMessage(threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            thread.Join(1000);
        }
    }
}
