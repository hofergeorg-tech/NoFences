using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NoFences.Util;

namespace NoFences.Widgets
{
    public sealed record SteamDeal(int AppId, string Name, int DiscountPercent, int FinalCents, int OriginalCents, string Currency, string? ImageUrl, bool FromWishlist);

    /// <summary>
    /// Current Steam sales; games from your wishlist that are on sale come first (the Steam account is
    /// found on this PC; the wishlist must be public). Click opens the store page in Steam.
    /// </summary>
    public sealed partial class SteamDealsWidget : FenceWidget
    {
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromHours(1);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<SteamDeal> deals = new();
        private readonly Dictionary<int, Image?> images = new();
        private readonly List<(RectangleF Rect, SteamDeal Deal)> rows = new();
        private DateTime nextFetch;
        private bool fetching, failed;
        private float scroll, maxScroll;
        private List<SteamDeal> wishlistDeals = new();
        private readonly List<SteamDeal> storeDeals = new();
        private int searchStart, searchTotal;
        private bool loadingMore;
        private static readonly Dictionary<int, string> Names = new();

        public SteamDealsWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "steamdeals";

        public override int RefreshMs => 60_000;

        internal void SetPreview(IEnumerable<(SteamDeal Deal, Image? Image)> demo)
        {
            deals = demo.Select(d => d.Deal).ToList();
            foreach (var (deal, image) in demo)
                images[deal.AppId] = image;
            nextFetch = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode || fetching || DateTime.UtcNow < nextFetch)
                return;
            _ = FetchAsync();
        }

        private static string Country => RegionInfo.CurrentRegion.TwoLetterISORegionName.ToLowerInvariant();

        private static string SteamLanguage => Strings.Effective switch
        {
            "de" => "german", "it" => "italian", "fr" => "french", "es" => "spanish", _ => "english"
        };

