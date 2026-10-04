using System.Runtime.InteropServices;
using NoFences.Model;

namespace NoFences.Win32
{
    /// <summary>
    /// Reserves a strip at a screen edge like the taskbar (Windows "application desktop toolbar"):
    /// maximized windows end next to it. An invisible window holds the reservation; it is released
    /// on Dispose, otherwise the space would stay blocked until Explorer restarts.
    /// </summary>
    internal sealed class AppBar : NativeWindow, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public Native.RECT rc;
            public IntPtr lParam;
        }

        [DllImport("shell32.dll")]
        private static extern UIntPtr SHAppBarMessage(uint message, ref APPBARDATA data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string name);

        private const uint ABM_NEW = 0, ABM_REMOVE = 1, ABM_QUERYPOS = 2, ABM_SETPOS = 3;
        private const int ABN_POSCHANGED = 1;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_EX_TOOLWINDOW = 0x80;

        private readonly uint callbackMessage = RegisterWindowMessage("NoFences.AppBar");
        private bool registered;
        private (Rectangle Monitor, DockEdge Edge, int Thickness)? current;

        /// <summary>Windows moved the reserved strips (another bar, the taskbar, a screen change).</summary>
        public event Action? PositionChanged;

        /// <summary>The reserved strip in screen coordinates (empty before <see cref="Reserve"/>).</summary>
        public Rectangle Bounds { get; private set; }

        public AppBar()
        {
            CreateHandle(new CreateParams { Caption = "NoFences bar", Style = WS_POPUP, ExStyle = WS_EX_TOOLWINDOW });
        }

        private APPBARDATA Data() => new()
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = Handle,
            uCallbackMessage = callbackMessage
        };

        /// <summary>Reserves <paramref name="thickness"/> pixels at the edge of the monitor; returns the strip it got.</summary>
        public Rectangle Reserve(Rectangle monitor, DockEdge edge, int thickness, bool force = false)
        {
            if (!force && current == (monitor, edge, thickness))
                return Bounds;
            var data = Data();
            if (!registered)
            {
                SHAppBarMessage(ABM_NEW, ref data);
                registered = true;
            }
            data.uEdge = edge switch { DockEdge.Left => 0u, DockEdge.Top => 1u, DockEdge.Right => 2u, _ => 3u };
            data.rc = new Native.RECT { Left = monitor.Left, Top = monitor.Top, Right = monitor.Right, Bottom = monitor.Bottom };
            Cut(ref data.rc, edge, thickness);
            // Windows moves the proposal past other bars (e.g. the taskbar on the same edge)
            SHAppBarMessage(ABM_QUERYPOS, ref data);
            Cut(ref data.rc, edge, thickness);
            SHAppBarMessage(ABM_SETPOS, ref data);
            current = (monitor, edge, thickness);
            Bounds = Rectangle.FromLTRB(data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);
            return Bounds;
        }

        /// <summary>Keeps the strip at the edge and makes it exactly <paramref name="thickness"/> thick.</summary>
        private static void Cut(ref Native.RECT rc, DockEdge edge, int thickness)
        {
            switch (edge)
            {
                case DockEdge.Left: rc.Right = rc.Left + thickness; break;
                case DockEdge.Right: rc.Left = rc.Right - thickness; break;
                case DockEdge.Top: rc.Bottom = rc.Top + thickness; break;
                default: rc.Top = rc.Bottom - thickness; break;
            }
        }

        public void Release()
        {
            if (!registered)
                return;
            var data = Data();
            SHAppBarMessage(ABM_REMOVE, ref data);
            registered = false;
            current = null;
            Bounds = Rectangle.Empty;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == callbackMessage && m.WParam.ToInt32() == ABN_POSCHANGED)
            {
                current = null; // negotiate again
                PositionChanged?.Invoke();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Release();
            DestroyHandle();
        }
    }
}
