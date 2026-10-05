using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Themes
{
    /// <summary>
    /// Brettljause: a wooden cutting board with bacon, cheese, bread, sausage, pickles and radishes –
    /// real photos (Themes\Jause\*). As a sidebar background it becomes a rustic plank table with the
    /// food lying between the fences.
    /// </summary>
    public sealed class JauseTheme : FenceTheme
    {
        private static readonly Color Burnt = Color.FromArgb(78, 42, 16);
        private static readonly Color Rim = Color.FromArgb(150, 104, 58);
        private static readonly Color Accent2 = Color.FromArgb(178, 58, 48);

        private static readonly Dictionary<string, Image?> Pictures = new();

        /// <summary>An embedded photo (bacon, cheese, bread, radish, sausage, pickle, board, table); cached.</summary>
        internal static Image? Picture(string name)
        {
            lock (Pictures)
            {
                if (Pictures.TryGetValue(name, out var cached))
                    return cached;
                Image? image = null;
                foreach (var ext in new[] { ".png", ".jpg" })
                {
                    using var stream = typeof(JauseTheme).Assembly.GetManifestResourceStream("Jause." + name + ext);
                    if (stream == null)
                        continue;
                    using var loaded = Image.FromStream(stream);
                    image = new Bitmap(loaded);
                    break;
                }
                Pictures[name] = image;
                return image;
            }
        }

        public override string Id => "jause";

        public override string DisplayName => Strings.ThemeName(Id);

        public override Color Accent => Accent2;

        public override bool Glass => false;

        public override int MinAlpha => 255;

        public override int ContentInset => 4;

        public override Color HintColor => Color.FromArgb(170, Burnt);

        public override (Color Back, Color Fore) EditorColors => (Color.FromArgb(240, 222, 188), Burnt);

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Georgia", "Cambria", "Times New Roman" }, Math.Max(6, titleHeightPx * 0.42f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) =>
            CreateFont(new[] { "Segoe UI Semibold", "Segoe UI" }, 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        #region Board

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (Picture("board") is { } board)
                DrawCover(g, board, bounds, 1f);
            else
                using (var fill = new SolidBrush(Color.FromArgb(214, 172, 116)))
                    g.FillRectangle(fill, bounds);

            // Edge of the board: a darker rim and a little light on the inner edge
            using (var rim = new Pen(Color.FromArgb(200, Rim), 3 * s))
                g.DrawRectangle(rim, bounds.X + 1.5f * s, bounds.Y + 1.5f * s, bounds.Width - 3 * s, bounds.Height - 3 * s);
            using (var light = new Pen(Color.FromArgb(70, Color.White), 1.2f * s))
                g.DrawRectangle(light, bounds.X + 4 * s, bounds.Y + 4 * s, bounds.Width - 8 * s, bounds.Height - 8 * s);

            // Bread and radishes in the bottom right corner, when there is room for them
            var content = bounds.Height - titleHeight;
            if (content > 150 * s && bounds.Width > 180 * s)
            {
                var size = Math.Min(70 * s, content * 0.3f);
                DrawPicture(g, "bread", new PointF(bounds.Right - size * 0.95f, bounds.Bottom - size * 0.95f), size, -8);
                DrawPicture(g, "radish", new PointF(bounds.Right - size * 1.75f, bounds.Bottom - size * 0.7f), size * 0.7f, 12);
            }
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            // Bacon on the left, cheese on the right, the name burnt into the wood in between
            var deco = titleRect.Width > 170 * s;
            var size = Math.Max(titleRect.Height * 1.45f, 34 * s);
            if (deco)
            {
                var cy = titleRect.Y + titleRect.Height / 2f;
                DrawPicture(g, "bacon", new PointF(titleRect.X + 6 * s + size / 2, cy + 2 * s), size, 0);
                DrawPicture(g, "cheese", new PointF(titleRect.Right - 6 * s - size / 2, cy + 2 * s), size * 0.95f, 0);
            }
            var margin = deco ? size + 10 * s : 8 * s;
            var textRect = new RectangleF(titleRect.X + margin, titleRect.Y, Math.Max(10, titleRect.Width - 2 * margin), titleRect.Height);
            using var format = TitleFormat(StringAlignment.Center);
            DrawShadowedString(g, text, font, Burnt, Color.FromArgb(110, 255, 236, 200), textRect, format, s);
        }

        // Hover = a soft lighter spot on the wood, selection a bit stronger with a thin edge
        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            var r = RectangleF.Inflate(rect, -2 * s, -1);
            FillRounded(g, Color.FromArgb(selected ? 110 : 70, 255, 246, 226), r, 6 * s);
            if (selected)
                DrawRounded(g, Color.FromArgb(150, Rim), 1.2f * s, r, 6 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Burnt, Color.FromArgb(120, 255, 240, 210), rect, format, s * 0.8f);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(200, Rim), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Accent2, x, top, height, 3 * s);

        #endregion

        #region Table (sidebar background)

        private static readonly string[] TableFood = { "bacon", "cheese", "sausage", "bread", "pickle", "radish" };

        public override void DrawBar(Graphics g, Rectangle bounds, bool vertical, FenceInfo info, float s)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (Picture("table") is { } table)
            {
                // The planks run along the bar: repeat the picture along it, turned for a sidebar
                var state = g.Save();
                g.SetClip(bounds, CombineMode.Intersect);
                if (vertical)
                {
                    g.TranslateTransform(bounds.Right, bounds.Top);
                    g.RotateTransform(90);
                    TileAlong(g, table, new Rectangle(0, 0, bounds.Height, bounds.Width));
                }
                else
                {
                    TileAlong(g, table, bounds);
                }
                g.Restore(state);
            }
            else
            {
                using var fill = new SolidBrush(Color.FromArgb(92, 58, 32));
                g.FillRectangle(fill, bounds);
            }

            // Food along the table; it peeks out between and beside the fences
            var rng = Seeded(info);
            var length = vertical ? bounds.Height : bounds.Width;
            var across = vertical ? bounds.Width : bounds.Height;
            var size = Math.Clamp(across * 0.32f, 40 * s, 110 * s);
            var i = rng.Next(TableFood.Length);
            for (var pos = size * 0.6f; pos < length - size * 0.4f; pos += size * (0.9f + (float)rng.NextDouble() * 0.6f))
            {
                // Alternate sides, so the food lies along both edges
                var side = (i % 2 == 0 ? 0.18f : 0.82f) + ((float)rng.NextDouble() - 0.5f) * 0.12f;
                var center = vertical
                    ? new PointF(bounds.X + across * side, bounds.Y + pos)
                    : new PointF(bounds.X + pos, bounds.Y + across * side);
                DrawPicture(g, TableFood[i % TableFood.Length], center, size, rng.Next(-35, 35));
                i++;
            }
        }

        private static Image? mirrored;

        /// <summary>
        /// The texture repeated along the rectangle's width, scaled to its height; every second copy is
        /// mirrored, so the joins match and no hard seams show.
        /// </summary>
        private static void TileAlong(Graphics g, Image image, Rectangle area)
        {
            if (mirrored == null)
            {
                var flipped = new Bitmap(image);
                flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
                mirrored = flipped;
            }
            var scale = area.Height / (float)image.Height;
            var w = Math.Max(1, image.Width * scale);
            // Scaling blends the picture's edge with "nothing" (a thin dark line); repeating the edge pixels avoids that
            using var attributes = new ImageAttributes();
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            var flip = false;
            for (var x = (float)area.X; x < area.Right; x += w - 2, flip = !flip)
            {
                var tile = flip ? mirrored : image;
                g.DrawImage(tile, new[] { new PointF(x, area.Y), new PointF(x + w, area.Y), new PointF(x, area.Bottom) },
                    new RectangleF(0, 0, tile.Width, tile.Height), GraphicsUnit.Pixel, attributes);
            }
        }

        #endregion

        /// <summary>A photo centered on a point, at most <paramref name="size"/> wide or high, turned by <paramref name="angle"/> degrees.</summary>
        private static void DrawPicture(Graphics g, string name, PointF center, float size, float angle)
        {
            if (Picture(name) is not { } image)
                return;
            var factor = size / Math.Max(image.Width, image.Height);
            var w = image.Width * factor;
            var h = image.Height * factor;
            var state = g.Save();
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TranslateTransform(center.X, center.Y);
            if (angle != 0)
                g.RotateTransform(angle);
            g.DrawImage(image, -w / 2, -h / 2, w, h);
            g.Restore(state);
        }

        /// <summary>Fills the rectangle with the picture (cut off what sticks out), slightly faded with <paramref name="opacity"/> &lt; 1.</summary>
        private static void DrawCover(Graphics g, Image image, Rectangle area, float opacity)
        {
            var factor = Math.Max(area.Width / (float)image.Width, area.Height / (float)image.Height);
            var w = image.Width * factor;
            var h = image.Height * factor;
            var target = new RectangleF(area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
            var state = g.Save();
            g.SetClip(area, CombineMode.Intersect);
            if (opacity >= 1)
            {
                g.DrawImage(image, target);
            }
            else
            {
                using var attributes = new ImageAttributes();
                attributes.SetColorMatrix(new ColorMatrix { Matrix33 = opacity });
                g.DrawImage(image, Rectangle.Round(target), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            g.Restore(state);
        }
    }
}
