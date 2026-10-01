using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Yellow sticky note: paper gradient, adhesive strip, dog-ear corner, handwriting.</summary>
    public sealed class PostItTheme : FenceTheme
    {
        private static readonly Color Top = Color.FromArgb(255, 241, 140);
        private static readonly Color Bottom = Color.FromArgb(255, 228, 102);
        private static readonly Color Strip = Color.FromArgb(246, 222, 96);
        private static readonly Color Ink = Color.FromArgb(70, 56, 18);

        private static readonly string[] Handwriting = { "Ink Free", "Segoe Print", "Comic Sans MS" };

        public override string Id => "postit";

        public override string DisplayName => "Post-it";

        public override int CornerPreference => 1;

        public override int MinAlpha => 235;

        public override int ContentInset => 2;

        public override Color HintColor => Color.FromArgb(140, Ink);

        public override (Color Back, Color Fore) EditorColors => (Color.FromArgb(255, 247, 178), Ink);

        public override Font CreateTitleFont(int titleHeightPx) =>
            CreateFont(Handwriting, Math.Max(6, titleHeightPx * 0.46f), FontStyle.Bold, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(Handwriting, 13.5f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateNoteFont(float s) => CreateFont(Handwriting, 17f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var paper = new LinearGradientBrush(bounds, Color.FromArgb(alpha, Top), Color.FromArgb(alpha, Bottom), LinearGradientMode.Vertical))
                g.FillRectangle(paper, bounds);

            // Adhesive strip at the top
            using (var strip = new SolidBrush(Color.FromArgb(alpha, Strip)))
                g.FillRectangle(strip, bounds.X, bounds.Y, bounds.Width, titleHeight);

            // Dog-ear in the bottom right corner
            var ear = 18 * s;
            var r = bounds.Right;
            var b = bounds.Bottom;
            using (var shadow = new SolidBrush(Color.FromArgb(60, 120, 90, 0)))
                g.FillPolygon(shadow, new[] { new PointF(r - ear, b), new PointF(r, b - ear), new PointF(r - ear * 0.15f, b - ear * 0.15f) });
            using (var fold = new LinearGradientBrush(new RectangleF(r - ear, b - ear, ear, ear), Color.FromArgb(255, 250, 200), Color.FromArgb(230, 200, 80), LinearGradientMode.ForwardDiagonal))
                g.FillPolygon(fold, new[] { new PointF(r - ear, b), new PointF(r - ear, b - ear * 0.15f), new PointF(r - ear * 0.15f, b - ear) , new PointF(r, b - ear) });
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Ink, new RectangleF(titleRect.X + 10 * s, titleRect.Y, titleRect.Width - 20 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 50 : 28, 90, 70, 0), rect, 6 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawPlainString(g, text, font, Ink, rect, format);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(140, Ink), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, Ink, x, top, height, 2 * s);
    }
}
