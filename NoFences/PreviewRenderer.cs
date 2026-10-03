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
            public bool Animations => false;
            public bool FpsEnabled => true;
            public void ToggleFps() { }
            public FenceTheme ThemeFor(FenceInfo info) => ThemeRegistry.Get(info.Theme);
            public void RequestSave() { }
            public void CreateFence(FenceKind kind, string? name = null) { }
            public void RemoveFence(FenceWindow window) { }
            public void AddCreateExtrasItems(ToolStripItemCollection items) { }
            public Guid? CurrentVirtualDesktop => null;
            public void TogglePinToDesktop(FenceInfo info) { }
            public IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except) => Array.Empty<Rectangle>();
        }

        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);
            // Show what a user style from a JSON file looks like, too.
            ThemeRegistry.SetCustom(new[] { JsonTheme.Parse(JsonTheme.ExampleJson, "example") });
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

            RenderNotes(outDir, host);
            RenderDialog(AboutDialog.CreateForPreview(), Path.Combine(outDir, "about.png"));
            RenderWidgets(outDir, host);
            RenderExtras(outDir, host, samples);
        }

        /// <summary>A fence with tabs and a compact quick-launch bar.</summary>
        private static void RenderExtras(string outDir, IFenceHost host, List<string> samples)
        {
            using var sheet = new Bitmap(820, 340, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(sheet);
            DrawBackdrop(g, new Rectangle(Point.Empty, sheet.Size));

            var tabs = new FenceInfo
            {
                Name = "Arbeit", Theme = "default", BackgroundAlpha = 140, Files = samples.Take(4).ToList(),
                Tabs = { new FenceTab { Name = "Arbeit" }, new FenceTab { Name = "Spiele" }, new FenceTab { Name = "Tools" } }
            };
            using (var window = new FenceWindow(host, tabs) { Size = new Size(380, 290) })
            {
                window.ApplySettings();
                window.ReloadEntries();
                var state = g.Save();
                g.TranslateTransform(24, 24);
                window.PaintFence(g); // first pass requests icons
                g.Restore(state);
            }

            var quick = new FenceInfo { Name = "Schnellstart", Theme = "gaming", Compact = true, IconSize = 48, Files = samples };
            using (var window = new FenceWindow(host, quick) { Size = new Size(380, 120) })
            {
                window.ApplySettings();
                window.ReloadEntries();
                // Paint once to request the 48 px icons, then let them load.
                using (var scratch = new Bitmap(380, 120))
                using (var sg = Graphics.FromImage(scratch))
                    window.PaintFence(sg);
                var until = DateTime.Now.AddSeconds(2);
                while (DateTime.Now < until) { Application.DoEvents(); Thread.Sleep(20); }
                var state = g.Save();
                g.TranslateTransform(424, 24);
                window.PaintFence(g);
                g.Restore(state);
            }
            sheet.Save(Path.Combine(outDir, "extras.png"), ImageFormat.Png);
        }

        /// <summary>All widgets once, in fitting styles (with this PC's real data).</summary>
        private static void RenderWidgets(string outDir, IFenceHost host)
        {
            var items = new (string Type, string Theme, Size Size)[]
            {
                ("clock", "default", new Size(280, 320)),
                ("system", "gaming", new Size(260, 300)),
                ("drives", "hardware", new Size(300, 240)),
                ("recyclebin", "nerd", new Size(220, 210)),
                ("playtime", "starcitizen", new Size(270, 260)),
            };
            const int gap = 24;
            using var sheet = new Bitmap(items.Sum(i => i.Size.Width + gap) + gap, items.Max(i => i.Size.Height) + 2 * gap, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(sheet);
            DrawBackdrop(g, new Rectangle(Point.Empty, sheet.Size));
            var x = gap;
            foreach (var (type, theme, size) in items)
            {
                var name = Widgets.WidgetRegistry.Types.First(t => t.Type == type).Name();
                var info = new FenceInfo { Name = name, Kind = FenceKind.Widget, WidgetType = type, Theme = theme, BackgroundAlpha = 140 };
                using var window = new FenceWindow(host, info) { Size = size };
                window.ApplySettings();
                window.RefreshWidgetForPreview();
                var state = g.Save();
                g.TranslateTransform(x, gap);
                window.PaintFence(g);
                g.Restore(state);
                x += size.Width + gap;
            }
            sheet.Save(Path.Combine(outDir, "widgets.png"), ImageFormat.Png);
        }

        /// <summary>Draws a dialog offscreen (it is never shown).</summary>
        internal static void RenderDialog(Form form, string path)
        {
            using (form)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-32000, -32000);
                form.Show();
                Application.DoEvents();
                using var bmp = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(Point.Empty, form.Size));
                bmp.Save(path, ImageFormat.Png);
                form.Hide();
            }
        }

        /// <summary>Sample sticky notes in a few styles, including checkboxes.</summary>
        private static void RenderNotes(string outDir, IFenceHost host)
        {
            const string text = "Einkaufen:\n[x] Milch\n[ ] Brot\n[ ] Kaffee\n\nTel.\t0664 123 456\nWeb:\twww.robertsspaceindustries.com";
            var themes = new[] { "postit", "postit-pink", "postit-green", "postit-blue", "postit-orange", "nerd" };
            const int w = 260, h = 260, gap = 24, columns = 3;
            var rows = (themes.Length + columns - 1) / columns;
            using var sheet = new Bitmap(columns * (w + gap) + gap, rows * (h + gap) + gap, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(sheet);
            DrawBackdrop(g, new Rectangle(Point.Empty, sheet.Size));
            for (var i = 0; i < themes.Length; i++)
            {
                var info = new FenceInfo
                {
                    Name = "Notiz", Kind = FenceKind.Note, Theme = themes[i], NoteText = text, TitleHeight = 30, BackgroundAlpha = 120,
                    ReminderAt = i == 0 ? DateTime.Today.AddHours(18) : null
                };
                using var window = new FenceWindow(host, info) { Size = new Size(w, h) };
                window.ApplySettings();
                var state = g.Save();
                g.TranslateTransform(gap + (i % columns) * (w + gap), gap + (i / columns) * (h + gap));
                window.PaintFence(g);
                g.Restore(state);
            }
            sheet.Save(Path.Combine(outDir, "notes.png"), ImageFormat.Png);

            // Collapsed notes (only the title should remain, on paper)
            using var strip = new Bitmap(3 * (w + gap) + gap, 120, PixelFormat.Format32bppArgb);
            using var sg = Graphics.FromImage(strip);
            DrawBackdrop(sg, new Rectangle(Point.Empty, strip.Size));
            for (var i = 0; i < 3; i++)
            {
                var info = new FenceInfo { Name = "Einkaufsliste", Kind = FenceKind.Note, Theme = themes[i * 2], NoteText = text, TitleHeight = 30, CanMinify = true };
                using var window = new FenceWindow(host, info) { Size = new Size(w, h) };
                window.ApplySettings();
                window.CollapseForPreview();
                var state = sg.Save();
                sg.TranslateTransform(gap + i * (w + gap), gap);
                window.PaintFence(sg);
                sg.Restore(state);
            }
            strip.Save(Path.Combine(outDir, "notes-collapsed.png"), ImageFormat.Png);
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
