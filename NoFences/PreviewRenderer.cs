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
        internal sealed class Host : IFenceHost
        {
            public bool ShowExtensions => false;
            public bool Animations => false;
            public UsageLog ScreenTime { get; } = DemoUsage();
            /// <summary>Style designer: draws with a style that isn't registered (yet).</summary>
            public Func<FenceInfo, FenceTheme>? ThemeOverride { get; init; }
            public FenceTheme ThemeFor(FenceInfo info) => ThemeOverride?.Invoke(info) ?? ThemeRegistry.Get(info.Theme);
            public void RequestSave() { }
            public void Notify(string text) { }
            public void StopAlarmSound() { }
            public void CreateFence(FenceKind kind, string? name = null) { }
            public void RemoveFence(FenceWindow window) { }
            public void AddCreateExtrasItems(ToolStripItemCollection items) { }
            public Guid? CurrentVirtualDesktop => null;
            public void TogglePinToDesktop(FenceInfo info) { }
            public void AddAppSettingsItems(ToolStripItemCollection items) { }
            public void AddToolItems(ToolStripItemCollection items) { }
            public IReadOnlyList<string> Profiles => Array.Empty<string>();
            public string? ActiveProfile => null;
            public void SwitchProfile(string? profile, bool automatic = false) { }
            public void AddFenceProfileItems(ToolStripItemCollection items, FenceInfo info, IWin32Window owner) { }
            public IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except) => Array.Empty<Rectangle>();
            public void RecordUndo(string description, IEnumerable<Guid> fences, Action? reverse = null) { }
            public void RecordUndo(string description, IReadOnlyList<string> snapshots) { }
            public void Undo() { }
            public void SearchFor(string text) { }
            public void SaveNoteTemplate(string name, string text) { }
            public void RaiseAboveOtherFences(FenceWindow window) { }
            public void DockMemberChanged(FenceWindow window, Rectangle before) { }
            public void Offer(string text, Action onClick) { }
            public void AddUndoItem(ToolStripItemCollection items) { }
            public string? UndoDescription => null;
            public IReadOnlyList<FenceWindow> GroupMembers(FenceWindow window) => Array.Empty<FenceWindow>();
            public void AddGroupItems(ToolStripItemCollection items, FenceWindow window) { }
            public void FenceSettingsChanged(FenceInfo info) { }
            public bool HoverPreview => false;
            public void OpenStyleDesigner(FenceInfo? info) { }
        }


        /// <summary>Made-up screen time (programs that don't exist here, so no real icons or data show).</summary>
        private static UsageLog DemoUsage()
        {
            var log = new UsageLog();
            foreach (var (exe, name, minutes) in new[] { (@"C:\Demo\browser.exe", "Web Browser", 142), (@"C:\Demo\code.exe", "Code Editor", 96),
                         (@"C:\Demo\game.exe", "Space Game", 75), (@"C:\Demo\mail.exe", "Mail", 31), (@"C:\Demo\music.exe", "Music Player", 18) })
                log.Add(DateTime.Now, exe, name, minutes * 60);
            return log;
        }

        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);
            Widgets.FenceWidget.PreviewMode = true;
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
            RenderDialog(RulerWindow.CreateForPreview(), Path.Combine(outDir, "ruler.png"));
            RenderDialog(new FenceSettingsDialog(new FenceInfo { Name = "Spiele", Theme = "gaming", Files = samples, Width = 340, Height = 260, AutoSortPatterns = "*.lnk" }),
                Path.Combine(outDir, "settings.png"));
            RenderDialog(new StyleDesignerDialog(Path.Combine(Path.GetTempPath(), "NoFencesPreviewStyles"), null, () => { }), Path.Combine(outDir, "style-designer.png"));
            RenderWidgets(outDir, host);
            RenderFlags(outDir);
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
                Tabs = { new FenceTab { Name = "Arbeit" }, new FenceTab { Name = "Spiele" }, new FenceTab { Name = "Tools" } },
                Marks = new(StringComparer.OrdinalIgnoreCase) { [samples[0]] = MarkColor.Red, [samples[2]] = MarkColor.Green }
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

            // Notes with a picture and a voice note, and a protected one
            if (samples.FirstOrDefault(s => s.EndsWith(".png")) is { } picture)
            {
                using var notes = new Bitmap(600, 360, PixelFormat.Format32bppArgb);
                using var ng = Graphics.FromImage(notes);
                DrawBackdrop(ng, new Rectangle(Point.Empty, notes.Size));
                var withMedia = new FenceInfo
                {
                    Name = "Urlaub", Kind = FenceKind.Note, Theme = "postit", TitleHeight = 30,
                    NoteText = $"Fotos vom Strand:\n{NoteText.MediaMarkup("Bild", picture)}\n{NoteText.MediaMarkup("Sprachnotiz 0:12", @"C:\demo\voice.wav")}"
                };
                var locked = new FenceInfo { Name = "Zugangsdaten", Kind = FenceKind.Note, Theme = "postit", TitleHeight = 30, NoteCipher = NoteCrypto.Encrypt("demo", "demo") };
                var x = 24;
                foreach (var info in new[] { withMedia, locked })
                {
                    using var window = new FenceWindow(host, info) { Size = new Size(260, 310) };
                    window.ApplySettings();
                    var state = ng.Save();
                    ng.TranslateTransform(x, 24);
                    window.PaintFence(ng);
                    ng.Restore(state);
                    x += 290;
                }
                notes.Save(Path.Combine(outDir, "notes-media.png"), ImageFormat.Png);
            }

            // Hover preview of a folder
            if (samples.FirstOrDefault(Directory.Exists) is { } folder && FenceEntry.FromPath(folder) is { } entry)
            {
                using var popup = new HoverPopup(entry, 1);
                using (var scratch = new Bitmap(400, 400))
                using (var sg = Graphics.FromImage(scratch))
                    popup.RenderTo(sg); // requests the icons
                var until = DateTime.Now.AddSeconds(1);
                while (DateTime.Now < until) { Application.DoEvents(); Thread.Sleep(20); }
                using var bmp = new Bitmap(popup.Width, popup.Height);
                using (var pg = Graphics.FromImage(bmp))
                    popup.RenderTo(pg);
                bmp.Save(Path.Combine(outDir, "hover-preview.png"), ImageFormat.Png);
            }
        }

        /// <summary>
        /// All widgets (widgets.png, with this PC's real drives and recycle bin – for checking only) and a set
        /// without personal data for the README (widgets-docs.png).
        /// </summary>
        private static void RenderWidgets(string outDir, IFenceHost host)
        {
            RenderWidgetSheet(outDir, host, "widgets.png", new (string, string, Size)[]
            {
                ("clock", "default", new Size(280, 320)),
                ("countdown", "family", new Size(280, 200)),
                ("weather", "default", new Size(270, 300)),
            });
            RenderWidgetSheet(outDir, host, "widgets-docs.png", new (string, string, Size)[]
            {
                ("clock", "default", new Size(280, 320)),
                ("countdown", "postit", new Size(280, 220)),
                ("network", "gaming", new Size(260, 230)),
            });
            RenderWidgetSheet(outDir, host, "widgets-more-docs.png", new (string, string, Size)[]
            {
                ("weather", "default", new Size(270, 300)),
                ("media", "music", new Size(330, 200)),
                ("network", "hardware", new Size(260, 230)),
                ("clipboard", "work", new Size(280, 260)),
                ("battery", "nature", new Size(220, 170)),
            });
            RenderWidgetSheet(outDir, host, "widgets-extra-docs.png", new (string, string, Size)[]
            {
                ("agenda", "windows", new Size(290, 330)),
                ("focus", "hobby", new Size(230, 280)),
                ("news", "documents", new Size(330, 330)),
                ("ticker", "finance", new Size(320, 270)),
                ("photos", "photos", new Size(330, 250)),
            });
            RenderWidgetSheet(outDir, host, "widgets-tools-docs.png", new (string, string, Size)[]
            {
                ("screentime", "default", new Size(300, 300)),
                ("audio", "gaming", new Size(300, 230)),
                ("status", "windows", new Size(330, 230)),
            });
            RenderWidgetSheet(outDir, host, "widgets-planning-docs.png", new (string, string, Size)[]
            {
                ("todo", "work", new Size(300, 300)),
                ("worldclock", "default", new Size(290, 260)),
                ("power", "hardware", new Size(280, 170)),
            });
            RenderWidgetSheet(outDir, host, "widgets-time-docs.png", new (string, string, Size)[]
            {
                ("timer", "default", new Size(280, 280)),
                ("habits", "nature", new Size(330, 230)),
                ("progress", "windows", new Size(260, 240)),
            });
            RenderWidgetSheet(outDir, host, "widgets-system-docs.png", new (string, string, Size)[]
            {
                ("network", "hardware", new Size(270, 270)),
                ("battery", "nature", new Size(240, 230)),
                ("autostart", "windows", new Size(300, 300)),
            });
            RenderWidgetSheet(outDir, host, "widgets-look-docs.png", new (string, string, Size)[]
            {
                ("weather", "contrast", new Size(280, 320)),
                ("clock", "contrast", new Size(280, 320)),
            });
            // Checking: news in a wide dark style, as people actually use it
            RenderWidgetSheet(outDir, host, "check-news.png", new (string, string, Size)[]
            {
                ("news", "multimedia", new Size(685, 344)),
                ("news", "postit", new Size(330, 330)),
            });
        }

        /// <summary>The language flags at menu size and enlarged (flags.png).</summary>
        private static void RenderFlags(string outDir)
        {
            using var sheet = new Bitmap(420, 90);
            using var g = Graphics.FromImage(sheet);
            g.Clear(Color.White);
            var x = 10;
            foreach (var code in Strings.Languages)
            {
                g.DrawImage(Flags.For(code), x, 8);
                g.DrawImage(Flags.For(code, 48), x, 30);
                x += 100;
            }
            sheet.Save(Path.Combine(outDir, "flags.png"), ImageFormat.Png);
        }

        /// <summary>Made-up content for widgets that would otherwise show live or personal data.</summary>
        private static void FillDemo(Widgets.FenceWidget? widget)
        {
            switch (widget)
            {
                case Widgets.WeatherWidget weather:
                    var today = DateTime.Today;
                    weather.SetReport(new Widgets.WeatherReport(17.4, 16.1, 12, Widgets.WeatherKind.PartlyCloudy, true, new[]
                    {
                        new Widgets.WeatherDay(today, Widgets.WeatherKind.PartlyCloudy, 19, 9),
                        new Widgets.WeatherDay(today.AddDays(1), Widgets.WeatherKind.Rain, 14, 8),
                        new Widgets.WeatherDay(today.AddDays(2), Widgets.WeatherKind.Thunder, 16, 10),
                        new Widgets.WeatherDay(today.AddDays(3), Widgets.WeatherKind.Clear, 21, 11),
                    },
                    // Rain starting in about 20 minutes, and today's sun times
                    Enumerable.Range(0, 9).Select(i => (DateTime.Now.AddMinutes(-5 + i * 15), i >= 2 ? 0.6 : 0.0)).ToList(),
                    today.AddHours(6.95), today.AddHours(18.45)));
                    break;
                case Widgets.MediaWidget media:
                    media.SetPreview("Midnight Drive", "The Synthwave Band", "Spotify", null, TimeSpan.FromSeconds(83), TimeSpan.FromSeconds(214));
                    break;
                case Widgets.AgendaWidget agenda:
                    var d = DateTime.Today;
                    agenda.SetPreview(new[]
                    {
                        new Widgets.CalendarEvent(d.AddHours(9), d.AddHours(9.5), "Team stand-up", false),
                        new Widgets.CalendarEvent(d.AddHours(23), d.AddHours(23.5), "Release NoFences", false),
                        new Widgets.CalendarEvent(d.AddDays(1), d.AddDays(2), "Anna's birthday", true),
                        new Widgets.CalendarEvent(d.AddDays(1).AddHours(18), d.AddDays(1).AddHours(19.5), "Football training", false),
                        new Widgets.CalendarEvent(d.AddDays(2).AddHours(10), d.AddDays(2).AddHours(11), "Dentist", false),
                        new Widgets.CalendarEvent(d.AddDays(3).AddHours(20), d.AddDays(3).AddHours(23), "Game night", false),
                    });
                    break;
                case Widgets.FocusWidget focus:
                    focus.SetPreview(TimeSpan.FromMinutes(17.4), true, 2);
                    break;
                case Widgets.NewsWidget news:
                    var n = DateTime.Now;
                    news.SetPreview(new[]
                    {
                        new Widgets.NewsItem("New open-source desktop tools are on the rise", "https://example.com", n.AddMinutes(-12), "Tech Daily"),
                        new Widgets.NewsItem("Ask HN: What's the most underrated tool you use every day, and why do you still rely on it after all these years?", "https://example.com", n.AddMinutes(-31), "Hacker News: Front Page"),
                        new Widgets.NewsItem("Weekend weather: sunny with a chance of clouds", "https://example.com", n.AddMinutes(-48), "Daily News"),
                        new Widgets.NewsItem("Local team wins the cup after penalty shoot-out", "https://example.com", n.AddHours(-2), "Sports"),
                        new Widgets.NewsItem("Five tips for a tidy desktop", "https://example.com", n.AddHours(-5), "Tech Daily"),
                        new Widgets.NewsItem("Museum night draws record crowds", "https://example.com", n.AddHours(-9), "Daily News"),
                        new Widgets.NewsItem("Space probe sends first close-up images", "https://example.com", n.AddDays(-1), "Science"),
                    });
                    break;
                case Widgets.TickerWidget ticker:
                    double[] Wave(double start, double drift, int seed)
                    {
                        var rnd = new Random(seed);
                        var v = start;
                        return Enumerable.Range(0, 40).Select(_ => v += start * (drift / 40 + (rnd.NextDouble() - 0.5) * 0.004)).ToArray();
                    }
                    ticker.SetPreview(new[]
                    {
                        new Widgets.Quote("BTC-EUR", "Bitcoin EUR", 75491.81, 0.0123, "EUR", Wave(74500, 0.013, 1)),
                        new Widgets.Quote("^GDAXI", "DAX", 24312.40, -0.0041, "EUR", Wave(24400, -0.004, 2)),
                        new Widgets.Quote("^ATX", "ATX", 4521.10, 0.0068, "EUR", Wave(4490, 0.007, 3)),
                        new Widgets.Quote("AAPL", "Apple Inc.", 248.37, 0.0152, "USD", Wave(244, 0.015, 4)),
                    });
                    break;
                case Widgets.PhotoWidget photo:
                    var picture = new Bitmap(800, 600);
                    using (var g = Graphics.FromImage(picture))
                    {
                        using var sky = new LinearGradientBrush(new Rectangle(0, 0, 800, 420), Color.FromArgb(255, 140, 90), Color.FromArgb(80, 60, 140), 90f);
                        g.FillRectangle(sky, 0, 0, 800, 600);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using var sun = new SolidBrush(Color.FromArgb(255, 220, 120));
                        g.FillEllipse(sun, 470, 250, 160, 160);
                        using var hills = new SolidBrush(Color.FromArgb(40, 30, 60));
                        g.FillPolygon(hills, new[] { new Point(0, 600), new Point(0, 380), new Point(180, 300), new Point(360, 400), new Point(560, 330), new Point(800, 420), new Point(800, 600) });
                    }
                    photo.SetPreview(picture);
                    break;
                case Widgets.PowerWidget power:
                    power.SetPreview(new List<Win32.PowerPlan>
                    {
                        new(Guid.NewGuid(), "Balanced", false),
                        new(Guid.NewGuid(), "High performance", true),
                        new(Guid.NewGuid(), "Power saver", false),
                    });
                    break;
                case Widgets.AudioWidget audio:
                    audio.SetPreview(new List<Win32.AudioDevice>
                    {
                        new("1", "Speakers (USB Soundbar)", true),
                        new("2", "Headphones (Wireless Headset)", false),
                        new("3", "Monitor (HDMI Audio)", false),
                    }, 0.42f, false);
                    break;
                case Widgets.StatusWidget status:
                    status.SetPreview(new[]
                    {
                        new Widgets.ServiceStatus("Game Network", Widgets.ServiceLevel.Ok, "", Array.Empty<string>(), "https://example.com"),
                        new Widgets.ServiceStatus("Chat Service", Widgets.ServiceLevel.Degraded, "Partially Degraded Service", new[] { "Voice", "Media Proxy" }, "https://example.com"),
                        new Widgets.ServiceStatus("Store", Widgets.ServiceLevel.Notice, "Maintenance", new[] { "Payments" }, "https://example.com"),
                    });
                    break;
                case Widgets.NetworkWidget network:
                    network.SetPreview(new Widgets.SpeedTest.Result(248, 41, DateTime.Now));
                    break;
                case Widgets.BatteryWidget battery:
                    battery.SetPreview(new List<Widgets.DeviceBattery> { new("Controller 1", 0.65), new("Headset", 0.8), new("Maus", 0.3) });
                    break;
                case Widgets.AutostartWidget autostart:
                    autostart.SetPreview(new List<AutostartEntry>
                    {
                        new("Chat App", @"C:\Demo\chat.exe", AutostartSource.UserRun, true),
                        new("Cloud Drive", @"C:\Demo\cloud.exe", AutostartSource.UserRun, true),
                        new("Game Launcher", @"C:\Demo\launcher.exe", AutostartSource.UserRun, false),
                        new("Music Player", @"C:\Demo\music.exe", AutostartSource.UserFolder, false),
                        new("NoFences", @"C:\Demo\NoFences.exe", AutostartSource.UserRun, true),
                        new("Audio Driver", @"C:\Demo\audio.exe", AutostartSource.MachineRun, true),
                    });
                    break;
                case Widgets.ClipboardWidget clipboard:
                    foreach (var text in new[] { "https://github.com/hofergeorg-tech/NoFences", "Meeting moved to 3 pm", "k9#Lmq2!xZ", "C:\\Projects\\report-2026.docx", "Thanks for the update!\nSee you tomorrow" }.Reverse())
                        clipboard.History.Add(text);
                    clipboard.History.AddPinned(new Widgets.ClipItem { Text = "IBAN DE00 1234 5678 9000 0000 00" });
                    using (var demo = new Bitmap(320, 180))
                    {
                        using (var dg = Graphics.FromImage(demo))
                        using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 320, 180), Color.SteelBlue, Color.LightSkyBlue, LinearGradientMode.Vertical))
                            dg.FillRectangle(sky, 0, 0, 320, 180);
                        clipboard.History.AddImage(Widgets.ClipItem.FromImage(demo));
                    }
                    break;
            }
        }

        private static void RenderWidgetSheet(string outDir, IFenceHost host, string file, (string Type, string Theme, Size Size)[] items)
        {
            const int gap = 24;
            using var sheet = new Bitmap(items.Sum(i => i.Size.Width + gap) + gap, items.Max(i => i.Size.Height) + 2 * gap, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(sheet);
            DrawBackdrop(g, new Rectangle(Point.Empty, sheet.Size));
            var x = gap;
            foreach (var (type, theme, size) in items)
            {
                var name = Widgets.WidgetRegistry.Types.First(t => t.Type == type).Name();
                var info = new FenceInfo { Name = name, Kind = FenceKind.Widget, WidgetType = type, Theme = theme, BackgroundAlpha = 140,
                    WidgetOption = type switch
                    {
                        "countdown" => Widgets.CountdownWidget.Format(new DateTime(DateTime.Now.Year, 12, 24, 18, 0, 0), "Weihnachten"),
                        "weather" => new Widgets.WeatherPlace("Wien", 48.2085, 16.3721).ToOption(),
                        "agenda" => "https://example.com/calendar.ics",
                        "news" => "https://example.com/feed.xml",
                        "photos" => Widgets.PhotoWidget.Format(60, @"C:\Pictures\Holidays"),
                        "todo" => TodoList.Format(new List<TodoItem>
                        {
                            new() { Text = "Send the report", Due = DateTime.Today.AddHours(17) },
                            new() { Text = "Water the plants", Due = DateTime.Today.AddDays(1).AddHours(8), Repeat = Repeat.Weekly },
                            new() { Text = "Book train tickets" },
                            new() { Text = "Call the dentist", Done = true },
                        }),
                        "timer" => new TimerSet
                        {
                            Timers = { new CountdownTimer { Label = "Pizza", Start = DateTime.Now.AddMinutes(-8), End = DateTime.Now.AddMinutes(7).AddSeconds(23) } },
                            Alarms = { new Alarm { Time = "06:45", Label = "Work", Days = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } },
                                       new Alarm { Time = "09:30", Label = "Weekend", Days = { DayOfWeek.Saturday, DayOfWeek.Sunday }, Enabled = false } }
                        }.Format(),
                        "habits" => System.Text.Json.JsonSerializer.Serialize(new List<Widgets.Habit>
                        {
                            new() { Name = "Sport", Done = Enumerable.Range(0, 5).Select(i => Widgets.Habit.Key(DateTime.Today.AddDays(-i))).ToList() },
                            new() { Name = "Drink water", Done = new[] { 1, 2, 4, 6 }.Select(i => Widgets.Habit.Key(DateTime.Today.AddDays(-i))).ToList() },
                            new() { Name = "Read 20 pages", Done = new[] { 0, 1 }.Select(i => Widgets.Habit.Key(DateTime.Today.AddDays(-i))).ToList() },
                        }, FenceStore.JsonOptions),
                        "worldclock" =>"Pacific Standard Time|Los Angeles\nEastern Standard Time|New York\nTokyo Standard Time|Tokyo",
                        _ => null
                    }
                };
                if (type == "weather")
                    info.Name = $"{name} Wien";
                using var window = new FenceWindow(host, info) { Size = size };
                window.ApplySettings();
                FillDemo(window.WidgetForPreview);
                window.RefreshWidgetForPreview();
                var state = g.Save();
                g.TranslateTransform(x, gap);
                window.PaintFence(g);
                g.Restore(state);
                x += size.Width + gap;
            }
            sheet.Save(Path.Combine(outDir, file), ImageFormat.Png);
        }

        /// <summary>Draws a dialog offscreen (it is never shown).</summary>
        internal static void RenderDialog(Form form, string path)
        {
            using (form)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-32000, -32000);
                form.Show();
                // Let timers (e.g. the settings preview) run before taking the picture
                var until = DateTime.Now.AddSeconds(1.5);
                while (DateTime.Now < until)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                using var bmp = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(Point.Empty, form.Size));
                bmp.Save(path, ImageFormat.Png);
                form.Hide();
            }
        }

        /// <summary>Sample sticky notes in a few styles, including checkboxes.</summary>
        private static void RenderNotes(string outDir, IFenceHost host)
        {
            const string plain = "Einkaufen:\n[x] Milch\n[ ] Brot\n[ ] Kaffee\n\nTel.\t0664 123 456\nWeb:\twww.robertsspaceindustries.com";
            // Every second note shows the formatting (headings, bold/italic, bullets, quote, rule)
            const string formatted = "# Wochenplan\n**Montag:** Sport um *18 Uhr*\n- Einkaufen\n- Paket abholen\n---\n> Geburtstag Anna!\n[ ] Kuchen backen";
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
                    Name = "Notiz", Kind = FenceKind.Note, Theme = themes[i], NoteText = i % 2 == 1 ? formatted : plain, TitleHeight = 30, BackgroundAlpha = 120,
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
                var info = new FenceInfo { Name = "Einkaufsliste", Kind = FenceKind.Note, Theme = themes[i * 2], NoteText = plain, TitleHeight = 30, CanMinify = true };
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
        internal static void DrawBackdrop(Graphics g, Rectangle r)
        {
            using var brush = new LinearGradientBrush(r, Color.FromArgb(58, 84, 120), Color.FromArgb(150, 130, 120), LinearGradientMode.ForwardDiagonal);
            g.FillRectangle(brush, r);
        }

        /// <summary>Harmless sample items (in the temp folder) for previews.</summary>
        internal static List<string> SampleFiles() => CreateSampleFiles(Path.Combine(Path.GetTempPath(), "NoFencesPreview"));

        private static List<string> CreateSampleFiles(string dir)
        {
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "Projekte"));
            // Something to show in the folder's hover preview
            Directory.CreateDirectory(Path.Combine(dir, "Projekte", "Entwürfe"));
            foreach (var name in new[] { "Angebot.docx", "Kalkulation.xlsx", "Zeitplan.pdf", "Notizen.txt" })
            {
                var path = Path.Combine(dir, "Projekte", name);
                if (!File.Exists(path))
                    File.WriteAllText(path, "NoFences preview");
            }
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
