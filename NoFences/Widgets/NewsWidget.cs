using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using NoFences.Util;

namespace NoFences.Widgets
{
    public sealed record NewsItem(string Title, string? Link, DateTime? Published, string Source);

    /// <summary>Headlines from RSS or Atom feeds, newest first; click opens the article.</summary>
    public sealed class NewsWidget : FenceWidget
    {
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromMinutes(30);

        public static readonly (string Name, string Url)[] Presets =
        {
            ("Tagesschau", "https://www.tagesschau.de/index~rss2.xml"),
            ("ORF", "https://rss.orf.at/news.xml"),
            ("heise online", "https://www.heise.de/rss/heise-atom.xml"),
            ("BBC News", "https://feeds.bbci.co.uk/news/rss.xml"),
            ("The Verge", "https://www.theverge.com/rss/index.xml"),
            ("Hacker News", "https://hnrss.org/frontpage"),
            ("ANSA", "https://www.ansa.it/sito/ansait_rss.xml"),
            ("Le Monde", "https://www.lemonde.fr/rss/une.xml"),
            ("El País", "https://feeds.elpais.com/mrss-s/pages/ep/site/elpais.com/portada"),
        };

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<NewsItem> items = new();
        private readonly List<(RectangleF Rect, NewsItem Item)> rows = new();
        private string? loadedFor;
        private DateTime nextFetch;
        private bool fetching, failed;
        private string? lastError;
        private bool wrongLinks;
        private float scroll, maxScroll;

        /// <summary>Game news: announcements of your installed Steam games instead of RSS feeds.</summary>
        private readonly bool gameNews;

        public NewsWidget(Func<string?> getOption, Action<string?> setOption, bool gameNews = false)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            this.gameNews = gameNews;
        }

        public override string Type => gameNews ? "gamenews" : "news";

        public override int RefreshMs => 30_000;

        /// <summary>Official announcements (patch notes, events) of the most recently played installed Steam games.</summary>
        private static async Task<List<NewsItem>> LoadGameNewsAsync()
        {
            var library = await GameLibrary.CachedAsync();
            var steam = library.Where(g => g.Source == GameSource.Steam)
                .OrderByDescending(g => g.LastPlayed).Take(10).ToList();
            var tasks = steam.Select(async game =>
            {
                try
                {
                    var appId = game.Id["steam:".Length..];
                    var json = await Web.Http.GetStringAsync($"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid={appId}&count=4&maxlength=1&feeds=steam_community_announcements");
                    return ParseSteamNews(json, game.Name);
                }
                catch (Exception e)
                {
                    Log.Write("Game news", $"{game.Name}: {Log.Describe(e)}");
                    return new List<NewsItem>();
                }
            });
            return (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
        }

        public static List<NewsItem> ParseSteamNews(string json, string game)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var list = new List<NewsItem>();
            if (!doc.RootElement.TryGetProperty("appnews", out var news) || !news.TryGetProperty("newsitems", out var items))
                return list;
            foreach (var i in items.EnumerateArray())
            {
                var date = i.TryGetProperty("date", out var d) ? DateTimeOffset.FromUnixTimeSeconds(d.GetInt64()).LocalDateTime : (DateTime?)null;
                list.Add(new NewsItem(Clean(i.GetProperty("title").GetString()), i.TryGetProperty("url", out var u) ? u.GetString() : null, date, game));
            }
            return list;
        }

        internal void SetPreview(IEnumerable<NewsItem> demo)
        {
            items = demo.ToList();
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
            if (fetching || DateTime.UtcNow < nextFetch || !gameNews && AgendaWidget.Urls(option).Count == 0)
                return;
            _ = FetchAsync(option);
        }

