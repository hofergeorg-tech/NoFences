using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// The background of a docked bar in a chosen style (e.g. a wooden table under the fences). The bar's
    /// fences are owned by this window while docked, so Windows always keeps them above it.
    /// </summary>
    internal sealed class DockBackdrop : Form
    {
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        private FenceTheme theme;
        private bool vertical;
        private bool raised;
        private readonly FenceInfo decoration;

        /// <summary>Right-click on the bar's background (for the bar menu).</summary>
        public event Action<Point>? MenuRequested;

        public DockBackdrop(string group, FenceTheme theme, bool vertical)
        {
            this.theme = theme;
            this.vertical = vertical;
            // Stable decorations per bar: the same "random" layout after every restart
            var id = new Guid(MD5.HashData(Encoding.UTF8.GetBytes(group)));
            decoration = new FenceInfo { Id = id, BackgroundAlpha = 225, BackgroundColor = 0x1E1E24 };
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            DoubleBuffered = true;
            Text = "NoFences bar";
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.HideFromAltTab(Handle);
            Native.GlueToDesktop(Handle);
            Native.SetCornerPreference(Handle, 1);
            Native.SetWindowShadow(Handle, false);
            ApplyGlass();
        }

        private void ApplyGlass()
        {
            if (!IsHandleCreated)
                return;
            if (theme.Glass)
                Native.EnableBlur(Handle);
            else
                Native.EnableClearBackground(Handle);
        }

        public void SetLook(FenceTheme newTheme, bool newVertical)
        {
            if (newTheme == theme && newVertical == vertical)
                return;
            theme = newTheme;
            vertical = newVertical;
            ApplyGlass();
            Invalidate();
        }

        /// <summary>Above other windows together with the bar's fences, or down on the desktop.</summary>
        public void SetRaised(bool on)
        {
            if (raised == on || !IsHandleCreated)
                return;
            raised = on;
            const uint flags = Native.SWP_NOSIZE | Native.SWP_NOMOVE | Native.SWP_NOACTIVATE;
            if (on)
            {
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0, flags);
            }
            else
            {
                Native.SetWindowPos(Handle, Native.HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                Native.SendToBottom(Handle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            try
            {
                theme.DrawBar(g, ClientRectangle, vertical, decoration, DeviceDpi / 96f);
            }
            catch (Exception ex)
            {
                Util.Log.Write("Bar", ex.Message);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
                MenuRequested?.Invoke(PointToScreen(e.Location));
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_MOUSEACTIVATE:
                    m.Result = new IntPtr(MA_NOACTIVATE);
                    return;
                case WM_WINDOWPOSCHANGING when !raised:
                    // Stay down on the desktop like the fences
                    var pos = Marshal.PtrToStructure<Native.WINDOWPOS>(m.LParam);
                    if ((pos.flags & Native.SWP_NOZORDER) == 0 && pos.hwndInsertAfter != Native.HWND_BOTTOM)
                    {
                        pos.hwndInsertAfter = Native.HWND_BOTTOM;
                        Marshal.StructureToPtr(pos, m.LParam, false);
                    }
                    break;
            }
            base.WndProc(ref m);
        }
    }
}
