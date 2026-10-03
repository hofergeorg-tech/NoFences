using System.Drawing.Drawing2D;

namespace NoFences.Util
{
    /// <summary>
    /// Small drawn flags for the language choice (Windows' emoji font has no country flags).
    /// "auto" gets a globe.
    /// </summary>
    public static class Flags
    {
        private static readonly Dictionary<(string, int), Bitmap> Cache = new();

        /// <summary>A flag <paramref name="height"/> pixels high (3:2); cached, don't dispose.</summary>
        public static Bitmap For(string language, int height = 12)
        {
            if (Cache.TryGetValue((language, height), out var cached))
                return cached;
            var width = height * 3 / 2;
            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new RectangleF(0, 0, width, height);
                switch (language)
                {
                    case "de":
                        Stripes(g, r, horizontal: true, Color.Black, Color.FromArgb(221, 0, 0), Color.FromArgb(255, 206, 0));
                        break;
                    case "it":
                        Stripes(g, r, horizontal: false, Color.FromArgb(0, 146, 70), Color.White, Color.FromArgb(206, 43, 55));
                        break;
                    case "fr":
                        Stripes(g, r, horizontal: false, Color.FromArgb(0, 35, 149), Color.White, Color.FromArgb(237, 41, 57));
                        break;
                    case "es":
                        // Red, yellow twice as high, red
                        Stripes(g, r, horizontal: true, Color.FromArgb(170, 21, 27), Color.FromArgb(241, 191, 0), Color.FromArgb(241, 191, 0), Color.FromArgb(170, 21, 27));
                        break;
                    case "en":
                        UnionJack(g, r);
                        break;
                    default:
                        Globe(g, new RectangleF((width - height) / 2f, 0, height, height));
                        break;
                }
                if (language is "de" or "it" or "en" or "fr" or "es")
                {
                    using var border = new Pen(Color.FromArgb(90, 0, 0, 0));
                    g.SmoothingMode = SmoothingMode.None;
                    g.DrawRectangle(border, 0, 0, width - 1, height - 1);
                }
            }
            Cache[(language, height)] = bitmap;
            return bitmap;
        }

        private static void Stripes(Graphics g, RectangleF r, bool horizontal, params Color[] colors)
        {
            for (var i = 0; i < colors.Length; i++)
            {
                using var brush = new SolidBrush(colors[i]);
                if (horizontal)
                    g.FillRectangle(brush, r.X, r.Y + r.Height * i / colors.Length, r.Width, r.Height / colors.Length + 0.5f);
                else
                    g.FillRectangle(brush, r.X + r.Width * i / colors.Length, r.Y, r.Width / colors.Length + 0.5f, r.Height);
            }
        }

        private static void UnionJack(Graphics g, RectangleF r)
        {
            var blue = Color.FromArgb(1, 33, 105);
            var red = Color.FromArgb(200, 16, 46);
            using (var b = new SolidBrush(blue))
                g.FillRectangle(b, r);
            var h = r.Height;
            using (var white = new Pen(Color.White, h * 0.2f))
            {
                g.DrawLine(white, r.Left, r.Top, r.Right, r.Bottom);
                g.DrawLine(white, r.Left, r.Bottom, r.Right, r.Top);
            }
            using (var thinRed = new Pen(red, h * 0.08f))
            {
                g.DrawLine(thinRed, r.Left, r.Top, r.Right, r.Bottom);
                g.DrawLine(thinRed, r.Left, r.Bottom, r.Right, r.Top);
            }
            using (var white = new SolidBrush(Color.White))
            {
                g.FillRectangle(white, r.X + r.Width / 2 - h * 0.17f, r.Y, h * 0.34f, h);
                g.FillRectangle(white, r.X, r.Y + h / 2 - h * 0.17f, r.Width, h * 0.34f);
            }
            using (var redBrush = new SolidBrush(red))
            {
                g.FillRectangle(redBrush, r.X + r.Width / 2 - h * 0.1f, r.Y, h * 0.2f, h);
                g.FillRectangle(redBrush, r.X, r.Y + h / 2 - h * 0.1f, r.Width, h * 0.2f);
            }
        }

        private static void Globe(Graphics g, RectangleF r)
        {
            r.Inflate(-0.5f, -0.5f);
            var blue = Color.FromArgb(0, 120, 212);
            using var pen = new Pen(blue, Math.Max(1, r.Height / 12));
            g.DrawEllipse(pen, r);
            g.DrawEllipse(pen, r.X + r.Width * 0.3f, r.Y, r.Width * 0.4f, r.Height);
            g.DrawLine(pen, r.Left, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
            g.DrawLine(pen, r.X + r.Width / 2, r.Top, r.X + r.Width / 2, r.Bottom);
        }
    }
}
