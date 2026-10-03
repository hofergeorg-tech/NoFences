using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// A screen ruler that floats above everything: drag to move, drag an end to resize, space turns it,
    /// arrow keys nudge it (Shift = 10). Shows the mouse's distance from the zero edge in pixels,
    /// centimetres or inches (real size, from the monitor's reported dimensions).
    /// </summary>
    public sealed class RulerWindow : Form
    {
        public enum Unit { Pixels, Centimeters, Inches }

        private static RulerWindow? open;
        private readonly System.Windows.Forms.Timer follow = new() { Interval = 30 };
        private bool vertical;
        private Unit unit = Unit.Pixels;
        private Point? dragStart;
        private Rectangle dragBounds;
        private int resizeEdge; // 0 = move, 1 = far end
        private int cursorOffset = -1;
        private bool preview;

        private const int Thickness = 64;

        public static void Toggle()
        {
            if (open is { IsDisposed: false })
            {
                open.Close();
                return;
            }
            open = new RulerWindow();
            open.Show();
            open.Activate();
        }

        internal static RulerWindow CreateForPreview()
        {
            // The preview shows a fixed mouse position and never follows the real mouse
            return new RulerWindow { cursorOffset = 312, preview = true };
        }

        private RulerWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            Opacity = 0.9;
            StartPosition = FormStartPosition.Manual;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Bounds = new Rectangle(area.X + (area.Width - 800) / 2, area.Y + area.Height / 3, 800, Thickness);
            Text = Strings.RulerTitle;
            follow.Tick += (_, _) => TrackCursor();
            Shown += (_, _) =>
            {
                if (!preview)
                    follow.Start();
            };
            ContextMenuStrip = BuildMenu();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW: not in Alt+Tab
                return cp;
            }
        }

        private int Length => vertical ? Height : Width;

        /// <summary>Pixels per unit on the monitor the ruler is on.</summary>
        private double PixelsPerUnit
        {
            get
            {
                if (unit == Unit.Pixels)
                    return 1;
                var mm = MonitorMillimetres(Screen.FromControl(this), vertical);
                var px = vertical ? Screen.FromControl(this).Bounds.Height : Screen.FromControl(this).Bounds.Width;
                var perMm = mm > 0 ? px / mm : DeviceDpi / 25.4;
                return unit == Unit.Centimeters ? perMm * 10 : perMm * 25.4;
            }
        }

        private string UnitLabel => unit switch { Unit.Centimeters => "cm", Unit.Inches => "in", _ => "px" };

        private string FormatLength(int pixels) => unit == Unit.Pixels ? $"{pixels} px" : $"{pixels / PixelsPerUnit:0.00} {UnitLabel}";

        private void TrackCursor()
        {
            // The timer can still tick once while the window is going away
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;
            var p = PointToClient(Cursor.Position);
            var offset = vertical ? p.Y : p.X;
            var across = vertical ? p.X : p.Y;
            // Follow the mouse along the ruler, also a bit beside it
            var value = offset >= 0 && offset <= Length && across > -400 && across < Thickness + 400 ? offset : -1;
            if (value != cursorOffset)
            {
                cursorOffset = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            using (var back = new LinearGradientBrush(ClientRectangle, Color.FromArgb(255, 236, 140), Color.FromArgb(250, 214, 90), vertical ? 0f : 90f))
                g.FillRectangle(back, ClientRectangle);
            using var tick = new Pen(Color.FromArgb(60, 45, 10));
            using var font = new Font("Segoe UI", 9f);
            using var brush = new SolidBrush(Color.FromArgb(60, 45, 10));

            // Tick spacing: every 2/10/50 px, or 1 mm / 5 mm / 1 cm, or 1/16 / 1/4 / 1 inch
            double minor, medium, major;
            switch (unit)
            {
                case Unit.Centimeters: major = PixelsPerUnit; medium = major / 2; minor = major / 10; break;
                case Unit.Inches: major = PixelsPerUnit; medium = major / 4; minor = major / 16; break;
                default: major = 50; medium = 10; minor = 2; break;
            }
            var count = (int)(Length / minor) + 1;
            for (var i = 0; i <= count; i++)
            {
                var pos = (float)(i * minor);
                if (pos > Length)
                    break;
                var isMajor = Math.Abs(pos / major - Math.Round(pos / major)) < 1e-6;
                var isMedium = !isMajor && Math.Abs(pos / medium - Math.Round(pos / medium)) < 1e-6;
                var len = isMajor ? 22 : isMedium ? 13 : 6;
                if (vertical)
                    g.DrawLine(tick, 0, pos, len, pos);
                else
                    g.DrawLine(tick, pos, 0, pos, len);
                if (isMajor && i > 0)
                {
                    var label = unit == Unit.Pixels ? ((int)pos).ToString() : Math.Round(pos / major).ToString();
                    if (vertical)
                        g.DrawString(label, font, brush, 24, pos - 8);
                    else
                        g.DrawString(label, font, brush, pos + 2, 22);
                }
            }

            // Mouse position line and distance
            if (cursorOffset >= 0)
            {
                using var red = new Pen(Color.FromArgb(220, 200, 30, 30), 1);
                if (vertical)
                    g.DrawLine(red, 0, cursorOffset, Thickness, cursorOffset);
                else
                    g.DrawLine(red, cursorOffset, 0, cursorOffset, Thickness);
                using var bold = new Font("Segoe UI", 9f, FontStyle.Bold);
                var text = FormatLength(cursorOffset);
                var size = g.MeasureString(text, bold);
                var at = vertical
                    ? new PointF(Thickness - size.Width - 2, Math.Min(cursorOffset + 2, Height - size.Height))
                    : new PointF(Math.Min(cursorOffset + 4, Width - size.Width), Thickness - size.Height - 2);
                using var label = new SolidBrush(Color.FromArgb(230, 255, 250, 225));
                g.FillRectangle(label, at.X - 1, at.Y, size.Width + 2, size.Height);
                g.DrawString(text, bold, Brushes.DarkRed, at);
            }

            // Total length at the far end
            using var small = new Font("Segoe UI", 8f);
            var total = FormatLength(Length);
            var totalSize = g.MeasureString(total, small);
            if (vertical)
                g.DrawString(total, small, brush, Thickness - totalSize.Width - 2, Height - totalSize.Height - 2);
            else
                g.DrawString(total, small, brush, Width - totalSize.Width - 4, Thickness - totalSize.Height - 2);
            g.DrawRectangle(tick, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            dragStart = Cursor.Position;
            dragBounds = Bounds;
            resizeEdge = (vertical ? e.Y > Height - 12 : e.X > Width - 12) ? 1 : 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = (vertical ? e.Y > Height - 12 : e.X > Width - 12) ? (vertical ? Cursors.SizeNS : Cursors.SizeWE) : Cursors.SizeAll;
            if (dragStart is not Point start)
                return;
            var dx = Cursor.Position.X - start.X;
            var dy = Cursor.Position.Y - start.Y;
            if (resizeEdge == 1)
            {
                if (vertical)
                    Height = Math.Max(100, dragBounds.Height + dy);
                else
                    Width = Math.Max(100, dragBounds.Width + dx);
            }
            else
            {
                Location = new Point(dragBounds.X + dx, dragBounds.Y + dy);
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragStart = null;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            Turn();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            var step = e.Shift ? 10 : 1;
            switch (e.KeyCode)
            {
                case Keys.Escape: Close(); break;
                case Keys.Space: Turn(); break;
                case Keys.Left: Left -= step; break;
                case Keys.Right: Left += step; break;
                case Keys.Up: Top -= step; break;
                case Keys.Down: Top += step; break;
                case Keys.U: SetUnit((Unit)(((int)unit + 1) % 3)); break;
                default: return;
            }
            e.Handled = true;
        }

        private void Turn()
        {
            vertical = !vertical;
            var length = Length;
            var center = new Point(Left + Width / 2, Top + Height / 2);
            Size = vertical ? new Size(Thickness, length) : new Size(length, Thickness);
            Location = vertical ? new Point(center.X - Thickness / 2, Top) : new Point(Left, center.Y - Thickness / 2);
            Invalidate();
        }

        private void SetUnit(Unit value)
        {
            unit = value;
            ContextMenuStrip = BuildMenu();
            Invalidate();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(Strings.RulerTurn, null, (_, _) => Turn());
            foreach (var u in Enum.GetValues<Unit>())
                menu.Items.Add(new ToolStripMenuItem(Strings.RulerUnitName(u), null, (_, _) => SetUnit(u)) { Checked = unit == u });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.RulerHelp) { Enabled = false });
            menu.Items.Add(Strings.Close, null, (_, _) => Close());
            return menu;
        }

        /// <summary>Physical width (or height) of a monitor in millimetres as it reports it (EDID); 0 if unknown.</summary>
        private static double MonitorMillimetres(Screen screen, bool height)
        {
            var dc = CreateDC("DISPLAY", screen.DeviceName, null, IntPtr.Zero);
            if (dc == IntPtr.Zero)
                return 0;
            try
            {
                return GetDeviceCaps(dc, height ? 6 /* VERTSIZE */ : 4 /* HORZSIZE */);
            }
            finally
            {
                DeleteDC(dc);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            follow.Stop();
            base.OnFormClosed(e);
        }

        /// <summary>Also when disposed without closing (e.g. the preview renderer): the timer must stop first.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                follow.Stop();
                follow.Dispose();
            }
            base.Dispose(disposing);
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDC(string driver, string device, string? output, IntPtr initData);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr dc, int index);
    }
}
