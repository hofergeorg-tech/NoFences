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
        private float scroll, maxScroll;

        public NewsWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "news";

        public override int RefreshMs => 30_000;

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
            if (fetching || DateTime.UtcNow < nextFetch || AgendaWidget.Urls(option).Count == 0)
                return;
            _ = FetchAsync(option);
        }

        private async Task FetchAsync(string? option)
        {
            fetching = true;
            try
            {
                var all = new List<NewsItem>();
                foreach (var url in AgendaWidget.Urls(option))
                {
                    try
                    {
                        all.AddRange(ParseFeed(await Web.Http.GetStringAsync(url)));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Feed {url}: {e.Message}");
                    }
                }
                items = all.OrderByDescending(i => i.Published ?? DateTime.MinValue).Take(60).ToList();
                failed = all.Count == 0;
                loadedFor = option;
                nextFetch = DateTime.UtcNow + (failed ? TimeSpan.FromMinutes(3) : UpdateEvery);
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
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
            if (AgendaWidget.Urls(getOption()).Count == 0)
            {
                c.Text(Strings.NewsHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }
            if (items.Count == 0)
            {
                c.Text(failed ? Strings.WeatherOffline : Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }

            var state = c.G.Save();
            c.G.SetClip(c.Area);
            float y = c.Area.Y - scroll;
            var now = DateTime.Now;
            using var small = c.Sized(c.Label.GetHeight(c.G) * 0.78f);
            var smallLine = small.GetHeight(c.G) + c.Px(1);
            foreach (var item in items)
            {
                var rect = new RectangleF(c.Area.X, y, c.Area.Width, line + smallLine + c.Px(6));
                if (rect.Bottom > c.Area.Top && rect.Top < c.Area.Bottom)
                {
                    c.Text(item.Title, new RectangleF(c.Area.X, y, c.Area.Width, line));
                    var meta = item.Published is { } p ? $"{Age(p, now)} · {item.Source}" : item.Source;
                    c.Text(meta, new RectangleF(c.Area.X, y + line, c.Area.Width, smallLine), small);
                    rows.Add((rect, item));
                }
                y += rect.Height;
            }
            c.G.Restore(state);
            maxScroll = Math.Max(0, y + scroll - c.Area.Bottom);
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
            if (maxScroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 60, 0, maxScroll);
            return true;
        }

        public override void DoubleClick(Point p)
        {
            if (AgendaWidget.Urls(getOption()).Count == 0)
                Edit(null);
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
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
