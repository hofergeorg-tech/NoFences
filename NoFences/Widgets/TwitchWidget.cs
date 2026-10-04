using System.Diagnostics;
using System.Text.RegularExpressions;
using NoFences.Util;

namespace NoFences.Widgets
{
    public sealed record TwitchChannel(string Name, bool Live, string Uptime, string Game, string Title);

    /// <summary>
    /// Which of your Twitch channels are live (via the public decapi.me service, no account needed),
    /// with game, title and uptime; a notification when one goes live. Click opens the stream.
    /// </summary>
    public sealed partial class TwitchWidget : FenceWidget
    {
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromMinutes(2);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly Action<string> notify;
        private List<TwitchChannel> channels = new();
        private readonly HashSet<string> wasLive = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(RectangleF Rect, TwitchChannel Channel)> rows = new();
        private string? loadedFor;
        private DateTime nextFetch;
        private bool fetching, firstLoad = true;

        public TwitchWidget(Func<string?> getOption, Action<string?> setOption, Action<string> notify)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            this.notify = notify;
        }

        public override string Type => "twitch";

        public override int RefreshMs => 20_000;

        [GeneratedRegex(@"^[A-Za-z0-9_]{3,25}$")]
        private static partial Regex ChannelName();

        /// <summary>Channel names from the option; full twitch.tv links work too.</summary>
        public static List<string> Channels(string? option) =>
            (option ?? "").Split(new[] { '\n', ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.TrimEnd('/').Split('/').Last().ToLowerInvariant())
                .Where(c => ChannelName().IsMatch(c))
                .Distinct()
                .Take(20)
                .ToList();

        internal void SetPreview(IEnumerable<TwitchChannel> demo)
        {
            channels = demo.ToList();
            loadedFor = getOption();
            nextFetch = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            var option = getOption();
            if (option != loadedFor)
            {
                nextFetch = DateTime.MinValue;
                firstLoad = true;
            }
            if (fetching || DateTime.UtcNow < nextFetch || Channels(option).Count == 0)
                return;
            _ = FetchAsync(option);
        }

        /// <summary>decapi answers "name is offline" or an uptime like "2 hours, 5 minutes".</summary>
        public static bool IsLive(string uptimeAnswer) =>
            !uptimeAnswer.Contains("offline", StringComparison.OrdinalIgnoreCase)
            && !uptimeAnswer.Contains("not found", StringComparison.OrdinalIgnoreCase)
            && !uptimeAnswer.Contains("error", StringComparison.OrdinalIgnoreCase)
            && Regex.IsMatch(uptimeAnswer, @"\d");

        /// <summary>"2 hours, 5 minutes, 10 seconds" → "2 h 5 min".</summary>
        public static string ShortUptime(string answer)
        {
            var h = Regex.Match(answer, @"(\d+)\s*hour");
            var m = Regex.Match(answer, @"(\d+)\s*minute");
            var hours = h.Success ? int.Parse(h.Groups[1].Value) : 0;
            var minutes = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            return hours > 0 ? $"{hours} h {minutes} min" : $"{minutes} min";
        }

        private async Task FetchAsync(string? option)
        {
            fetching = true;
            try
            {
                var names = Channels(option);
                var results = await Task.WhenAll(names.Select(LoadAsync));
                channels = results.OrderByDescending(c => c.Live).ThenBy(c => c.Name).ToList();
                // Notify about channels that just went live (not about those already live at start)
                foreach (var c in channels.Where(c => c.Live && !wasLive.Contains(c.Name)))
                {
                    if (!firstLoad)
                        notify(Strings.TwitchWentLive(c.Name, c.Game));
                }
                wasLive.Clear();
                foreach (var c in channels.Where(c => c.Live))
                    wasLive.Add(c.Name);
                firstLoad = false;
                loadedFor = option;
                nextFetch = DateTime.UtcNow + UpdateEvery;
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
        }

        private static async Task<TwitchChannel> LoadAsync(string name)
        {
            try
            {
                var uptime = (await Web.Http.GetStringAsync($"https://decapi.me/twitch/uptime/{name}")).Trim();
                if (!IsLive(uptime))
                    return new TwitchChannel(name, false, "", "", "");
                var game = (await Web.Http.GetStringAsync($"https://decapi.me/twitch/game/{name}")).Trim();
                var title = (await Web.Http.GetStringAsync($"https://decapi.me/twitch/title/{name}")).Trim();
                return new TwitchChannel(name, true, ShortUptime(uptime), game, title);
            }
            catch (Exception e)
            {
                Log.Write("Twitch", $"{name}: {Log.Describe(e)}");
                return new TwitchChannel(name, false, "", "", "");
            }
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (Channels(getOption()).Count == 0 && channels.Count == 0)
            {
                c.TextWrapped(Strings.TwitchHint, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                return;
            }
            if (channels.Count == 0)
            {
                c.Text(Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }
            using var bold = new Font(c.Label.FontFamily, c.Label.Size, FontStyle.Bold, c.Label.Unit);
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            var dot = c.Px(9);
            float y = c.Area.Y;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var ch in channels)
            {
                if (y + line > c.Area.Bottom)
                    break;
                var top = y;
                using (var brush = new SolidBrush(ch.Live ? Color.FromArgb(235, 40, 40) : Color.FromArgb(120, c.Theme.HintColor)))
                    c.G.FillEllipse(brush, c.Area.X, y + (line - dot) / 2, dot, dot);
                var x = c.Area.X + dot + c.Px(8);
                c.Text(ch.Name, new RectangleF(x, y, c.Area.Width * 0.55f, line), ch.Live ? bold : null);
                c.Text(ch.Live ? ch.Uptime : Strings.TwitchOffline, new RectangleF(c.Area.X + c.Area.Width * 0.5f, y, c.Area.Width * 0.5f, line), align: StringAlignment.Far);
                y += line;
                if (ch.Live)
                {
                    c.Muted(ch.Game, x, y, small, Color.FromArgb(230, c.Theme.Accent));
                    y += small.GetHeight(c.G);
                    if (ch.Title.Length > 0 && y + line <= c.Area.Bottom)
                        y += c.TextWrapped(ch.Title, x, y, c.Area.Right - x, small, 2);
                }
                y += c.Px(6);
                rows.Add((new RectangleF(c.Area.X, top, c.Area.Width, y - top), ch));
            }
        }

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p));

        public override string? TooltipAt(Point p) => rows.FirstOrDefault(r => r.Rect.Contains(p)).Channel?.Title;

        public override bool Click(Point p)
        {
            var ch = rows.FirstOrDefault(r => r.Rect.Contains(p)).Channel;
            if (ch == null)
                return false;
            try { Process.Start(new ProcessStartInfo($"https://www.twitch.tv/{ch.Name}") { UseShellExecute = true }); } catch { }
            return true;
        }

        public override void DoubleClick(Point p)
        {
            if (Channels(getOption()).Count == 0)
                Edit(null);
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.TwitchSet, null, (_, _) => Edit(owner));
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }

        private void Edit(IWin32Window? owner)
        {
            using var dialog = new TextListDialog(Strings.WidgetTwitch, Strings.TwitchPrompt, string.Join("\n", Channels(getOption())));
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            setOption(string.Join("\n", Channels(dialog.Value)));
            Refresh();
        }
    }
}
