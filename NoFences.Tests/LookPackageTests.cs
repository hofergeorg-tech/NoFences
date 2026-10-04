using System.Drawing;
using NoFences.Themes;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class LookPackageTests
    {
        [Fact]
        public void HighContrast_IsABasicSolidStyle()
        {
            var theme = ThemeRegistry.Get("contrast");
            Assert.IsType<HighContrastTheme>(theme);
            Assert.Equal(ThemeRegistry.Group.Basic, ThemeRegistry.GroupOf(theme));
            Assert.Equal(255, theme.MinAlpha);
            Assert.False(theme.Glass);
            using var font = theme.CreateLabelFont(1);
            Assert.True(font.Size >= 14 && font.Bold);
        }

        [Fact]
        public void Designer_DefinitionRoundTripsThroughJson()
        {
            var def = new JsonTheme.Definition { Id = "mein-blau", Name = "Mein Blau", Background = JsonTheme.Hex(Color.FromArgb(10, 20, 200)), TitleBackground = JsonTheme.Hex(Color.FromArgb(128, 0, 0, 0)), CornerRadius = 12 };
            var theme = JsonTheme.Parse(JsonTheme.ToJson(def), "fallback");
            Assert.Equal("mein-blau", theme.Id);
            Assert.Equal("#0A14C8", theme.Source.Background);
            Assert.Equal("#00000080", theme.Source.TitleBackground);
            Assert.Equal(12, theme.Source.CornerRadius);
        }

        [Theory]
        [InlineData("Mein Blau", "mein-blau")]
        [InlineData("Grün & Gold!", "gruen-gold")]
        [InlineData("  ", "style")]
        public void Designer_IdsFromNames(string name, string id) => Assert.Equal(id, JsonTheme.IdFor(name));

        [Theory]
        [InlineData("example.com", "https://example.com/")]
        [InlineData("http://192.168.1.10:8123/lovelace", "http://192.168.1.10:8123/lovelace")]
        [InlineData("ftp://example.com", null)]
        [InlineData("", null)]
        public void WebPage_NormalizesAddresses(string input, string? expected) => Assert.Equal(expected, WebPageWidget.NormalizeUrl(input));

        [Fact]
        public void WebPage_OptionsAndOldPlainUrls()
        {
            Assert.Equal("https://a.example", WebPageWidget.Parse("https://a.example").Url);
            var s = WebPageWidget.Parse("{\"Url\":\"https://b.example\",\"Seconds\":300,\"Zoom\":75}");
            Assert.Equal(300, s.Seconds);
            Assert.Equal(75, s.Zoom);
            Assert.Equal("", WebPageWidget.Parse(null).Url);
        }

        [Fact]
        public void Steam_AccountFromIdOrShareLink()
        {
            Assert.Equal(("76561197960287930", (string?)null), SteamDealsWidget.ParseAccount(" 76561197960287930 "));
            Assert.Equal(("76561197960287930", "123456789"), SteamDealsWidget.ParseAccount("https://store.steampowered.com/wishlist/profiles/76561197960287930/?st=123456789"));
            Assert.Equal(("76561197960287930", (string?)null), SteamDealsWidget.ParseAccount("https://store.steampowered.com/wishlist/profiles/76561197960287930/"));
            Assert.Equal(((string?)null, (string?)null), SteamDealsWidget.ParseAccount("hello"));
        }

        [Fact]
        public void Steam_GamesFromSharedWishlistPage()
        {
            const string html = "x{\\\"items\\\":[{\\\"appid\\\":1297900,\\\"priority\\\":1},{\\\"appid\\\":1511480,\\\"priority\\\":2}]} {\"appid\":42,\"name\":\"not on the list\"}";
            Assert.Equal(new[] { 1297900, 1511480 }, SteamDealsWidget.ParseSharedWishlist(html));
        }

        [Fact]
        public void Steam_StoreSearchRows()
        {
            const string html = """
                <a href="https://store.steampowered.com/app/1091500/x/" data-ds-appid="1091500" class="search_result_row">
                  <div class="search_capsule"><img src="https://cdn/capsule.jpg" ></div><span class="title">Cyberpunk&amp;2077</span>
                  <div class="search_price_discount_combined" data-price-final="1799"><div class="discount_block" data-price-final="1799" data-discount="70"></div></div></a>
                <a href="https://store.steampowered.com/app/5/y/" data-ds-appid="5"><span class="title">Full price</span><div data-price-final="999"></div></a>
                """;
            var all = SteamDealsWidget.ParseSearchResults(html, "EUR", discountedOnly: false);
            Assert.Equal(2, all.Count);
            var deal = all[0];
            Assert.Equal(("Cyberpunk&2077", 70, 1799, 5997, "https://cdn/capsule.jpg"), (deal.Name, deal.DiscountPercent, deal.FinalCents, deal.OriginalCents, deal.ImageUrl));
            Assert.Equal(999, all[1].OriginalCents);
            Assert.Single(SteamDealsWidget.ParseSearchResults(html, "EUR", discountedOnly: true));
            Assert.False(SteamDealsWidget.Matches(deal, new SteamDealsWidget.Options { MinDiscount = 75 }));
            Assert.True(SteamDealsWidget.Matches(deal, new SteamDealsWidget.Options { MaxPrice = 18 }));
        }
    }
}