        private async Task FetchAsync()
        {
            fetching = true;
            try
            {
                var o = Settings;
                var wishlist = new List<SteamDeal>();
                var steamId = o.SteamId is { Length: > 0 } manual ? manual : LocalSteamId();
                if (steamId != null && (o.WishlistFirst || o.Source == DealSource.Wishlist))
                {
                    try
                    {
                        wishlist = await WishlistDealsAsync(steamId, o.SteamId == steamId ? o.ShareToken : null);
                    }
                    catch (Exception e)
                    {
                        Log.Write("Steam", "Wishlist: " + Log.Describe(e));
                    }
                }
                // The whole wishlist (only discount and price limits apply), then the store's list page by page
                wishlistDeals = wishlist.Where(d => Matches(d, o)).ToList();
                storeDeals.Clear();
                searchStart = 0;
                searchTotal = o.Source == DealSource.Wishlist ? 0 : int.MaxValue;
                deals = wishlistDeals.ToList();
                if (o.Source != DealSource.Wishlist)
                    await LoadStorePageAsync(o);
                failed = false;
                nextFetch = DateTime.UtcNow + UpdateEvery;
                RequestRedraw();
                await LoadImagesAsync();
            }
            catch (Exception e)
            {
                Log.Write("Steam", Log.Describe(e));
                failed = deals.Count == 0;
                nextFetch = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
        }

        public enum DealSource { Specials, TopSellers, NewReleases, Wishlist }

        /// <summary>What the widget shows; stored as JSON in the fence's widget option.</summary>
        public sealed class Options
        {
            public DealSource Source { get; set; }
            public bool WishlistFirst { get; set; } = true;
            public int MinDiscount { get; set; }
            /// <summary>Highest price in whole currency units; 0 = no limit.</summary>
            public int MaxPrice { get; set; }
            public int Count { get; set; } = 15;
            /// <summary>SteamID64 for the wishlist; empty = the account signed in on this PC.</summary>
            public string? SteamId { get; set; }
            /// <summary>The "st" of a wishlist share link: opens a wishlist that isn't public.</summary>
            public string? ShareToken { get; set; }
        }

        private Options Settings
        {
            get
            {
                var raw = getOption();
                // Before 2.5 the option was just a SteamID
                if (raw is { Length: 17 } && raw.StartsWith("7656") && raw.All(char.IsDigit))
                    return new Options { SteamId = raw };
                try { return raw == null ? new() : JsonSerializer.Deserialize<Options>(raw, Model.FenceStore.JsonOptions) ?? new(); }
                catch (JsonException) { return new(); }
            }
            set => setOption(JsonSerializer.Serialize(value, Model.FenceStore.JsonOptions));
        }

        /// <summary>Applies the minimum discount, maximum price and count; wishlist entries keep their place first.</summary>
        public static List<SteamDeal> Filter(IEnumerable<SteamDeal> deals, Options o) =>
            deals.Where(d => d.DiscountPercent >= o.MinDiscount && (o.MaxPrice <= 0 || d.FinalCents <= o.MaxPrice * 100))
                .Take(Math.Clamp(o.Count, 1, 50))
                .ToList();

        public static bool Matches(SteamDeal d, Options o) =>
            d.DiscountPercent >= o.MinDiscount && (o.MaxPrice <= 0 || d.FinalCents <= o.MaxPrice * 100);

        /// <summary>Steam's store search for the chosen list: all sales (not only the featured ones), page by page.</summary>
        private static string SearchUrl(DealSource source, int start, int count)
        {
            var filter = source switch { DealSource.TopSellers => "filter=topsellers", DealSource.NewReleases => "filter=popularnew", _ => "specials=1" };
            return $"https://store.steampowered.com/search/results/?{filter}&infinite=1&start={start}&count={count}&cc={Country}&l={SteamLanguage}";
        }

        private async Task LoadStorePageAsync(Options o)
        {
            // Pages until enough pass the discount/price limits (or the list ends)
            var page = Math.Clamp(o.Count, 5, 50);
            var added = 0;
            for (var tries = 0; tries < 5 && added < page && searchStart < searchTotal; tries++)
            {
                using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync(SearchUrl(o.Source, searchStart, 50)));
                searchTotal = doc.RootElement.TryGetProperty("total_count", out var total) && total.ValueKind == JsonValueKind.Number ? total.GetInt32() : 0;
                var html = doc.RootElement.TryGetProperty("results_html", out var h) ? h.GetString() ?? "" : "";
                var found = ParseSearchResults(html, RegionCurrency, o.Source == DealSource.Specials);
                searchStart += 50;
                if (found.Count == 0)
                    break;
                foreach (var d in found.Where(d => Matches(d, o) && wishlistDeals.All(w => w.AppId != d.AppId) && storeDeals.All(s => s.AppId != d.AppId)))
                {
                    storeDeals.Add(d);
                    added++;
                }
            }
            deals = wishlistDeals.Concat(storeDeals).ToList();
        }

        /// <summary>More of the store's list when scrolled to the end.</summary>
        private async void LoadMore()
        {
            if (loadingMore || fetching || searchStart >= searchTotal || PreviewMode)
                return;
            loadingMore = true;
            try
            {
                await LoadStorePageAsync(Settings);
                RequestRedraw();
                await LoadImagesAsync();
            }
            catch (Exception e)
            {
                Log.Write("Steam", Log.Describe(e));
                searchTotal = searchStart; // stop trying until the next refresh
            }
            finally
            {
                loadingMore = false;
            }
        }

        private static string RegionCurrency
        {
            get
            {
                try { return RegionInfo.CurrentRegion.ISOCurrencySymbol; }
                catch (ArgumentException) { return "EUR"; }
            }
        }

