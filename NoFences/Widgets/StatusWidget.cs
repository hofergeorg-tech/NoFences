using System.Diagnostics;
using System.Text.Json;
using NoFences.Util;

namespace NoFences.Widgets
{
    public enum ServiceLevel { Ok, Notice, Degraded, Down, Unknown }

    public sealed record ServiceStatus(string Name, ServiceLevel Level, string Text, IReadOnlyList<string> Problems, string Url);

    /// <summary>
    /// Whether online services work right now, from their public status pages – Statuspage
    /// (Discord, Epic Games, GitHub …) and cState (RSI) are understood. Updated every five minutes.
    /// </summary>
    public sealed class StatusWidget : FenceWidget
    {
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromMinutes(5);

        public static readonly (string Name, string Url)[] Presets =
        {
            ("RSI / Star Citizen", "https://status.robertsspaceindustries.com"),
            ("Discord", "https://discordstatus.com"),
            ("Epic Games", "https://status.epicgames.com"),
            ("GitHub", "https://www.githubstatus.com"),
            ("OpenAI", "https://status.openai.com"),
            ("Cloudflare", "https://www.cloudflarestatus.com"),
            ("Reddit", "https://www.redditstatus.com"),
        };

        public const string DefaultOption = "https://status.robertsspaceindustries.com\nhttps://discordstatus.com\nhttps://status.epicgames.com";

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<ServiceStatus> services = new();
        private readonly List<(RectangleF Rect, ServiceStatus Service)> rows = new();
        private string? loadedFor;
        private DateTime nextFetch;
        private bool fetching;

        public StatusWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "status";

        public override int RefreshMs => 20_000;

        internal void SetPreview(IEnumerable<ServiceStatus> demo)
        {
            services = demo.ToList();
            loadedFor = getOption();
            nextFetch = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            var option = getOption();
            if (option != loadedFor)
                nextFetch = DateTime.MinValue;
            if (fetching || DateTime.UtcNow < nextFetch)
                return;
            _ = FetchAsync(option);
        }

        private async Task FetchAsync(string? option)
        {
            fetching = true;
            try
            {
                var urls = AgendaWidget.Urls(option ?? DefaultOption);
                var results = await Task.WhenAll(urls.Select(LoadAsync));
                services = results.ToList();
                loadedFor = option;
                // Something is wrong somewhere: look again sooner
                nextFetch = DateTime.UtcNow + (services.Any(s => s.Level is ServiceLevel.Degraded or ServiceLevel.Down) ? TimeSpan.FromMinutes(2) : UpdateEvery);
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
        }

        private static async Task<ServiceStatus> LoadAsync(string url)
        {
            var site = url.TrimEnd('/');
            var host = Uri.TryCreate(site, UriKind.Absolute, out var u) ? u.Host.Replace("www.", "") : site;
            try
            {
                if (site.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return ParseAny(await Web.Http.GetStringAsync(site), site, host);
                try
                {
                    return ParseStatuspage(await Web.Http.GetStringAsync(site + "/api/v2/summary.json"), site);
                }
                catch (Exception)
                {
                    // Not a Statuspage site: try cState
                    return ParseCState(await Web.Http.GetStringAsync(site + "/index.json"), site);
                }
            }
            catch (Exception e)
            {
                Log.Write("Status", $"{site}: {Log.Describe(e)}");
                return new ServiceStatus(host, ServiceLevel.Unknown, Strings.StatusUnknown, Array.Empty<string>(), site);
            }
        }

        private static ServiceStatus ParseAny(string json, string url, string host)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("systems", out _) ? ParseCState(json, url) : ParseStatuspage(json, url);
        }

        /// <summary>Statuspage.io summary.json (Discord, GitHub, Epic Games …).</summary>
        public static ServiceStatus ParseStatuspage(string json, string url)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var name = root.GetProperty("page").GetProperty("name").GetString() ?? url;
            var status = root.GetProperty("status");
            var indicator = status.GetProperty("indicator").GetString();
            var level = indicator switch
            {
                "none" => ServiceLevel.Ok,
                "maintenance" => ServiceLevel.Notice,
                "minor" => ServiceLevel.Degraded,
                _ => ServiceLevel.Down
            };
            var problems = new List<string>();
            if (root.TryGetProperty("components", out var components))
            {
                foreach (var c in components.EnumerateArray())
                {
                    var s = c.TryGetProperty("status", out var st) ? st.GetString() : "operational";
                    // Group headers repeat their children; skip them
                    if (s is "operational" or null || c.TryGetProperty("group", out var g) && g.ValueKind == JsonValueKind.True)
                        continue;
                    problems.Add(c.GetProperty("name").GetString() ?? "?");
                    if (s == "under_maintenance" && level == ServiceLevel.Ok)
                        level = ServiceLevel.Notice;
                }
            }
            return new ServiceStatus(name.Replace(" Public", ""), level, status.GetProperty("description").GetString() ?? "", problems, url);
        }

