using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Screen magnifier: a round lens follows the mouse and shows what's under it enlarged (2×–8×).
    /// +/– or the mouse wheel change the zoom, Esc or a click closes it. The lens is excluded from its
    /// own screen capture, so it never magnifies itself.
    /// </summary>
    public sealed class Magnifier : Form
    {
        private static Magnifier? open;
        private static readonly int[] Zooms = { 2, 3, 4, 6, 8 };

        private readonly System.Windows.Forms.Timer timer = new() { Interval = 30 };
        private Bitmap? frame;
        private int zoomIndex = 1;
        private Point lastCursor;

        public static void Toggle()
        {
            if (open is { IsDisposed: false })
            {
                open.Close();
                return;
            }
            open = new Magnifier();
            open.Show();
            open.Activate();
        }

        private Magnifier()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            KeyPreview = true;
            var size = (int)(240 * DeviceDpi / 96f);
            Size = new Size(size, size);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, size, size);
                Region = new Region(path);
            }
            timer.Tick += (_, _) => Capture();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Leave the lens out of screen captures (Windows 10 2004+)
            SetWindowDisplayAffinity(Handle, WDA_EXCLUDEFROMCAPTURE);
            Native.HideFromAltTab(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Capture();
            timer.Start();
        }

        private int Zoom => Zooms[zoomIndex];

        /// <summary>The screen area shown in a lens of <paramref name="lensSize"/> px at <paramref name="zoom"/>.</summary>
        public static Rectangle SourceArea(Point cursor, int lensSize, int zoom)
        {
            var side = Math.Max(1, lensSize / zoom);
            return new Rectangle(cursor.X - side / 2, cursor.Y - side / 2, side, side);
        }

        /// <summary>Lens position: beside the cursor, flipped to stay on its monitor.</summary>
        public static Point LensPosition(Point cursor, Size lens, Rectangle screen)
        {
            var gap = lens.Width / 8;
            var x = cursor.X + gap;
            var y = cursor.Y + gap;
            if (x + lens.Width > screen.Right)
                x = cursor.X - gap - lens.Width;
            if (y + lens.Height > screen.Bottom)
                y = cursor.Y - gap - lens.Height;
            return new Point(Math.Max(screen.Left, x), Math.Max(screen.Top, y));
        }

        private new void Capture()
        {
            var cursor = Cursor.Position;
            if (cursor != lastCursor)
            {
                Location = LensPosition(cursor, Size, Screen.FromPoint(cursor).Bounds);
                lastCursor = cursor;
            }
            if (Native.IsKeyDown(Native.VK_ESCAPE))
            {
                Close();
                return;
            }
            var source = SourceArea(cursor, Width, Zoom);
            frame ??= new Bitmap(source.Width, source.Height);
            if (frame.Width != source.Width)
            {
                frame.Dispose();
                frame = new Bitmap(source.Width, source.Height);
            }
            try
            {
                using var g = Graphics.FromImage(frame);
                g.CopyFromScreen(source.Location, Point.Empty, source.Size);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Secure desktop (UAC prompt) or a locked screen: nothing to show
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (frame != null)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(frame, new Rectangle(0, 0, Width, Height));
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var ring = new Pen(Color.FromArgb(220, 40, 40, 44), 4);
            g.DrawEllipse(ring, 2, 2, Width - 5, Height - 5);
            // Small cross in the middle: the exact pixel under the mouse
            using var cross = new Pen(Color.FromArgb(150, 255, 60, 60), 1);
            g.DrawLine(cross, Width / 2 - 6, Height / 2, Width / 2 + 6, Height / 2);
            g.DrawLine(cross, Width / 2, Height / 2 - 6, Width / 2, Height / 2 + 6);
            using var font = new Font(SystemFonts.MessageBoxFont ?? Font, FontStyle.Bold);
            var label = $"{Zoom}×";
            var size = g.MeasureString(label, font);
            using var back = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
            g.FillRectangle(back, (Width - size.Width) / 2 - 4, Height - size.Height - 14, size.Width + 8, size.Height);
            g.DrawString(label, font, Brushes.White, (Width - size.Width) / 2, Height - size.Height - 14);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                case Keys.Add or Keys.Oemplus or Keys.Up:
                    zoomIndex = Math.Min(Zooms.Length - 1, zoomIndex + 1);
                    break;
                case Keys.Subtract or Keys.OemMinus or Keys.Down:
                    zoomIndex = Math.Max(0, zoomIndex - 1);
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            zoomIndex = Math.Clamp(zoomIndex + Math.Sign(e.Delta), 0, Zooms.Length - 1);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Close();
        }

        // Clicking another window ends the magnifier
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            BeginInvoke(Close);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                frame?.Dispose();
                if (open == this)
                    open = null;
            }
            base.Dispose(disposing);
        }

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);
    }
}
