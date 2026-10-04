using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using NoFences.Util;

namespace NoFences
{
    /// <summary>A picture or pattern behind a fence's content, faded to the chosen opacity.</summary>
    public sealed partial class FenceWindow
    {
        private Image? backgroundImage;
        private string? backgroundImagePath;

        /// <summary>The picture, read once into memory (the file stays free to change or delete).</summary>
        private Image? BackgroundPicture()
        {
            var path = Info.BackgroundImage;
            if (path != backgroundImagePath)
            {
                backgroundImage?.Dispose();
                backgroundImage = null;
                backgroundImagePath = path;
                if (path != null && File.Exists(AppData.Resolve(path)))
                {
                    try
                    {
                        using var stream = new MemoryStream(File.ReadAllBytes(AppData.Resolve(path)));
                        using var loaded = Image.FromStream(stream);
                        backgroundImage = new Bitmap(loaded);
                    }
                    catch (Exception e) when (e is ArgumentException or IOException or OutOfMemoryException)
                    {
                        Log.Write("Background", $"{path}: {e.Message}");
                    }
                }
            }
            return backgroundImage;
        }

        private void DrawBackgroundPicture(Graphics g)
        {
            if (collapsed || Info.BackgroundImage == null || BackgroundPicture() is not { } image)
                return;
            var ins = theme.SurfaceInsets;
            var area = Rectangle.FromLTRB(Px(ins.Left), titleHeight, ClientSize.Width - Px(ins.Right), ClientSize.Height - Px(ins.Bottom));
            if (area.Width <= 0 || area.Height <= 0)
                return;

            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Clamp(Info.BackgroundImageOpacity, 5, 100) / 100f });
            var clip = g.Clip;
            g.SetClip(area, CombineMode.Intersect);
            if (Info.BackgroundImageTiled)
            {
                // Patterns: repeated at their own size (scaled with the screen)
                var w = Math.Max(4, (int)(image.Width * scale));
                var h = Math.Max(4, (int)(image.Height * scale));
                for (var y = area.Top; y < area.Bottom; y += h)
                    for (var x = area.Left; x < area.Right; x += w)
                        g.DrawImage(image, new Rectangle(x, y, w, h), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            else
            {
                // Photos: fill the area, cut off what sticks out (centered)
                var factor = Math.Max(area.Width / (float)image.Width, area.Height / (float)image.Height);
                var w = image.Width * factor;
                var h = image.Height * factor;
                var target = new RectangleF(area.Left + (area.Width - w) / 2, area.Top + (area.Height - h) / 2, w, h);
                g.DrawImage(image, Rectangle.Round(target), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            g.Clip = clip;
        }

        private void DisposeBackgroundPicture()
        {
            backgroundImage?.Dispose();
            backgroundImage = null;
        }
    }
}