        private async Task FetchAsync(string? option)
        {
            fetching = true;
            try
            {
                var all = new List<NewsItem>();
                var problems = new List<string>();
                var onlyWrongLinks = true;
                if (gameNews)
                {
                    try
                    {
                        all.AddRange(await LoadGameNewsAsync());
                    }
                    catch (Exception e)
                    {
                        Log.Write("Game news", Log.Describe(e));
                        problems.Add(Strings.WeatherOffline);
                        onlyWrongLinks = false;
                    }
                }
                foreach (var url in gameNews ? Array.Empty<string>() : AgendaWidget.Urls(option))
                {
                    try
                    {
                        all.AddRange(ParseFeed(await LoadFeedAsync(url)));
                    }
                    catch (Exception e)
                    {
                        Log.Write("News", $"{url}: {Log.Describe(e)}");
                        problems.Add(Explain(url, e));
                        onlyWrongLinks &= e is NotAFeedException;
                    }
                }
                var error = problems.Count > 0 ? string.Join("\n", problems) : null;
                // A failed update keeps the headlines that are already there
                if (all.Count > 0 || option != loadedFor)
                    items = all.OrderByDescending(i => i.Published ?? DateTime.MinValue).Take(60).ToList();
                failed = all.Count == 0;
                lastError = failed ? error : null;
                wrongLinks = failed && onlyWrongLinks;
                loadedFor = option;
                // A web page won't turn into a feed: no point in asking every 30 seconds
                nextFetch = DateTime.UtcNow + (failed && !wrongLinks ? TimeSpan.FromSeconds(30) : UpdateEvery);
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
        }

        /// <summary>The address is a web page, not a feed (and the page names no working feed either).</summary>
        public sealed class NotAFeedException : Exception
        {
            public NotAFeedException() : base("The address is a web page, not an RSS/Atom feed (and it names no working feed).")
            {
            }
        }

        /// <summary>
        /// Loads a feed. A web page instead of a feed is common ("I pasted the site's address"); then the
        /// feed the page announces (&lt;link rel="alternate" type="application/rss+xml"&gt;) is used.
        /// </summary>
        private static async Task<string> LoadFeedAsync(string url)
        {
            var text = await Web.Http.GetStringAsync(url);
            if (!LooksLikeHtml(text))
                return text;
            var feed = DiscoverFeed(text, url);
            if (feed == null || feed == url)
                throw new NotAFeedException();
            text = await Web.Http.GetStringAsync(feed);
            return LooksLikeHtml(text) ? throw new NotAFeedException() : text;
        }

        public static bool LooksLikeHtml(string text)
        {
            var start = text.TrimStart().Length > 300 ? text.TrimStart()[..300] : text.TrimStart();
            return start.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) || start.Contains("<html", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The RSS/Atom link a web page announces in its head, made absolute; null if none.</summary>
        public static string? DiscoverFeed(string html, string pageUrl)
        {
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(html, "<link\\b[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                var tag = m.Value;
                if (!tag.Contains("alternate", StringComparison.OrdinalIgnoreCase)
                    || !(tag.Contains("application/rss+xml", StringComparison.OrdinalIgnoreCase) || tag.Contains("application/atom+xml", StringComparison.OrdinalIgnoreCase)))
                    continue;
                var href = System.Text.RegularExpressions.Regex.Match(tag, "href\\s*=\\s*[\"']([^\"']+)[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (href.Success && Uri.TryCreate(new Uri(pageUrl), System.Net.WebUtility.HtmlDecode(href.Groups[1].Value), out var feed))
                    return feed.AbsoluteUri;
            }
            return null;
        }

        /// <summary>A short, friendly reason for the widget (the details go to the log).</summary>
        private static string Explain(string url, Exception e)
        {
            var host = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host.Replace("www.", "") : url;
            return e switch
            {
                NotAFeedException => Strings.NewsNotAFeed(host),
                System.Xml.XmlException => Strings.NewsBroken(host),
                _ => Strings.NewsUnreachable(host)
            };
        }

        /// <summary>RSS 2.0, RSS 1.0 (RDF) and Atom.</summary>
        public static List<NewsItem> ParseFeed(string xml)
        {
            // Some feeds carry a DOCTYPE; ignore it (and never resolve anything external)
            using var reader = System.Xml.XmlReader.Create(new StringReader(xml), new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore, XmlResolver = null });
            var doc = XDocument.Load(reader);
            var root = doc.Root!;
            var list = new List<NewsItem>();
            XNamespace atom = "http://www.w3.org/2005/Atom";
            if (root.Name == atom + "feed")
            {
                var source = root.Element(atom + "title")?.Value.Trim() ?? "";
                foreach (var e in root.Elements(atom + "entry"))
                {
                    var link = e.Elements(atom + "link").FirstOrDefault(l => (string?)l.Attribute("rel") is null or "alternate")?.Attribute("href")?.Value;
                    list.Add(new NewsItem(Clean(e.Element(atom + "title")?.Value), link, Date(e.Element(atom + "published")?.Value ?? e.Element(atom + "updated")?.Value), source));
                }
                return list;
            }

            // RSS 2.0 has channel/item, RSS 1.0 has item next to channel; namespaces vary
            var channel = root.Descendants().FirstOrDefault(x => x.Name.LocalName == "channel");
            var feedTitle = channel?.Elements().FirstOrDefault(x => x.Name.LocalName == "title")?.Value.Trim() ?? "";
            foreach (var item in root.Descendants().Where(x => x.Name.LocalName == "item"))
            {
                string? Child(string name) => item.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
                list.Add(new NewsItem(Clean(Child("title")), Child("link")?.Trim(), Date(Child("pubDate") ?? Child("date")), feedTitle));
            }
            return list;
        }

        private static string Clean(string? text) =>
            System.Net.WebUtility.HtmlDecode(string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));

        private static DateTime? Date(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            text = text.Trim();
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d))
                return d.LocalDateTime;
            // RFC 822 with a zone name ("Sat, 03 Oct 2026 10:00:00 GMT" parses; "… CEST" doesn't)
            var lastSpace = text.LastIndexOf(' ');
            if (lastSpace > 0 && DateTime.TryParse(text[..lastSpace], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                return dt.ToLocalTime();
            return null;
        }

        /// <summary>Feed titles are often long ("Hacker News: Front Page", "heise online News"); keep the name part.</summary>
        public static string ShortSource(string source)
        {
            var s = source.Trim();
            var colon = s.IndexOfAny(new[] { ':', '|', '–', '—' });
            if (colon > 2)
                s = s[..colon].Trim();
            if (s.EndsWith(" News", StringComparison.OrdinalIgnoreCase) && s.Length > 12)
                s = s[..^5];
            return s.Length > 28 ? s[..28].TrimEnd() + "…" : s;
        }

        /// <summary>"5 min", "3 h", "2 d" since publishing.</summary>
        public static string Age(DateTime published, DateTime now)
        {
            var age = now - published;
            if (age.TotalMinutes < 60)
                return $"{Math.Max(1, (int)age.TotalMinutes)} min";
            if (age.TotalHours < 24)
                return $"{(int)age.TotalHours} h";
            return Strings.CountdownDays((int)age.TotalDays);
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (!gameNews && AgendaWidget.Urls(getOption()).Count == 0)
            {
                c.Text(Strings.NewsHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }
            if (items.Count == 0)
            {
                // Loaded, but no installed Steam games (or none with news)
                if (gameNews && !failed && !fetching && nextFetch > DateTime.UtcNow)
                {
                    c.TextWrapped(Strings.GameNewsNone, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                    return;
                }
                if (!failed)
                {
                    c.Text(Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                    return;
                }
                var used = c.TextWrapped(wrongLinks ? Strings.NewsCheckLinks : Strings.NewsFailed, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                if (lastError != null)
                {
                    using var detail = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
                    c.TextWrapped(lastError, c.Area.X, c.Area.Y + used + c.Px(6), c.Area.Width, detail, 4);
                }
                return;
            }

            var state = c.G.Save();
            c.G.SetClip(c.Area);
            float y = c.Area.Y - scroll;
            var now = DateTime.Now;
            // Headline semibold and a little larger, up to two lines; source and age small and dimmed below
            using var title = new Font(c.Label.FontFamily, c.Label.Size * 1.08f, FontStyle.Bold, c.Label.Unit);
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            var smallLine = small.GetHeight(c.G);
            var gap = c.Px(7);
            using var separator = new Pen(Color.FromArgb(45, c.Theme.HintColor));
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var top = y;
                // Measure (and draw) only rows near the visible area; others count with a typical height
                var visible = y < c.Area.Bottom + c.Px(40) && y > c.Area.Top - c.Px(120);
                var titleHeight = visible
                    ? c.TextWrapped(item.Title, c.Area.X, y, c.Area.Width, title, 2)
                    : title.GetHeight(c.G) * 1.5f;
                y += titleHeight + c.Px(2);
                if (visible)
                {
                    var x = c.Area.X + c.Muted(ShortSource(item.Source), c.Area.X, y, small, Color.FromArgb(230, c.Theme.Accent));
                    if (item.Published is { } p)
                        c.Muted($"  ·  {Age(p, now)}", x, y, small);
                }
                y += smallLine + gap;
                rows.Add((new RectangleF(c.Area.X, top, c.Area.Width, y - top), item));
                if (visible && i < items.Count - 1)
                    c.G.DrawLine(separator, c.Area.X, y - gap / 2f, c.Area.Right, y - gap / 2f);
            }
            c.G.Restore(state);
            maxScroll = Math.Max(0, y + scroll - c.Area.Bottom);
            // Grown fence: everything fits now, so don't stay scrolled down (the top would stay hidden)
            if (scroll > maxScroll)
            {
                scroll = maxScroll;
                RequestRedraw();
            }
        }

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p) && r.Item.Link != null);

        public override string? TooltipAt(Point p) => rows.FirstOrDefault(r => r.Rect.Contains(p)).Item?.Title;

        public override bool Click(Point p)
        {
            var item = rows.FirstOrDefault(r => r.Rect.Contains(p)).Item;
            if (item?.Link is not { } link || !Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                return false;
            try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); } catch { }
            return true;
        }

        public override bool Wheel(int delta)
        {
            if (maxScroll <= 0 && scroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 60, 0, maxScroll);
            return true;
        }

        public override void DoubleClick(Point p)
        {
            if (!gameNews && AgendaWidget.Urls(getOption()).Count == 0)
                Edit(null);
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            if (!gameNews)
                menu.Add(Strings.NewsSet, null, (_, _) => Edit(owner));
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }

        private void Edit(IWin32Window? owner)
        {
            var urls = AskFeeds(owner, getOption());
            if (urls == null)
                return;
            setOption(urls);
            scroll = 0;
            Refresh();
        }

        public static string? AskFeeds(IWin32Window? owner, string? current)
        {
            using var dialog = new TextListDialog(Strings.WidgetNews, Strings.NewsPrompt, current ?? "", Presets);
            return dialog.ShowDialog(owner) == DialogResult.OK ? string.Join("\n", AgendaWidget.Urls(dialog.Value)) : null;
        }
    }
}
