using System.Drawing.Drawing2D;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Color picker: freezes each screen, shows a magnifier at the mouse; a click copies the color as
    /// #RRGGBB (Shift+click: rgb(r, g, b)), Esc cancels.
    /// </summary>
    public static class ColorPicker
    {
        private static List<Overlay>? open;

        public static void Start(Action<string> picked)
        {
            if (open != null)
                return;
            open = new List<Overlay>();
            foreach (var screen in Screen.AllScreens)
            {
                var shot = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var g = Graphics.FromImage(shot))
                    g.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                var overlay = new Overlay(screen.Bounds, shot, (text) =>
                {
                    Close();
                    if (text != null)
                        picked(text);
                });
                open.Add(overlay);
                overlay.Show();
            }
            open.FirstOrDefault(o => o.Bounds.Contains(Cursor.Position))?.Activate();
        }

        private static void Close()
        {
            var overlays = open;
            open = null;
            if (overlays == null)
                return;
            foreach (var o in overlays)
            {
                o.Close();
                o.Dispose();
            }
        }

        public static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static string Rgb(Color c) => $"rgb({c.R}, {c.G}, {c.B})";

        private sealed class Overlay : Form
        {
            private const int Zoom = 10, Radius = 7;
            private readonly Bitmap shot;
            private readonly Action<string?> done;
            private Point mouse;

            public Overlay(Rectangle bounds, Bitmap shot, Action<string?> done)
            {
                this.shot = shot;
                this.done = done;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                StartPosition = FormStartPosition.Manual;
                AutoScaleMode = AutoScaleMode.None;
                Bounds = bounds;
                DoubleBuffered = true;
                Cursor = Cursors.Cross;
                KeyPreview = true;
                mouse = PointToClientSafe(Cursor.Position);
            }

            private Point PointToClientSafe(Point screen) => new(screen.X - Bounds.X, screen.Y - Bounds.Y);

            private Color ColorAt(Point p) =>
                p.X >= 0 && p.Y >= 0 && p.X < shot.Width && p.Y < shot.Height ? shot.GetPixel(p.X, p.Y) : Color.Black;

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                var old = mouse;
                mouse = e.Location;
                // Only the magnifier area changes
                Invalidate(MagnifierRect(old));
                Invalidate(MagnifierRect(mouse));
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Right)
                {
                    done(null);
                    return;
                }
                var color = ColorAt(e.Location);
                done((ModifierKeys & Keys.Shift) != 0 ? Rgb(color) : Hex(color));
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.Escape)
                    done(null);
            }

            private Rectangle MagnifierRect(Point at)
            {
                var size = (2 * Radius + 1) * Zoom;
                var x = at.X + 24;
                var y = at.Y + 24;
                if (x + size > ClientSize.Width)
                    x = at.X - 24 - size;
                if (y + size + 30 > ClientSize.Height)
                    y = at.Y - 24 - size - 30;
                return new Rectangle(x - 2, y - 2, size + 4, size + 34);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.DrawImageUnscaled(shot, 0, 0);

                // Hint at the top
                using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
                var hint = Strings.ColorPickerHint;
                var hintSize = g.MeasureString(hint, font);
                var hintRect = new RectangleF((ClientSize.Width - hintSize.Width) / 2 - 10, 16, hintSize.Width + 20, hintSize.Height + 8);
                using (var back = new SolidBrush(Color.FromArgb(210, 20, 20, 24)))
                    g.FillRectangle(back, hintRect);
                g.DrawString(hint, font, Brushes.White, hintRect.X + 10, hintRect.Y + 4);

                // Magnifier: the pixels around the mouse, enlarged, with the center one framed
                var rect = MagnifierRect(mouse);
                var size = (2 * Radius + 1) * Zoom;
                var inner = new Rectangle(rect.X + 2, rect.Y + 2, size, size);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(shot, inner, new Rectangle(mouse.X - Radius, mouse.Y - Radius, 2 * Radius + 1, 2 * Radius + 1), GraphicsUnit.Pixel);
                g.PixelOffsetMode = PixelOffsetMode.Default;
                using (var grid = new Pen(Color.FromArgb(40, 0, 0, 0)))
                {
                    for (var i = 1; i < 2 * Radius + 1; i++)
                    {
                        g.DrawLine(grid, inner.X + i * Zoom, inner.Y, inner.X + i * Zoom, inner.Bottom);
                        g.DrawLine(grid, inner.X, inner.Y + i * Zoom, inner.Right, inner.Y + i * Zoom);
                    }
                }
                g.DrawRectangle(Pens.White, inner.X + Radius * Zoom, inner.Y + Radius * Zoom, Zoom, Zoom);
                g.DrawRectangle(Pens.Black, inner.X + Radius * Zoom - 1, inner.Y + Radius * Zoom - 1, Zoom + 2, Zoom + 2);
                g.DrawRectangle(Pens.Black, inner);

                // Color swatch and value under it
                var color = ColorAt(mouse);
                var label = new Rectangle(inner.X, inner.Bottom + 2, size, 28);
                using (var back = new SolidBrush(Color.FromArgb(230, 20, 20, 24)))
                    g.FillRectangle(back, label);
                using (var swatch = new SolidBrush(color))
                    g.FillRectangle(swatch, label.X + 4, label.Y + 4, 20, 20);
                using var small = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                g.DrawString(Hex(color), small, Brushes.White, label.X + 30, label.Y + 5);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    shot.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
