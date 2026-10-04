using System.Drawing.Drawing2D;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Themes
{
    /// <summary>
    /// Brettljause: a wooden cutting board with slices of bacon, a wedge of cheese, bread and radishes.
    /// As a sidebar background it becomes a wooden table with a checked cloth and food between the fences.
    /// </summary>
    public sealed class JauseTheme : FenceTheme
    {
        private static readonly Color BoardLight = Color.FromArgb(222, 180, 122);
        private static readonly Color BoardDark = Color.FromArgb(196, 148, 92);
        private static readonly Color Grain = Color.FromArgb(150, 104, 58);
        private static readonly Color Rim = Color.FromArgb(132, 86, 44);
        private static readonly Color Burnt = Color.FromArgb(84, 46, 20);
        private static readonly Color Meat = Color.FromArgb(186, 62, 58);
        private static readonly Color MeatDark = Color.FromArgb(120, 34, 30);
        private static readonly Color Fat = Color.FromArgb(250, 236, 226);
        private static readonly Color Cheese = Color.FromArgb(252, 210, 86);
        private static readonly Color CheeseRind = Color.FromArgb(226, 164, 44);
        private static readonly Color Crust = Color.FromArgb(150, 92, 40);
        private static readonly Color Crumb = Color.FromArgb(236, 206, 156);
        private static readonly Color Radish = Color.FromArgb(214, 52, 92);
        private static readonly Color Gingham = Color.FromArgb(200, 40, 46);
        private static readonly Color TableLight = Color.FromArgb(120, 76, 42);
        private static readonly Color TableDark = Color.FromArgb(88, 54, 28);

        public override string Id => "jause";

        public override string DisplayName => Strings.ThemeName(Id);

        public override Color Accent => Meat;

        public override bool Glass => false;

        public override int MinAlpha => 255;

        public override int ContentInset => 4;

        public override Color HintColor => Color.FromArgb(170, Burnt);

        public override (Color Back, Color Fore) EditorColors => (Color.FromArgb(240, 220, 182), Burnt);

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(new[] { "Georgia", "Cambria", "Times New Roman" }, Math.Max(6, titleHeightPx * 0.42f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) =>
            CreateFont(new[] { "Segoe UI Semibold", "Segoe UI" }, 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        #region Board

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            DrawWood(g, bounds, info, s, BoardLight, BoardDark, Grain, planks: false);

            // Rim and a lighter bevel inside it
            using (var rim = new Pen(Rim, 3 * s))
                g.DrawRectangle(rim, bounds.X + 1.5f * s, bounds.Y + 1.5f * s, bounds.Width - 3 * s, bounds.Height - 3 * s);
            using (var bevel = new Pen(Color.FromArgb(90, Color.White), 1.2f * s))
                g.DrawRectangle(bevel, bounds.X + 4 * s, bounds.Y + 4 * s, bounds.Width - 8 * s, bounds.Height - 8 * s);

            // Bread and radishes in the bottom right corner, when there is room
            if (bounds.Height - titleHeight > 120 * s && bounds.Width > 160 * s)
            {
                var size = 46 * s;
                DrawBread(g, new RectangleF(bounds.Right - size * 1.55f, bounds.Bottom - size * 1.15f, size * 1.3f, size * 0.85f), s);
                DrawRadish(g, new PointF(bounds.Right - size * 1.75f, bounds.Bottom - size * 0.45f), 7 * s);
                DrawRadish(g, new PointF(bounds.Right - size * 2.1f, bounds.Bottom - size * 0.62f), 6 * s);
            }
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            // Bacon on the left, cheese on the right, the name burnt into the wood in between
            var h = Math.Min(titleRect.Height * 0.86f, 36 * s);
            var deco = titleRect.Width > 170 * s;
            if (deco)
            {
                var y = titleRect.Y + (titleRect.Height - h) / 2;
                DrawBacon(g, new RectangleF(titleRect.X + 8 * s, y + h * 0.02f, h * 2f, h * 0.5f), -9, s);
                DrawBacon(g, new RectangleF(titleRect.X + 12 * s, y + h * 0.42f, h * 2f, h * 0.5f), 5, s);
                DrawCheese(g, new RectangleF(titleRect.Right - 10 * s - h * 1.5f, y + h * 0.05f, h * 1.5f, h * 0.95f), s);
            }
            var margin = deco ? h * 2.3f + 12 * s : 8 * s;
            var textRect = new RectangleF(titleRect.X + margin, titleRect.Y, Math.Max(10, titleRect.Width - 2 * margin), titleRect.Height);
            using var format = TitleFormat(StringAlignment.Center);
            DrawShadowedString(g, text, font, Burnt, Color.FromArgb(110, 255, 236, 200), textRect, format, s);
        }

        // Hover = a red-white checked napkin under the item
        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            var r = RectangleF.Inflate(rect, -2 * s, -1);
            using (var shadow = new SolidBrush(Color.FromArgb(50, 60, 30, 0)))
                g.FillRectangle(shadow, r.X + 2 * s, r.Y + 2 * s, r.Width, r.Height);
            DrawGingham(g, r, s, selected ? 150 : 95, 9 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Burnt, Color.FromArgb(120, 255, 240, 210), rect, format, s * 0.8f);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(200, Rim), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Meat, x, top, height, 3 * s);

        #endregion

        #region Table (sidebar background)

        public override void DrawBar(Graphics g, Rectangle bounds, bool vertical, FenceInfo info, float s)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawWood(g, bounds, info, s, TableLight, TableDark, Color.FromArgb(70, 40, 18), planks: true, vertical: vertical);

            // A checked runner along the table
            var runner = vertical
                ? new RectangleF(bounds.X + bounds.Width * 0.18f, bounds.Y, bounds.Width * 0.64f, bounds.Height)
                : new RectangleF(bounds.X, bounds.Y + bounds.Height * 0.18f, bounds.Width, bounds.Height * 0.64f);
            DrawGingham(g, runner, s, 150, 14 * s);

            // Food along the table; some of it peeks out between the fences
            var rng = Seeded(info);
            var length = vertical ? bounds.Height : bounds.Width;
            var step = 105 * s;
            for (var pos = 24 * s; pos < length - 20 * s; pos += step * (0.8f + (float)rng.NextDouble() * 0.5f))
            {
                var across = (float)rng.NextDouble();
                var size = 44 * s;
                var p = vertical
                    ? new PointF(bounds.X + 8 * s + across * Math.Max(1, bounds.Width - size - 16 * s), bounds.Y + pos)
                    : new PointF(bounds.X + pos, bounds.Y + 8 * s + across * Math.Max(1, bounds.Height - size - 16 * s));
                switch (rng.Next(4))
                {
                    case 0: DrawBacon(g, new RectangleF(p.X, p.Y, size * 1.4f, size * 0.5f), rng.Next(-25, 25), s); break;
                    case 1: DrawCheese(g, new RectangleF(p.X, p.Y, size * 1.2f, size * 0.9f), s); break;
                    case 2: DrawBread(g, new RectangleF(p.X, p.Y, size * 1.3f, size * 0.85f), s); break;
                    default:
                        DrawRadish(g, new PointF(p.X + 10 * s, p.Y + 10 * s), 9 * s);
                        DrawRadish(g, new PointF(p.X + 26 * s, p.Y + 18 * s), 8 * s);
                        DrawRadish(g, new PointF(p.X + 14 * s, p.Y + 28 * s), 7 * s);
                        break;
                }
            }
        }

        #endregion

        #region Food and materials

        private static void DrawWood(Graphics g, Rectangle bounds, FenceInfo info, float s, Color light, Color dark, Color grain, bool planks, bool vertical = false)
        {
            var horizontalGrain = !vertical;
            using (var fill = new LinearGradientBrush(bounds, light, dark, horizontalGrain ? LinearGradientMode.Vertical : LinearGradientMode.Horizontal))
                g.FillRectangle(fill, bounds);

            var rng = Seeded(info);
            var state = g.Save();
            g.SetClip(bounds, CombineMode.Intersect);
            var lines = Math.Clamp((horizontalGrain ? bounds.Height : bounds.Width) / (int)Math.Max(1, 7 * s), 4, 160);
            for (var i = 0; i < lines; i++)
            {
                using var pen = new Pen(Color.FromArgb(25 + rng.Next(45), grain), (0.8f + (float)rng.NextDouble() * 1.4f) * s);
                var offset = (float)rng.NextDouble() * (horizontalGrain ? bounds.Height : bounds.Width);
                var wave = (2 + (float)rng.NextDouble() * 5) * s;
                var phase = (float)rng.NextDouble() * 6;
                var points = new List<PointF>();
                var length = horizontalGrain ? bounds.Width : bounds.Height;
                for (var t = 0f; t <= length + 20; t += 18 * s)
                {
                    var d = offset + (float)Math.Sin(t / (60 * s) + phase) * wave;
                    points.Add(horizontalGrain ? new PointF(bounds.X + t, bounds.Y + d) : new PointF(bounds.X + d, bounds.Y + t));
                }
                if (points.Count > 1)
                    g.DrawCurve(pen, points.ToArray(), 0.5f);
            }
            // A knot or two
            for (var k = 0; k < 1 + bounds.Width * bounds.Height / (int)(160_000 * s * s); k++)
            {
                var kx = bounds.X + rng.Next(Math.Max(1, bounds.Width));
                var ky = bounds.Y + rng.Next(Math.Max(1, bounds.Height));
                using var knot = new Pen(Color.FromArgb(70, grain), 1.4f * s);
                g.DrawEllipse(knot, kx, ky, 14 * s, 7 * s);
                g.DrawEllipse(knot, kx + 3 * s, ky + 1.5f * s, 8 * s, 4 * s);
            }
            if (planks)
            {
                // Joints between the table's planks
                using var joint = new Pen(Color.FromArgb(120, 30, 16, 6), 1.6f * s);
                var width = 46 * s;
                if (horizontalGrain)
                    for (var y = bounds.Y + width; y < bounds.Bottom; y += width) g.DrawLine(joint, bounds.X, y, bounds.Right, y);
                else
                    for (var x = bounds.X + width; x < bounds.Right; x += width) g.DrawLine(joint, x, bounds.Y, x, bounds.Bottom);
            }
            g.Restore(state);
        }

        private static void DrawGingham(Graphics g, RectangleF r, float s, int alpha, float check)
        {
            var state = g.Save();
            g.SetClip(r, CombineMode.Intersect);
            using (var white = new SolidBrush(Color.FromArgb(alpha, 250, 246, 238)))
                g.FillRectangle(white, r);
            using var red = new SolidBrush(Color.FromArgb(alpha / 2, Gingham));
            for (var x = r.X; x < r.Right; x += check * 2)
                g.FillRectangle(red, x, r.Y, check, r.Height);
            for (var y = r.Y; y < r.Bottom; y += check * 2)
                g.FillRectangle(red, r.X, y, r.Width, check);
            g.Restore(state);
        }

        /// <summary>A slice of bacon: dark rind on top, red meat with white fat stripes.</summary>
        private static void DrawBacon(Graphics g, RectangleF r, float angle, float s)
        {
            var state = g.Save();
            g.TranslateTransform(r.X + r.Width / 2, r.Y + r.Height / 2);
            g.RotateTransform(angle);
            var slice = new RectangleF(-r.Width / 2, -r.Height / 2, r.Width, r.Height);
            using (var shadow = new SolidBrush(Color.FromArgb(60, 40, 20, 0)))
                FillPath(g, shadow, Wavy(slice.Offset2(1.5f * s, 1.5f * s)));
            using (var path = Wavy(slice))
            {
                using (var meat = new LinearGradientBrush(slice, Meat, MeatDark, LinearGradientMode.Vertical))
                    g.FillPath(meat, path);
                var clip = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                // Fat runs through the meat in irregular, wavy streaks
                void Streak(float at, float thickness, float wobble, int alpha)
                {
                    using var fat = new Pen(Color.FromArgb(alpha, Fat), slice.Height * thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    var y = slice.Y + slice.Height * at;
                    g.DrawBezier(fat, slice.Left - 2, y, slice.Left + slice.Width * 0.3f, y - slice.Height * wobble,
                        slice.Left + slice.Width * 0.65f, y + slice.Height * wobble, slice.Right + 2, y - slice.Height * wobble * 0.5f);
                }
                Streak(0.42f, 0.16f, 0.12f, 235);
                Streak(0.74f, 0.1f, 0.18f, 200);
                Streak(0.58f, 0.04f, 0.25f, 120);
                // Smoked rind along the top edge
                using (var rind = new LinearGradientBrush(new RectangleF(slice.X, slice.Y - 1, slice.Width, slice.Height * 0.22f), Color.FromArgb(120, 60, 24), Color.FromArgb(0, 120, 60, 24), LinearGradientMode.Vertical))
                    g.FillRectangle(rind, slice.X, slice.Y - slice.Height * 0.2f, slice.Width, slice.Height * 0.42f);
                g.Restore(clip);
                using var edge = new Pen(Color.FromArgb(120, 70, 20, 16), 0.8f * s);
                g.DrawPath(edge, path);
            }
            g.Restore(state);
        }

        private static GraphicsPath Wavy(RectangleF r)
        {
            var path = new GraphicsPath();
            var w = r.Height * 0.2f;
            path.AddBezier(r.Left, r.Top, r.Left + r.Width / 3, r.Top - w, r.Left + r.Width * 2 / 3, r.Top + w, r.Right, r.Top);
            path.AddLine(r.Right, r.Top, r.Right, r.Bottom);
            path.AddBezier(r.Right, r.Bottom, r.Left + r.Width * 2 / 3, r.Bottom + w, r.Left + r.Width / 3, r.Bottom - w, r.Left, r.Bottom);
            path.CloseFigure();
            return path;
        }

        private static void FillPath(Graphics g, Brush brush, GraphicsPath path)
        {
            using (path)
                g.FillPath(brush, path);
        }

        /// <summary>A wedge of cheese with holes.</summary>
        private static void DrawCheese(Graphics g, RectangleF r, float s)
        {
            var points = new[] { new PointF(r.Left, r.Bottom), new PointF(r.Right, r.Bottom), new PointF(r.Right, r.Top + r.Height * 0.15f) };
            using (var shadow = new SolidBrush(Color.FromArgb(60, 40, 20, 0)))
                g.FillPolygon(shadow, points.Select(p => new PointF(p.X + 1.5f * s, p.Y + 1.5f * s)).ToArray());
            using (var fill = new LinearGradientBrush(r, Cheese, Color.FromArgb(240, 186, 60), LinearGradientMode.ForwardDiagonal))
                g.FillPolygon(fill, points);
            using (var holes = new SolidBrush(Color.FromArgb(220, 214, 160, 50)))
            {
                g.FillEllipse(holes, r.Left + r.Width * 0.55f, r.Top + r.Height * 0.58f, r.Width * 0.16f, r.Height * 0.16f);
                g.FillEllipse(holes, r.Left + r.Width * 0.76f, r.Top + r.Height * 0.42f, r.Width * 0.1f, r.Height * 0.1f);
                g.FillEllipse(holes, r.Left + r.Width * 0.32f, r.Top + r.Height * 0.8f, r.Width * 0.11f, r.Height * 0.1f);
            }
            using var rind = new Pen(CheeseRind, 1.6f * s);
            g.DrawPolygon(rind, points);
        }

        /// <summary>A slice of bread: brown crust around a light crumb.</summary>
        private static void DrawBread(Graphics g, RectangleF r, float s)
        {
            using (var shadow = new SolidBrush(Color.FromArgb(60, 40, 20, 0)))
                g.FillEllipse(shadow, r.X + 2 * s, r.Y + 2 * s, r.Width, r.Height);
            using (var crust = new SolidBrush(Crust))
                g.FillEllipse(crust, r);
            var inner = RectangleF.Inflate(r, -r.Width * 0.09f, -r.Height * 0.12f);
            using (var crumb = new SolidBrush(Crumb))
                g.FillEllipse(crumb, inner);
            using var pores = new SolidBrush(Color.FromArgb(70, Crust));
            for (var i = 0; i < 6; i++)
                g.FillEllipse(pores, inner.X + inner.Width * (0.2f + i % 3 * 0.25f), inner.Y + inner.Height * (0.3f + i / 3 * 0.3f), 2.4f * s, 1.6f * s);
        }

        /// <summary>A radish slice: white inside, pink rim.</summary>
        private static void DrawRadish(Graphics g, PointF center, float radius)
        {
            using (var rim = new SolidBrush(Radish))
                g.FillEllipse(rim, center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
            using var inside = new SolidBrush(Color.FromArgb(250, 244, 246));
            var r = radius * 0.78f;
            g.FillEllipse(inside, center.X - r, center.Y - r, 2 * r, 2 * r);
        }

        #endregion
    }

    internal static class RectangleFExtensions
    {
        /// <summary>A copy moved by dx/dy (RectangleF.Offset changes the value in place).</summary>
        public static RectangleF Offset2(this RectangleF r, float dx, float dy) => new(r.X + dx, r.Y + dy, r.Width, r.Height);
    }
}