        /// <summary>cState index.json (RSI).</summary>
        public static ServiceStatus ParseCState(string json, string url)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var name = root.TryGetProperty("title", out var t) ? (t.GetString() ?? url).Replace(" Status", "") : url;
            var problems = new List<string>();
            var worst = ServiceLevel.Ok;
            foreach (var system in root.GetProperty("systems").EnumerateArray())
            {
                var level = system.GetProperty("status").GetString() switch
                {
                    "ok" or "operational" => ServiceLevel.Ok,
                    "notice" or "maintenance" => ServiceLevel.Notice,
                    "disrupted" or "degraded" or "partial" => ServiceLevel.Degraded,
                    _ => ServiceLevel.Down
                };
                if (level != ServiceLevel.Ok)
                    problems.Add(system.GetProperty("name").GetString() ?? "?");
                if (level > worst)
                    worst = level;
            }
            return new ServiceStatus(name, worst, Strings.StatusLevelName(worst), problems, url);
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (services.Count == 0)
            {
                c.Text(Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }
            float y = c.Area.Y;
            using var bold = new Font(c.Label.FontFamily, c.Label.Size, FontStyle.Bold, c.Label.Unit);
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            var dot = c.Px(10);
            foreach (var s in services)
            {
                if (y + line > c.Area.Bottom)
                    break;
                var top = y;
                using (var brush = new SolidBrush(LevelColor(s.Level)))
                    c.G.FillEllipse(brush, c.Area.X, y + (line - dot) / 2, dot, dot);
                var x = c.Area.X + dot + c.Px(8);
                c.Text(s.Name, new RectangleF(x, y, c.Area.Width * 0.5f, line), bold);
                c.Text(s.Level == ServiceLevel.Ok ? Strings.StatusLevelName(ServiceLevel.Ok) : s.Text,
                    new RectangleF(c.Area.X + c.Area.Width * 0.45f, y, c.Area.Width * 0.55f, line), align: StringAlignment.Far);
                y += line;
                if (s.Problems.Count > 0 && y + line <= c.Area.Bottom)
                {
                    var used = c.TextWrapped(string.Join(", ", s.Problems), x, y, c.Area.Right - x, small, 2);
                    y += used;
                }
                y += c.Px(8);
                rows.Add((new RectangleF(c.Area.X, top, c.Area.Width, y - top), s));
            }
        }

        public static Color LevelColor(ServiceLevel level) => level switch
        {
            ServiceLevel.Ok => Color.FromArgb(70, 200, 110),
            ServiceLevel.Notice => Color.FromArgb(80, 160, 240),
            ServiceLevel.Degraded => Color.FromArgb(245, 180, 40),
            ServiceLevel.Down => Color.FromArgb(240, 70, 60),
            _ => Color.FromArgb(150, 150, 150)
        };

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p));

        public override bool Click(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Rect.Contains(p));
            if (row.Service == null || !Uri.TryCreate(row.Service.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                return false;
            var page = uri.AbsoluteUri.EndsWith(".json") ? uri.GetLeftPart(UriPartial.Authority) : uri.AbsoluteUri;
            try { Process.Start(new ProcessStartInfo(page) { UseShellExecute = true }); } catch { }
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.StatusSet, null, (_, _) =>
            {
                using var dialog = new TextListDialog(Strings.WidgetStatus, Strings.StatusPrompt, getOption() ?? DefaultOption, Presets);
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;
                setOption(string.Join("\n", AgendaWidget.Urls(dialog.Value)));
                Refresh();
            });
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }
    }
}