        /// <summary>Rows of the store search (HTML): app, name, picture, discount and prices.</summary>
        public static List<SteamDeal> ParseSearchResults(string html, string currency, bool discountedOnly)
        {
            var list = new List<SteamDeal>();
            foreach (var row in html.Split("<a href=", StringSplitOptions.RemoveEmptyEntries))
            {
                var app = Regex.Match(row, "data-ds-appid=\"(\\d+)\"");
                var title = Regex.Match(row, "<span class=\"title\">(.*?)</span>", RegexOptions.Singleline);
                var final = Regex.Match(row, "data-price-final=\"(\\d+)\"");
                if (!app.Success || !title.Success || !final.Success)
                    continue;
                var discountMatch = Regex.Match(row, "data-discount=\"(\\d+)\"");
                var discount = discountMatch.Success ? int.Parse(discountMatch.Groups[1].Value) : 0;
                if (discountedOnly && discount <= 0)
                    continue;
                var finalCents = int.Parse(final.Groups[1].Value);
                var original = discount is > 0 and < 100 ? (int)Math.Round(finalCents * 100.0 / (100 - discount)) : finalCents;
                var image = Regex.Match(row, "<img src=\"([^\"]+)\"");
                list.Add(new SteamDeal(int.Parse(app.Groups[1].Value), System.Net.WebUtility.HtmlDecode(title.Groups[1].Value.Trim()), discount,
                    finalCents, original, currency, image.Success ? image.Groups[1].Value : null, false));
            }
            return list;
        }

        public static List<SteamDeal> ParseSpecials(string json) => ParseCategory(json, "specials");

