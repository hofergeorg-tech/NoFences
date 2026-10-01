using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>
    /// A sticky note taped onto the desktop: no glass, the window is clear around a sheet of paper
    /// with its own soft shadow, slightly lifted bottom corners, two strips of translucent tape and
    /// handwriting.
    /// </summary>
    public sealed class PostItTheme : FenceTheme
    {
        private static readonly Color Top = Color.FromArgb(255, 243, 150);
        private static readonly Color Bottom = Color.FromArgb(255, 229, 104);
        private static readonly Color Ink = Color.FromArgb(70, 56, 18);
        private static readonly Color TapeColor = Color.FromArgb(120, 255, 253, 246);

        private static readonly string[] Handwriting = { "Ink Free", "Segoe Print", "Comic Sans MS" };

        /// <summary>Clear margin around the paper (logical px) for tape and shadow.</summary>
        private const float Margin = 12;
        private const float TopMargin = 15;
        private const float BottomMargin = 16;

        public override string Id => "postit";

        public override string DisplayName => "Post-it";

        public override bool Glass => false;

        public override bool WindowShadow => false;

        public override int CornerPreference => 1;

        public override int ContentInset => 13;

        // Paper ends above the clear shadow margin.
        public override int BottomInset => (int)BottomMargin + 4;

        public override Color HintColor => Color.FromArgb(140, Ink);

        public override (Color Back, Color Fore) EditorColors => (Color.FromArgb(255, 240, 138), Ink);

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(Handwriting, Math.Max(6, titleHeightPx * 0.46f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(Handwriting, 13.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateNoteFont(float s) => CreateFont(Handwriting, 17f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        private static RectangleF Paper(Rectangle bounds, float s)
        {
            var m = Margin * s;
            var top = TopMargin * s;
            return new RectangleF(bounds.X + m, bounds.Y + top, Math.Max(1, bounds.Width - 2 * m), Math.Max(1, bounds.Height - top - BottomMargin * s));
        }

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var paper = Paper(bounds, s);

            DrawShadow(g, paper, s);

            // The paper itself, slightly darker towards the bottom where it lifts off
            using (var fill = new LinearGradientBrush(paper, Top, Bottom, LinearGradientMode.Vertical))
                g.FillRectangle(fill, paper);
            var lift = new RectangleF(paper.X, paper.Bottom - 16 * s, paper.Width, 16 * s);
            using (var curl = new LinearGradientBrush(lift, Color.FromArgb(0, 120, 90, 0), Color.FromArgb(30, 120, 90, 0), LinearGradientMode.Vertical))
                g.FillRectangle(curl, lift);
            // A faint highlight along the top edge
            using (var shine = new LinearGradientBrush(new RectangleF(paper.X, paper.Y, paper.Width, 10 * s), Color.FromArgb(70, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical))
                g.FillRectangle(shine, paper.X, paper.Y, paper.Width, 10 * s);

            DrawCurledCorner(g, paper, s);

            // Tape over both top corners
            DrawTape(g, new PointF(paper.Left + 7 * s, paper.Top + 3 * s), -40, s, info, 0);
            DrawTape(g, new PointF(paper.Right - 7 * s, paper.Top + 3 * s), 40, s, info, 1);
        }

        /// <summary>Soft layered shadow, stronger under the lifted bottom corners.</summary>
        private static void DrawShadow(Graphics g, RectangleF paper, float s)
        {
            for (var i = 6; i >= 1; i--)
            {
                var r = RectangleF.Inflate(paper, i * 0.9f * s, i * 0.9f * s);
                r.Offset(0, 1.5f * s);
                FillRounded(g, Color.FromArgb(11, 30, 22, 0), r, (2 + i) * s);
            }

            var w = paper.Width * 0.3f;
            // Centered a bit inside the corners, so the shadow only peeks out below the paper.
            foreach (var x in new[] { paper.Left + w * 0.4f, paper.Right - w * 0.4f })
            {
                var shadowRect = new RectangleF(x - w / 2, paper.Bottom - 4 * s, w, 10 * s);
                using var path = new GraphicsPath();
                path.AddEllipse(shadowRect);
                using var brush = new PathGradientBrush(path)
                {
                    CenterColor = Color.FromArgb(75, 30, 22, 0),
                    SurroundColors = new[] { Color.FromArgb(0, 40, 30, 0) }
                };
                g.FillPath(brush, path);
            }
        }

        private static void DrawCurledCorner(Graphics g, RectangleF paper, float s)
        {
            var c = 16 * s;
            var r = paper.Right;
            var b = paper.Bottom;
            // Cut the corner away, then draw the folded-up flap
            using (var cut = new LinearGradientBrush(new RectangleF(r - c, b - c, c, c), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(40, 60, 45, 0), LinearGradientMode.ForwardDiagonal))
                g.FillPolygon(cut, new[] { new PointF(r - c, b), new PointF(r, b - c), new PointF(r, b) });
            using var flap = new LinearGradientBrush(new RectangleF(r - c, b - c, c, c), Color.FromArgb(255, 252, 214), Color.FromArgb(232, 206, 92), LinearGradientMode.BackwardDiagonal);
            g.FillPolygon(flap, new[] { new PointF(r - c, b), new PointF(r, b - c), new PointF(r - c * 0.85f, b - c * 0.85f) });
        }

        /// <summary>A strip of translucent tape with torn, zigzag ends.</summary>
        private static void DrawTape(Graphics g, PointF center, float angle, float s, FenceInfo info, int salt)
        {
            var rng = new Random(info.Id.GetHashCode() + salt);
            var w = (36 + rng.Next(6)) * s;
            var h = 12 * s;
            var state = g.Save();
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(angle + (float)(rng.NextDouble() * 6 - 3));

            var teeth = 4;
            var points = new List<PointF>();
            points.Add(new PointF(-w / 2, -h / 2));
            points.Add(new PointF(w / 2, -h / 2));
            for (var i = 1; i <= teeth; i++)
                points.Add(new PointF(w / 2 + (i % 2 == 1 ? 2.5f * s : 0), -h / 2 + h * i / teeth));
            points.Add(new PointF(-w / 2, h / 2));
            for (var i = teeth - 1; i >= 1; i--)
                points.Add(new PointF(-w / 2 - (i % 2 == 1 ? 2.5f * s : 0), -h / 2 + h * i / teeth));

            using (var shadow = new SolidBrush(Color.FromArgb(28, 0, 0, 0)))
            {
                var shadowPoints = points.Select(p => new PointF(p.X + 0.8f * s, p.Y + 1.2f * s)).ToArray();
                g.FillPolygon(shadow, shadowPoints);
            }
            using (var tape = new SolidBrush(TapeColor))
                g.FillPolygon(tape, points.ToArray());
            using (var edge = new Pen(Color.FromArgb(60, Color.White), 1))
                g.DrawLine(edge, -w / 2, -h / 2 + 1, w / 2, -h / 2 + 1);

            g.Restore(state);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var m = Margin * s;
            using var format = TitleFormat(StringAlignment.Center);
            var rect = new RectangleF(titleRect.X + m + 30 * s, titleRect.Y + m, titleRect.Width - 2 * m - 60 * s, titleRect.Height - m + 4 * s);
            DrawPlainString(g, text, font, Ink, rect, format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 50 : 28, 90, 70, 0), rect, 6 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawPlainString(g, text, font, Ink, rect, format);

        // Keep the scrollbar on the paper, not in the clear margin.
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s)
        {
            var shift = (int)(Margin * s);
            track.Offset(-shift, 0);
            thumb.Offset(-shift, 0);
            DrawPillThumb(g, track, thumb, Color.FromArgb(140, Ink), s);
        }

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Ink, x, top, height, 2 * s);
    }
}
