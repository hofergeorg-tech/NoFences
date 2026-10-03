using System.Drawing.Drawing2D;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>Manila folder: folder tab as title, stacked paper edges, ink-brown text.</summary>
    public sealed class DocumentsTheme : FenceTheme
    {
        private static readonly Color Manila = Color.FromArgb(240, 222, 170);
        private static readonly Color TabColor = Color.FromArgb(226, 200, 138);
        private static readonly Color Ink = Color.FromArgb(78, 56, 24);

        public override string Id => "documents";
        public override string DisplayName => "Dokumente";
        public override Color Accent => Color.FromArgb(196, 140, 50);
        public override int MinAlpha => 225;
        public override int CornerPreference => 3;
        public override int ContentInset => 3;
        public override Color HintColor => Color.FromArgb(150, Ink);
        public override (Color Back, Color Fore) EditorColors => (Color.FromArgb(250, 238, 200), Ink);

        public override Font CreateTitleFont(int titleHeightPx) => new("Segoe UI Semibold", Math.Max(6, titleHeightPx * 0.42f), FontStyle.Regular, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new SolidBrush(Color.FromArgb(alpha, Manila)))
                g.FillRectangle(bg, bounds.X, bounds.Y + titleHeight * 0.35f, bounds.Width, bounds.Height - titleHeight * 0.35f);
            // Folder tab on the left
            var tabWidth = Math.Min(bounds.Width * 0.55f, 220 * s);
            using (var tab = new SolidBrush(Color.FromArgb(alpha, TabColor)))
                g.FillPolygon(tab, new[]
                {
                    new PointF(bounds.X, bounds.Y + titleHeight), new PointF(bounds.X, bounds.Y + 4 * s),
                    new PointF(bounds.X + 4 * s, bounds.Y), new PointF(bounds.X + tabWidth - 14 * s, bounds.Y),
                    new PointF(bounds.X + tabWidth, bounds.Y + titleHeight * 0.35f), new PointF(bounds.Right, bounds.Y + titleHeight * 0.35f),
                    new PointF(bounds.Right, bounds.Y + titleHeight)
                });
            // Stacked paper edges at the bottom
            using var edge = new Pen(Color.FromArgb(110, Ink), 1);
            for (var i = 1; i <= 3; i++)
                g.DrawLine(edge, bounds.X + i * 3 * s, bounds.Bottom - i * 3 * s, bounds.Right - i * 3 * s, bounds.Bottom - i * 3 * s);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Ink, new RectangleF(titleRect.X + 12 * s, titleRect.Y, Math.Min(titleRect.Width * 0.55f, 220 * s) - 26 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (hover || selected)
                FillRounded(g, Color.FromArgb(selected ? 70 : 40, Accent), rect, 4 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) => DrawPlainString(g, text, font, Ink, rect, format);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(150, Ink), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Ink, x, top, height, 2 * s);
    }

    /// <summary>Cinema: dark screen between film-strip perforations, red play button.</summary>
    public sealed class MultimediaTheme : FenceTheme
    {
        private static readonly Color Screen = Color.FromArgb(16, 16, 20);
        private static readonly Color Red = Color.FromArgb(229, 20, 30);

        public override string Id => "multimedia";
        public override string DisplayName => "Multimedia";
        public override Color Accent => Red;
        public override int MinAlpha => 210;
        public override int CornerPreference => 1;
        public override int ContentInset => 14;

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(new[] { "Bahnschrift SemiBold", "Segoe UI Semibold" }, Math.Max(6, titleHeightPx * 0.44f), FontStyle.Regular, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);
        public override string FormatTitle(string title) => title.ToUpperInvariant();

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Screen)))
                g.FillRectangle(bg, bounds);
            // Film strip edges with sprocket holes
            var strip = 12 * s;
            using var holes = new SolidBrush(Color.FromArgb(200, 70, 70, 76));
            for (var y = bounds.Y + titleHeight + 6 * s; y < bounds.Bottom - 8 * s; y += 14 * s)
            {
                g.FillRectangle(holes, bounds.X + 3 * s, y, strip - 6 * s, 8 * s);
                g.FillRectangle(holes, bounds.Right - strip + 3 * s, y, strip - 6 * s, 8 * s);
            }
            using var line = new Pen(Color.FromArgb(160, Red), 2 * s);
            g.DrawLine(line, bounds.X, bounds.Y + titleHeight - 1, bounds.Right, bounds.Y + titleHeight - 1);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            // Play button
            var r = titleRect.Height * 0.3f;
            var cx = titleRect.X + 14 * s + r;
            var cy = titleRect.Y + titleRect.Height / 2f;
            using (var red = new SolidBrush(Red))
                g.FillEllipse(red, cx - r, cy - r, 2 * r, 2 * r);
            g.FillPolygon(Brushes.White, new[] { new PointF(cx - r * 0.35f, cy - r * 0.5f), new PointF(cx - r * 0.35f, cy + r * 0.5f), new PointF(cx + r * 0.55f, cy) });
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Color.White, new RectangleF(cx + r + 8 * s, titleRect.Y, titleRect.Right - cx - r - 16 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 70 : 40, Color.White), rect, 4 * s);
            if (selected)
                DrawRounded(g, Red, 1.5f * s, rect, 4 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(200, 0, 0, 0), rect, format, s);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(200, Red), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Red, x, top, height, 2 * s);
    }

    /// <summary>Music: deep purple night, a vinyl record peeking out, equalizer in the title (animated on hover).</summary>
    public sealed class MusicTheme : FenceTheme
    {
        private static readonly Color Top = Color.FromArgb(40, 16, 60);
        private static readonly Color Bottom = Color.FromArgb(12, 8, 24);
        private static readonly Color Green = Color.FromArgb(30, 215, 96);

        public override string Id => "music";
        public override string DisplayName => "Musik";
        public override Color Accent => Green;
        public override int MinAlpha => 200;
        public override bool AnimatesOnHover => true;

        public override Font CreateTitleFont(int titleHeightPx) => new("Segoe UI Semibold", Math.Max(6, titleHeightPx * 0.44f), FontStyle.Regular, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new LinearGradientBrush(bounds, Color.FromArgb(alpha, Top), Color.FromArgb(alpha, Bottom), LinearGradientMode.Vertical))
                g.FillRectangle(bg, bounds);

            // Vinyl in the bottom right corner
            var r = Math.Min(bounds.Width, bounds.Height) * 0.42f;
            var c = new PointF(bounds.Right - r * 0.35f, bounds.Bottom - r * 0.35f);
            using (var disc = new SolidBrush(Color.FromArgb(120, 8, 8, 10)))
                g.FillEllipse(disc, c.X - r, c.Y - r, 2 * r, 2 * r);
            using (var groove = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
            {
                for (var k = 0.45f; k < 1f; k += 0.08f)
                    g.DrawEllipse(groove, c.X - r * k, c.Y - r * k, 2 * r * k, 2 * r * k);
            }
            using (var label = new SolidBrush(Color.FromArgb(150, Green)))
                g.FillEllipse(label, c.X - r * 0.28f, c.Y - r * 0.28f, r * 0.56f, r * 0.56f);

            DrawEqualizer(g, bounds, titleHeight, s, 0);
        }

        private static void DrawEqualizer(Graphics g, Rectangle bounds, int titleHeight, float s, float t)
        {
            using var bar = new SolidBrush(Green);
            var heights = new[] { 0.5f, 0.9f, 0.6f, 0.8f, 0.4f };
            for (var i = 0; i < heights.Length; i++)
            {
                var h = t == 0 ? heights[i] : 0.25f + 0.75f * Math.Abs(MathF.Sin(t * (3 + i) + i));
                var height = (titleHeight - 14 * s) * h;
                g.FillRectangle(bar, bounds.Right - (16 + (heights.Length - i) * 6) * s, bounds.Y + titleHeight - 7 * s - height, 4 * s, height);
            }
        }

        public override void DrawHoverEffect(Graphics g, Rectangle bounds, int titleHeight, float t, float s)
        {
            // Redraw the equalizer bars dancing over the static ones
            using (var cover = new SolidBrush(Top))
                g.FillRectangle(cover, bounds.Right - 52 * s, bounds.Y + 4 * s, 40 * s, titleHeight - 8 * s);
            DrawEqualizer(g, bounds, titleHeight, s, t + 0.01f);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, "♪ " + text, font, Color.White, new RectangleF(titleRect.X + 12 * s, titleRect.Y, titleRect.Width - 72 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 80 : 45, Green), rect, 8 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(200, 0, 0, 0), rect, format, s);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(200, Green), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Green, x, top, height, 2 * s);
    }

    /// <summary>Sports: striped turf with field lines, scoreboard title with LED digits.</summary>
    public sealed class SportTheme : FenceTheme
    {
        private static readonly Color Turf = Color.FromArgb(46, 125, 50);
        private static readonly Color TurfLight = Color.FromArgb(56, 142, 60);
        private static readonly Color Led = Color.FromArgb(255, 196, 0);

        public override string Id => "sport";
        public override string DisplayName => "Sport";
        public override Color Accent => Led;
        public override int MinAlpha => 215;
        public override int CornerPreference => 1;

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(new[] { "Consolas" }, Math.Max(6, titleHeightPx * 0.46f), FontStyle.Bold, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI Semibold", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);
        public override string FormatTitle(string title) => title.ToUpperInvariant();

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            // Mowing stripes
            var stripe = 26 * s;
            for (var x = (float)bounds.X; x < bounds.Right; x += stripe)
            {
                using var b = new SolidBrush(Color.FromArgb(alpha, ((int)((x - bounds.X) / stripe) % 2 == 0) ? Turf : TurfLight));
                g.FillRectangle(b, x, bounds.Y, stripe, bounds.Height);
            }
            // Field lines: outline, halfway line and centre circle
            using var line = new Pen(Color.FromArgb(150, Color.White), 2 * s);
            var field = RectangleF.Inflate(new RectangleF(bounds.X, bounds.Y + titleHeight, bounds.Width, bounds.Height - titleHeight), -8 * s, -8 * s);
            g.DrawRectangle(line, field.X, field.Y, field.Width, field.Height);
            g.DrawLine(line, field.X + field.Width / 2, field.Y, field.X + field.Width / 2, field.Bottom);
            var r = Math.Min(field.Width, field.Height) * 0.18f;
            g.DrawEllipse(line, field.X + field.Width / 2 - r, field.Y + field.Height / 2 - r, 2 * r, 2 * r);
            // Scoreboard
            using var board = new SolidBrush(Color.FromArgb(240, 18, 18, 18));
            g.FillRectangle(board, bounds.X, bounds.Y, bounds.Width, titleHeight);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat(StringAlignment.Center);
            DrawShadowedString(g, text, font, Led, Color.FromArgb(90, Led), titleRect, format, 0);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 110 : 70, 0, 0, 0), rect, 6 * s);
            if (selected)
                DrawRounded(g, Led, 1.5f * s, rect, 6 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(220, 0, 40, 0), rect, format, s);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(220, Color.White), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Led, x, top, height, 2.5f * s);
    }

    /// <summary>Photos: instant-photo white with a handwritten caption and soft vignette.</summary>
    public sealed class PhotosTheme : FenceTheme
    {
        private static readonly Color Paper = Color.FromArgb(250, 250, 246);
        private static readonly Color Ink = Color.FromArgb(50, 50, 60);

        public override string Id => "photos";
        public override string DisplayName => "Fotos";
        public override Color Accent => Color.FromArgb(255, 140, 0);
        public override int MinAlpha => 230;
        public override int CornerPreference => 1;
        public override int ContentInset => 6;
        public override Color HintColor => Color.FromArgb(150, Ink);
        public override (Color Back, Color Fore) EditorColors => (Paper, Ink);

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(new[] { "Segoe Print", "Ink Free" }, Math.Max(6, titleHeightPx * 0.4f), FontStyle.Bold, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Paper)))
                g.FillRectangle(bg, bounds);
            // The "picture" area with a darker inner frame, like an instant photo
            var photo = new RectangleF(bounds.X + 8 * s, bounds.Y + titleHeight, bounds.Width - 16 * s, bounds.Height - titleHeight - 8 * s);
            using (var inner = new LinearGradientBrush(photo, Color.FromArgb(40, 120, 140, 170), Color.FromArgb(25, 200, 160, 120), LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(inner, photo);
            using var pen = new Pen(Color.FromArgb(60, Ink), 1);
            g.DrawRectangle(pen, photo.X, photo.Y, photo.Width, photo.Height);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat(StringAlignment.Center);
            DrawPlainString(g, text, font, Ink, titleRect, format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            // A small white photo frame around the item
            using (var shadow = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                g.FillRectangle(shadow, rect.X + 2 * s, rect.Y + 2 * s, rect.Width, rect.Height);
            using (var frame = new SolidBrush(Color.FromArgb(selected ? 255 : 220, Color.White)))
                g.FillRectangle(frame, rect);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) => DrawPlainString(g, text, font, Ink, rect, format);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(150, Ink), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Accent, x, top, height, 2 * s);
    }

    /// <summary>Travel: airmail postcard with striped border, stamp and postmark.</summary>
    public sealed class TravelTheme : FenceTheme
    {
        private static readonly Color Card = Color.FromArgb(250, 244, 228);
        private static readonly Color Red = Color.FromArgb(200, 40, 46);
        private static readonly Color Blue = Color.FromArgb(30, 70, 160);
        private static readonly Color Ink = Color.FromArgb(40, 50, 90);

        public override string Id => "travel";
        public override string DisplayName => "Reisen";
        public override Color Accent => Blue;
        public override int MinAlpha => 230;
        public override int CornerPreference => 1;
        public override int ContentInset => 8;
        public override Color HintColor => Color.FromArgb(150, Ink);
        public override (Color Back, Color Fore) EditorColors => (Card, Ink);

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(new[] { "Segoe Print", "Ink Free" }, Math.Max(6, titleHeightPx * 0.4f), FontStyle.Bold, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new SolidBrush(Color.FromArgb(alpha, Card)))
                g.FillRectangle(bg, bounds);

            // Airmail border: diagonal red/blue stripes
            var w = 5 * s;
            var state = g.Save();
            using (var clip = new Region(bounds))
            {
                clip.Exclude(RectangleF.Inflate(bounds, -w, -w));
                g.SetClip(clip, CombineMode.Intersect);
                var i = 0;
                for (float x = bounds.X - bounds.Height; x < bounds.Right; x += 10 * s, i++)
                {
                    using var b = new SolidBrush(i % 2 == 0 ? Red : Blue);
                    g.FillPolygon(b, new[] { new PointF(x, bounds.Bottom), new PointF(x + 5 * s, bounds.Bottom), new PointF(x + 5 * s + bounds.Height, bounds.Y), new PointF(x + bounds.Height, bounds.Y) });
                }
            }
            g.Restore(state);

            // Stamp with perforated edge and a postmark
            var stamp = new RectangleF(bounds.Right - 40 * s, bounds.Y + 9 * s, 26 * s, Math.Max(10, titleHeight - 12 * s));
            using (var paper = new SolidBrush(Color.White))
                g.FillRectangle(paper, stamp);
            using (var picture = new SolidBrush(Color.FromArgb(180, Blue)))
                g.FillRectangle(picture, RectangleF.Inflate(stamp, -4 * s, -4 * s));
            using (var mark = new Pen(Color.FromArgb(110, 30, 30, 30), 1.2f * s))
            {
                g.DrawEllipse(mark, stamp.X - 22 * s, stamp.Y + 2 * s, 20 * s, 20 * s);
                for (var k = 0; k < 3; k++)
                    g.DrawLine(mark, stamp.X - 26 * s, stamp.Y + (8 + k * 5) * s, stamp.X + stamp.Width * 0.6f, stamp.Y + (8 + k * 5) * s);
            }
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawPlainString(g, text, font, Ink, new RectangleF(titleRect.X + 14 * s, titleRect.Y + 2 * s, titleRect.Width - 90 * s, titleRect.Height), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (hover || selected)
                FillRounded(g, Color.FromArgb(selected ? 60 : 32, Blue), rect, 4 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) => DrawPlainString(g, text, font, Ink, rect, format);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(160, Blue), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Red, x, top, height, 2 * s);
    }

    /// <summary>Cooking: recipe card with a gingham title band and ruled lines.</summary>
    public sealed class CookingTheme : FenceTheme
    {
        private static readonly Color Card = Color.FromArgb(255, 252, 244);
        private static readonly Color Red = Color.FromArgb(206, 52, 52);
        private static readonly Color Ink = Color.FromArgb(70, 40, 30);

        public override string Id => "cooking";
        public override string DisplayName => "Kochen";
        public override Color Accent => Red;
        public override int MinAlpha => 230;
        public override int CornerPreference => 3;
        public override Color HintColor => Color.FromArgb(150, Ink);
        public override (Color Back, Color Fore) EditorColors => (Card, Ink);

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(new[] { "Segoe Print", "Ink Free" }, Math.Max(6, titleHeightPx * 0.4f), FontStyle.Bold, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            using (var bg = new SolidBrush(Color.FromArgb(Alpha(info), Card)))
                g.FillRectangle(bg, bounds);
            // Gingham: two layers of translucent red stripes
            var cell = 8 * s;
            using (var stripe = new SolidBrush(Color.FromArgb(110, Red)))
            {
                for (var x = (float)bounds.X; x < bounds.Right; x += 2 * cell)
                    g.FillRectangle(stripe, x, bounds.Y, cell, titleHeight);
                for (var y = (float)bounds.Y; y < bounds.Y + titleHeight; y += 2 * cell)
                    g.FillRectangle(stripe, bounds.X, y, bounds.Width, Math.Min(cell, bounds.Y + titleHeight - y));
            }
            // Ruled lines and a margin line like an index card
            using var rule = new Pen(Color.FromArgb(70, 80, 130, 200), 1);
            for (var y = bounds.Y + titleHeight + 22 * s; y < bounds.Bottom; y += 22 * s)
                g.DrawLine(rule, bounds.X, y, bounds.Right, y);
            using var margin = new Pen(Color.FromArgb(90, Red), 1);
            g.DrawLine(margin, bounds.X + 22 * s, bounds.Y + titleHeight, bounds.X + 22 * s, bounds.Bottom);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            // White label in the middle of the gingham
            using var format = TitleFormat(StringAlignment.Center);
            var size = g.MeasureString(text, font);
            var w = Math.Min(titleRect.Width - 20 * s, size.Width + 24 * s);
            var label = new RectangleF(titleRect.X + (titleRect.Width - w) / 2, titleRect.Y + 4 * s, w, titleRect.Height - 8 * s);
            FillRounded(g, Color.FromArgb(235, Card), label, 4 * s);
            DrawPlainString(g, text, font, Red, label, format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (hover || selected)
                FillRounded(g, Color.FromArgb(selected ? 55 : 30, Red), rect, 6 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) => DrawPlainString(g, text, font, Ink, rect, format);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(170, Red), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Red, x, top, height, 2 * s);
    }

    /// <summary>Nature: forest green with scattered leaves.</summary>
    public sealed class NatureTheme : FenceTheme
    {
        private static readonly Color Top = Color.FromArgb(34, 70, 40);
        private static readonly Color Bottom = Color.FromArgb(18, 40, 24);
        private static readonly Color Leaf = Color.FromArgb(120, 200, 80);

        public override string Id => "nature";
        public override string DisplayName => "Natur";
        public override Color Accent => Leaf;
        public override int MinAlpha => 200;

        public override Font CreateTitleFont(int titleHeightPx) => new("Segoe UI Semibold", Math.Max(6, titleHeightPx * 0.44f), FontStyle.Regular, GraphicsUnit.Pixel);
        public override Font CreateLabelFont(float s) => new("Segoe UI", 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Alpha(info);
            using (var bg = new LinearGradientBrush(bounds, Color.FromArgb(alpha, Top), Color.FromArgb(alpha, Bottom), LinearGradientMode.Vertical))
                g.FillRectangle(bg, bounds);

            var rng = Seeded(info);
            var count = Math.Clamp(bounds.Width * bounds.Height / (int)(4000 * s * s), 4, 30);
            for (var i = 0; i < count; i++)
                DrawLeaf(g, new PointF(bounds.X + rng.Next(bounds.Width), bounds.Y + titleHeight + rng.Next(Math.Max(1, bounds.Height - titleHeight))),
                    (6 + rng.Next(10)) * s, rng.Next(360), Color.FromArgb(40 + rng.Next(40), Leaf));
            DrawLeaf(g, new PointF(bounds.X + 18 * s, bounds.Y + titleHeight / 2f), 9 * s, -35, Leaf);
        }

        private static void DrawLeaf(Graphics g, PointF center, float size, float angle, Color color)
        {
            var state = g.Save();
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(angle);
            using var path = new GraphicsPath();
            path.AddBezier(-size, 0, -size * 0.3f, -size * 0.7f, size * 0.3f, -size * 0.7f, size, 0);
            path.AddBezier(size, 0, size * 0.3f, size * 0.7f, -size * 0.3f, size * 0.7f, -size, 0);
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
            using (var vein = new Pen(Color.FromArgb(color.A, 20, 50, 20), 1))
                g.DrawLine(vein, -size * 0.8f, 0, size * 0.8f, 0);
            g.Restore(state);
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            using var format = TitleFormat();
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(150, 0, 0, 0), new RectangleF(titleRect.X + 34 * s, titleRect.Y, titleRect.Width - 44 * s, titleRect.Height), format, s);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (hover || selected)
                FillRounded(g, Color.FromArgb(selected ? 80 : 45, Leaf), rect, 10 * s);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, Color.White, Color.FromArgb(200, 0, 20, 0), rect, format, s);
        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) => DrawPillThumb(g, track, thumb, Color.FromArgb(200, Leaf), s);
        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) => DrawBarMarker(g, Leaf, x, top, height, 2 * s);
    }
}
