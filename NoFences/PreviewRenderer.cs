using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// <c>NoFences.exe --preview &lt;dir&gt;</c>: renders every style with sample items into PNGs
    /// (one per style plus an overview), without touching the config or the desktop.
    /// Handy for checking styles and for README screenshots.
    /// </summary>
    internal static class PreviewRenderer
    {
        private sealed class Host : IFenceHost
        {
            public bool ShowExtensions => false;
            public FenceTheme ThemeFor(FenceInfo info) => ThemeRegistry.Get(info.Theme);
            public void RequestSave() { }
            public void CreateFence(FenceKind kind, string? name = null) { }
            public void RemoveFence(FenceWindow window) { }
        }

        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var samples = CreateSampleFiles(Path.Combine(Path.GetTempPath(), "NoFencesPreview"));
            var host = new Host();
            const int w = 340, h = 270, gap = 24, columns = 4;

            var windows = ThemeRegistry.All.Select(t =>
            {
                var info = new FenceInfo { Name = t.DisplayName, Theme = t.Id, Files = samples, BackgroundAlpha = 120 };
                var window = new FenceWindow(host, info) { Size = new Size(w, h) };
                window.ApplySettings();
                window.ReloadEntries();
                return (theme: t, window);
            }).ToList();

            // Icons load asynchronously: paint once to request them, then pump messages until they arrive.
            using (var scratch = new Bitmap(w, h))
            using (var sg = Graphics.FromImage(scratch))
            {
                foreach (var (_, window) in windows)
                    window.PaintFence(sg);
            }
            var until = DateTime.Now.AddSeconds(4);
            while (DateTime.Now < until)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }

            var rows = (windows.Count + columns - 1) / columns;
            using var overview = new Bitmap(columns * (w + gap) + gap, rows * (h + gap) + gap, PixelFormat.Format32bppArgb);
            using (var og = Graphics.FromImage(overview))
                DrawBackdrop(og, new Rectangle(Point.Empty, overview.Size));

            for (var i = 0; i < windows.Count; i++)
            {
                var (theme, window) = windows[i];
                using var single = new Bitmap(w + 2 * gap, h + 2 * gap, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(single))
                {
                    DrawBackdrop(g, new Rectangle(Point.Empty, single.Size));
                    g.TranslateTransform(gap, gap);
                    window.PaintFence(g);
                }
                single.Save(Path.Combine(outDir, $"style-{theme.Id}.png"), ImageFormat.Png);

                using (var og = Graphics.FromImage(overview))
                {
                    og.TranslateTransform(gap + (i % columns) * (w + gap), gap + (i / columns) * (h + gap));
                    window.PaintFence(og);
                }
                window.Dispose();
            }
            overview.Save(Path.Combine(outDir, "styles.png"), ImageFormat.Png);
        }

        /// <summary>A neutral "wallpaper" that is neither too dark nor too light, so every style shows.</summary>
        private static void DrawBackdrop(Graphics g, Rectangle r)
        {
            using var brush = new LinearGradientBrush(r, Color.FromArgb(58, 84, 120), Color.FromArgb(150, 130, 120), LinearGradientMode.ForwardDiagonal);
            g.FillRectangle(brush, r);
        }

        private static List<string> CreateSampleFiles(string dir)
        {
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "Projekte"));
            var names = new[] { "Notizen.txt", "Rechnung.pdf", "Tabelle.xlsx", "Präsentation.pptx", "Urlaub.png" };
            foreach (var name in names)
            {
                var path = Path.Combine(dir, name);
                if (File.Exists(path))
                    continue;
                if (name.EndsWith(".png"))
                {
                    using var bmp = new Bitmap(160, 120);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        using var sky = new LinearGradientBrush(new Rectangle(0, 0, 160, 120), Color.SkyBlue, Color.LightYellow, LinearGradientMode.Vertical);
                        g.FillRectangle(sky, 0, 0, 160, 120);
                        g.FillEllipse(Brushes.Orange, 100, 15, 35, 35);
                        g.FillPolygon(Brushes.SeaGreen, new[] { new Point(0, 120), new Point(60, 50), new Point(120, 120) });
                    }
                    bmp.Save(path, ImageFormat.Png);
                }
                else
                {
                    File.WriteAllText(path, "NoFences preview");
                }
            }
            var files = names.Select(n => Path.Combine(dir, n)).ToList();
            files.Insert(3, Path.Combine(dir, "Projekte"));
            files.Add(Path.Combine(Environment.SystemDirectory, "calc.exe"));
            return files;
        }
    }
}
