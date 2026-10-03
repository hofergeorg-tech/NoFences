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
                var list = new List<SteamDeal>();
                var steamId = getOption() is { Length: > 0 } manual ? manual : LocalSteamId();
                if (steamId != null)
                {
                    try
                    {
                        list.AddRange(await WishlistDealsAsync(steamId));
                    }
                    catch (Exception e)
                    {
                        Log.Write("Steam", "Wishlist: " + Log.Describe(e));
                    }
                }
                var specials = ParseSpecials(await Web.Http.GetStringAsync($"https://store.steampowered.com/api/featuredcategories?cc={Country}&l={SteamLanguage}"));
                list.AddRange(specials.Where(s => list.All(w => w.AppId != s.AppId)));
                deals = list;
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

        public static List<SteamDeal> ParseSpecials(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<SteamDeal>();
            if (!doc.RootElement.TryGetProperty("specials", out var specials) || !specials.TryGetProperty("items", out var items))
                return list;
            foreach (var i in items.EnumerateArray())
            {
                var discount = i.TryGetProperty("discount_percent", out var d) ? d.GetInt32() : 0;
                if (discount <= 0)
                    continue;
                list.Add(new SteamDeal(i.GetProperty("id").GetInt32(), i.GetProperty("name").GetString() ?? "?", discount,
                    i.GetProperty("final_price").GetInt32(), i.GetProperty("original_price").GetInt32(),
                    i.TryGetProperty("currency", out var c) ? c.GetString() ?? "EUR" : "EUR",
                    i.TryGetProperty("small_capsule_image", out var img) ? img.GetString() : null, false));
            }
            return list;
        }

        private async Task<List<SteamDeal>> WishlistDealsAsync(string steamId)
        {
            var json = await Web.Http.GetStringAsync($"https://api.steampowered.com/IWishlistService/GetWishlist/v1/?steamid={steamId}");
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.GetProperty("response").TryGetProperty("items", out var items))
                return new();
            var ids = items.EnumerateArray().Select(i => i.GetProperty("appid").GetInt32()).Take(100).ToList();
            var result = new List<SteamDeal>();
            // Prices in batches (only "price_overview" works for several apps at once)
            foreach (var batch in ids.Chunk(25))
            {
                var prices = await Web.Http.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={string.Join(",", batch)}&filters=price_overview&cc={Country}");
                foreach (var (id, price) in ParsePrices(prices).Where(p => p.Price.Discount > 0).Take(10 - result.Count))
                {
                    var name = await AppNameAsync(id);
                    result.Add(new SteamDeal(id, name, price.Discount, price.Final, price.Initial, price.Currency,
                        $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/capsule_184x69.jpg", true));
                }
                if (result.Count >= 10)
                    break;
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
            try
            {
                var json = await Web.Http.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={id}&filters=basic&l={SteamLanguage}");
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty(id.ToString()).GetProperty("data").GetProperty("name").GetString() ?? id.ToString();
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
                var badge = $"-{deal.DiscountPercent} %";
                var badgeSize = c.G.MeasureString(badge, small);
                using (var green = new SolidBrush(Color.FromArgb(255, 76, 107, 34)))
                    c.G.FillRectangle(green, x, y + line + c.Px(2), badgeSize.Width + c.Px(4), badgeSize.Height);
                using (var lime = new SolidBrush(Color.FromArgb(255, 190, 238, 17)))
                    c.G.DrawString(badge, small, lime, x + c.Px(2), y + line + c.Px(2));
                var px = x + badgeSize.Width + c.Px(10);
                px += c.Muted(FormatPrice(deal.FinalCents, deal.Currency), px, y + line + c.Px(2), small, c.Ink) + c.Px(8);
                using (var struck = new Font(small, FontStyle.Strikeout))
                    c.Muted(FormatPrice(deal.OriginalCents, deal.Currency), px, y + line + c.Px(2), struck);
                rows.Add((rect, deal));
                y += imageHeight + c.Px(8);
            }
            c.G.Restore(state);
            maxScroll = Math.Max(0, y + scroll - c.Area.Bottom);
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
            if (maxScroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 60, 0, maxScroll);
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.SteamIdMenu, null, (_, _) =>
            {
                using var dialog = new InputDialog(Strings.WidgetSteamDeals, Strings.SteamIdPrompt, getOption() ?? "");
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;
                var value = dialog.Value.Trim();
                setOption(Regex.IsMatch(value, @"^7656\d{13}$") ? value : null);
                nextFetch = DateTime.MinValue;
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
