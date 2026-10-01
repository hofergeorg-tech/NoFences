using System.Runtime.InteropServices;

namespace NoFences.Win32
{
    internal static class Native
    {
        public const int WM_SETFOCUS = 0x0007;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_NCCALCSIZE = 0x0083;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_MAXIMIZE_CAPTION = 0xF032;
        public const int MA_NOACTIVATE = 3;

        public const int HTCLIENT = 1, HTCAPTION = 2, HTLEFT = 10, HTRIGHT = 11, HTTOP = 12,
            HTTOPLEFT = 13, HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        public const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
        public static readonly IntPtr HWND_BOTTOM = new(1);
        public static readonly IntPtr HWND_TOPMOST = new(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new(-2);

        public const int WM_HOTKEY = 0x0312;
        public const int WM_SIZING = 0x0214;
        public const int WM_NCLBUTTONDBLCLK = 0x00A3;
        public const int WM_ENTERSIZEMOVE = 0x0231;
        public const int WM_EXITSIZEMOVE = 0x0232;
        public const int WM_MOVING = 0x0216;
        public const int WMSZ_LEFT = 1, WMSZ_RIGHT = 2, WMSZ_TOP = 3, WMSZ_TOPLEFT = 4, WMSZ_TOPRIGHT = 5,
            WMSZ_BOTTOM = 6, WMSZ_BOTTOMLEFT = 7, WMSZ_BOTTOMRIGHT = 8;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
        public const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_WIN = 0x8, MOD_NOREPEAT = 0x4000;
        public const int VK_LBUTTON = 0x01, VK_RBUTTON = 0x02, VK_ESCAPE = 0x1B;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>Works while handling the user's own click (Windows only allows it then).</summary>
        public static void SetForegroundWindowSafe(IntPtr hWnd) => SetForegroundWindow(hWnd);

        /// <summary>Whether a window belongs to this process (our menus, dialogs, fences).</summary>
        public static bool IsOwnWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;
            GetWindowThreadProcessId(hwnd, out var pid);
            return pid == Environment.ProcessId;
        }

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vk);

        public static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        public const int GWL_EXSTYLE = -20;
        public const int GWLP_HWNDPARENT = -8;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string? className, string? windowName);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        // Undocumented, but the only way to get the shell context menu to follow dark mode.
        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        public static extern int SetPreferredAppMode(int mode);

        #region Blur / DWM

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_ENABLE_BLURBEHIND = 3;
        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        private const int ACCENT_ENABLE_TRANSPARENTGRADIENT = 2;

        public static void EnableBlur(IntPtr hwnd) => SetAccent(hwnd, ACCENT_ENABLE_BLURBEHIND);

        /// <summary>Per-pixel transparency without the frosted-glass blur (fully clear where nothing is painted).</summary>
        /// <remarks>Flag 2 makes DWM use our (fully transparent) gradient color; without it Windows fills in a tint.</remarks>
        public static void EnableClearBackground(IntPtr hwnd) => SetAccent(hwnd, ACCENT_ENABLE_TRANSPARENTGRADIENT, flags: 2);

        /// <summary>Turns the DWM drop shadow around the window on or off.</summary>
        public static void SetWindowShadow(IntPtr hwnd, bool enabled)
        {
            var policy = enabled ? 2 : 1; // DWMNCRP_ENABLED : DWMNCRP_DISABLED
            DwmSetWindowAttribute(hwnd, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));
        }

        private static void SetAccent(IntPtr hwnd, int state, int flags = 0)
        {
            // GradientColor 0 = fully transparent, so only what we paint is visible.
            var accent = new AccentPolicy { AccentState = state, AccentFlags = flags, GradientColor = 0 };
            var size = Marshal.SizeOf(accent);
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, ptr, false);
                var data = new WindowCompositionAttributeData { Attribute = WCA_ACCENT_POLICY, Data = ptr, SizeOfData = size };
                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>Lets DWM draw a shadow around the borderless window.</summary>
        public static void EnableShadow(IntPtr hwnd)
        {
            var policy = 2; // DWMNCRP_ENABLED
            DwmSetWindowAttribute(hwnd, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));
            var margins = new MARGINS { Top = 1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }

        /// <summary>Windows 11 rounded corners: 1 = square, 2 = round, 3 = small round.</summary>
        public static void SetCornerPreference(IntPtr hwnd, int preference)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        #endregion

        public static void HideFromAltTab(IntPtr hwnd)
        {
            var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_TOOLWINDOW));
        }

        /// <summary>
        /// Makes the desktop (Progman) the owner window, so the fence survives Win+D / "show desktop".
        /// </summary>
        public static void GlueToDesktop(IntPtr hwnd)
        {
            var progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
                SetWindowLongPtr(hwnd, GWLP_HWNDPARENT, progman);
        }

        public static void SendToBottom(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }
    }
}