        /// <summary>One list of the store's featured categories (specials, top_sellers, new_releases).</summary>
        public static List<SteamDeal> ParseCategory(string json, string category)
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<SteamDeal>();
            if (!doc.RootElement.TryGetProperty(category, out var specials) || !specials.TryGetProperty("items", out var items))
                return list;
            foreach (var i in items.EnumerateArray())
            {
                var discount = i.TryGetProperty("discount_percent", out var d) ? d.GetInt32() : 0;
                // Sales show discounted games only; top sellers and new releases show all
                if (discount <= 0 && category == "specials")
                    continue;
                if (!i.TryGetProperty("final_price", out _) || !i.TryGetProperty("original_price", out var op) || op.ValueKind != JsonValueKind.Number)
                    continue;
                list.Add(new SteamDeal(i.GetProperty("id").GetInt32(), i.GetProperty("name").GetString() ?? "?", discount,
                    i.GetProperty("final_price").GetInt32(), i.GetProperty("original_price").GetInt32(),
                    i.TryGetProperty("currency", out var c) ? c.GetString() ?? "EUR" : "EUR",
                    i.TryGetProperty("small_capsule_image", out var img) ? img.GetString() : null, false));
            }
            return list;
        }

        /// <summary>Account and share token from what the user entered: a SteamID64 or a wishlist share link.</summary>
        public static (string? SteamId, string? Token) ParseAccount(string text)
        {
            text = text.Trim();
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^7656\d{13}$"))
                return (text, null);
            var link = System.Text.RegularExpressions.Regex.Match(text, @"/wishlist/profiles/(7656\d{13})/?(?:\?(?:.*&)?st=(\d+))?");
            return link.Success ? (link.Groups[1].Value, link.Groups[2].Success ? link.Groups[2].Value : null) : (null, null);
        }

        /// <summary>The games of a shared wishlist page (the page carries them for its first render).</summary>
        public static List<int> ParseSharedWishlist(string html) =>
            System.Text.RegularExpressions.Regex.Matches(html, @"appid\\*"":(\d+),\\*""priority")
                .Select(m => int.Parse(m.Groups[1].Value)).Distinct().ToList();

        private async Task<List<SteamDeal>> WishlistDealsAsync(string steamId, string? shareToken)
        {
            var json = await Web.Http.GetStringAsync($"https://api.steampowered.com/IWishlistService/GetWishlist/v1/?steamid={steamId}");
            List<int> ids;
            using (var doc = JsonDocument.Parse(json))
            {
                ids = doc.RootElement.GetProperty("response").TryGetProperty("items", out var items)
                    ? items.EnumerateArray().Select(i => i.GetProperty("appid").GetInt32()).Take(300).ToList()
                    : new();
            }
            // Not public: the share link's page still lists the games
            if (ids.Count == 0 && shareToken != null)
                ids = ParseSharedWishlist(await Web.Http.GetStringAsync($"https://store.steampowered.com/wishlist/profiles/{steamId}/?st={shareToken}")).Take(300).ToList();
            var result = new List<SteamDeal>();
            // Prices in batches (only "price_overview" works for several apps at once)
            foreach (var batch in ids.Chunk(25))
            {
                var prices = await Web.Http.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={string.Join(",", batch)}&filters=price_overview&cc={Country}");
                foreach (var (id, price) in ParsePrices(prices).Where(p => p.Price.Discount > 0))
                {
                    var name = await AppNameAsync(id);
                    result.Add(new SteamDeal(id, name, price.Discount, price.Final, price.Initial, price.Currency,
                        $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/capsule_184x69.jpg", true));
                }
            }
            return result.OrderByDescending(d => d.DiscountPercent).ToList();
        }

        public static List<(int Id, (int Discount, int Final, int Initial, string Currency) Price)> ParsePrices(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<(int, (int, int, int, string))>();
            foreach (var app in doc.RootElement.EnumerateObject())
            {
                if (!int.TryParse(app.Name, out var id) || !app.Value.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object
                    || !data.TryGetProperty("price_overview", out var p))
                    continue;
                list.Add((id, (p.GetProperty("discount_percent").GetInt32(), p.GetProperty("final").GetInt32(), p.GetProperty("initial").GetInt32(),
                    p.GetProperty("currency").GetString() ?? "EUR")));
            }
            return list;
        }

        private async Task<string> AppNameAsync(int id)
        {
            if (Names.TryGetValue(id, out var known))
                return known;
            try
            {
                var json = await Web.Http.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={id}&filters=basic&l={SteamLanguage}");
                using var doc = JsonDocument.Parse(json);
                return Names[id] = doc.RootElement.GetProperty(id.ToString()).GetProperty("data").GetProperty("name").GetString() ?? id.ToString();
            }
            catch (Exception)
            {
                return id.ToString();
            }
        }

        private async Task LoadImagesAsync()
        {
            foreach (var deal in deals.Where(d => d.ImageUrl != null && !images.ContainsKey(d.AppId)).ToList())
            {
                try
                {
                    var bytes = await Web.Http.GetByteArrayAsync(deal.ImageUrl);
                    using var stream = new MemoryStream(bytes);
                    using var image = Image.FromStream(stream);
                    images[deal.AppId] = new Bitmap(image);
                }
                catch (Exception)
                {
                    images[deal.AppId] = null;
                }
                RequestRedraw();
            }
        }

        [GeneratedRegex("\"(7656\\d{13})\"\\s*\\{[^}]*?\"MostRecent\"\\s*\"1\"", RegexOptions.Singleline)]
        private static partial Regex MostRecentUser();

        [GeneratedRegex("\"(7656\\d{13})\"")]
        private static partial Regex AnyUser();

        /// <summary>The Steam account last signed in on this PC (from Steam's loginusers.vdf), or null.</summary>
        public static string? LocalSteamId()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key?.GetValue("SteamPath") is not string steam)
                    return null;
                var file = Path.Combine(steam.Replace('/', '\\'), "config", "loginusers.vdf");
                if (!File.Exists(file))
                    return null;
                var vdf = File.ReadAllText(file);
                var recent = MostRecentUser().Match(vdf);
                return recent.Success ? recent.Groups[1].Value : AnyUser().Match(vdf) is { Success: true } any ? any.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string FormatPrice(int cents, string currency)
        {
            var culture = new CultureInfo(Strings.Effective);
            var symbol = currency switch { "EUR" => "€", "USD" => "$", "GBP" => "£", _ => currency };
            return $"{(cents / 100.0).ToString("N2", culture)} {symbol}";
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (deals.Count == 0)
            {
                c.Text(failed ? Strings.WeatherOffline : Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            using var bold = new Font(c.Label.FontFamily, c.Label.Size, FontStyle.Bold, c.Label.Unit);
            var imageHeight = c.Px(40);
            var imageWidth = imageHeight * 184 / 69f;
            var state = c.G.Save();
            c.G.SetClip(c.Area);
            c.G.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            float y = c.Area.Y - scroll;
            var wishlistHeader = false;
            var salesHeader = false;
            foreach (var deal in deals)
            {
                // Section headings: wishlist first, then the general sales
                if (deal.FromWishlist && !wishlistHeader || !deal.FromWishlist && !salesHeader && deals.Any(d => d.FromWishlist))
                {
                    c.Muted(deal.FromWishlist ? Strings.SteamWishlist : Strings.SteamSpecials, c.Area.X, y, small, Color.FromArgb(230, c.Theme.Accent));
                    y += small.GetHeight(c.G) + c.Px(4);
                    wishlistHeader |= deal.FromWishlist;
                    salesHeader |= !deal.FromWishlist;
                }
                var rect = new RectangleF(c.Area.X, y, c.Area.Width, imageHeight);
                if (images.TryGetValue(deal.AppId, out var image) && image != null)
                    c.G.DrawImage(image, c.Area.X, y, imageWidth, imageHeight);
                else
                    using (var back = new SolidBrush(Color.FromArgb(50, c.Theme.HintColor)))
                        c.G.FillRectangle(back, c.Area.X, y, imageWidth, imageHeight);
                var x = c.Area.X + imageWidth + c.Px(8);
                c.Text(deal.Name, new RectangleF(x, y, c.Area.Right - x, line), bold);

                // "-70 %" badge, new price, old price struck through
                var px = x;
                if (deal.DiscountPercent > 0)
                {
                    var badge = $"-{deal.DiscountPercent} %";
                    var badgeSize = c.G.MeasureString(badge, small);
                    using (var green = new SolidBrush(Color.FromArgb(255, 76, 107, 34)))
                        c.G.FillRectangle(green, x, y + line + c.Px(2), badgeSize.Width + c.Px(4), badgeSize.Height);
                    using (var lime = new SolidBrush(Color.FromArgb(255, 190, 238, 17)))
                        c.G.DrawString(badge, small, lime, x + c.Px(2), y + line + c.Px(2));
                    px += badgeSize.Width + c.Px(10);
                }
                // Free games and full-price ones (top sellers, new releases) show just the price
                var price = deal.FinalCents == 0 ? Strings.SteamFree : FormatPrice(deal.FinalCents, deal.Currency);
                px += c.Muted(price, px, y + line + c.Px(2), small, c.Ink) + c.Px(8);
                if (deal.DiscountPercent > 0)
                    using (var struck = new Font(small, FontStyle.Strikeout))
                        c.Muted(FormatPrice(deal.OriginalCents, deal.Currency), px, y + line + c.Px(2), struck);
                rows.Add((rect, deal));
                y += imageHeight + c.Px(8);
            }
            c.G.Restore(state);
            maxScroll = Math.Max(0, y + scroll - c.Area.Bottom);
            // Near the end: fetch the next page of the store's list
            if (scroll >= maxScroll - (imageHeight + c.Px(8)) * 2 && searchStart < searchTotal)
                LoadMore();
            // Grown fence: everything fits now, so don't stay scrolled down (the top would stay hidden)
            if (scroll > maxScroll)
            {
                scroll = maxScroll;
                RequestRedraw();
            }
        }

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p));

        public override string? TooltipAt(Point p) => rows.FirstOrDefault(r => r.Rect.Contains(p)).Deal?.Name;

        public override bool Click(Point p)
        {
            var deal = rows.FirstOrDefault(r => r.Rect.Contains(p)).Deal;
            if (deal == null)
                return false;
            // In the Steam client if it's installed, else in the browser
            var steamInstalled = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") != null;
            var target = steamInstalled ? $"steam://store/{deal.AppId}" : $"https://store.steampowered.com/app/{deal.AppId}";
            try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch { }
            return true;
        }

        public override bool Wheel(int delta)
        {
            if (maxScroll <= 0 && scroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 60, 0, maxScroll);
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.SteamSettingsMenu, null, (_, _) =>
            {
                using var dialog = new SteamDealsDialog(Settings);
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;
                Settings = dialog.Result;
                nextFetch = DateTime.MinValue;
                scroll = 0;
                Refresh();
            });
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }

        public override void Dispose()
        {
            foreach (var image in images.Values)
                image?.Dispose();
            images.Clear();
        }
    }
}
