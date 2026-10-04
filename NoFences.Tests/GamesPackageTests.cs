using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class GamesPackageTests
    {
        [Fact]
        public void Playtime_PerLibraryGame()
        {
            var log = new PlaytimeLog();
            var key = PlaytimeLog.GameKey("steam:1245620");
            var start = new DateTime(2026, 10, 4, 18, 0, 0);
            log.RunningKey(key, start, start);
            log.RunningKey(key, start.AddMinutes(1), start);
            Assert.Equal(60, log.SessionsOfKey(key).Sum(s => s.End - s.Start));
            Assert.Empty(log.SessionsOf(@"C:\x\other.exe"));
        }

        [Fact]
        public void Games_SortByPlaytime()
        {
            var games = new[] { new GameInfo("a", "Alpha", GameSource.Steam, "", null, null), new GameInfo("b", "Beta", GameSource.Steam, "", null, null) };
            var played = new Dictionary<string, TimeSpan> { ["a"] = TimeSpan.FromHours(1), ["b"] = TimeSpan.FromHours(5) };
            var sorted = GamesWidget.Arrange(games, new GamesWidget.Options { SortByPlaytime = true }, g => played[g.Id]);
            Assert.Equal(new[] { "Beta", "Alpha" }, sorted.Select(g => g.Name));
        }

        [Fact]
        public void Steam_InstallDirFromManifest()
        {
            var acf = "\"appid\" \"10\"\n\"name\" \"Game\"\n\"StateFlags\" \"4\"\n\"installdir\" \"Game Folder\"";
            var game = GameLibrary.ParseAppManifest(acf, @"C:\Steam", @"D:\SteamLibrary");
            Assert.Equal(@"D:\SteamLibrary\steamapps\common\Game Folder", game!.InstallDir);
        }

        [Fact]
        public void SteamNews_Parse()
        {
            var json = """{"appnews":{"appid":10,"newsitems":[{"title":"Patch 1.2 &amp; more","url":"https://store.steampowered.com/news/1","date":1791000000}]}}""";
            var item = Assert.Single(NewsWidget.ParseSteamNews(json, "Game"));
            Assert.Equal("Patch 1.2 & more", item.Title);
            Assert.Equal("Game", item.Source);
        }

        [Theory]
        [InlineData("knebeltv is offline", false)]
        [InlineData("2 hours, 5 minutes, 10 seconds", true)]
        [InlineData("User not found: xyz", false)]
        public void Twitch_IsLive(string answer, bool live) => Assert.Equal(live, TwitchWidget.IsLive(answer));

        [Fact]
        public void Twitch_UptimeAndChannels()
        {
            Assert.Equal("2 h 5 min", TwitchWidget.ShortUptime("2 hours, 5 minutes, 10 seconds"));
            Assert.Equal("12 min", TwitchWidget.ShortUptime("12 minutes, 3 seconds"));
            Assert.Equal(new[] { "knebeltv", "shroud" }, TwitchWidget.Channels("https://www.twitch.tv/KnebelTV\nshroud, shroud\nbad-name!"));
        }

        [Fact]
        public void SteamDeals_CategoriesAndFilter()
        {
            var json = """
                {"top_sellers":{"items":[{"id":1,"name":"Full","discount_percent":0,"original_price":2999,"final_price":2999,"currency":"EUR"},
                                         {"id":2,"name":"Cheap","discount_percent":50,"original_price":1000,"final_price":500,"currency":"EUR"},
                                         {"id":3,"name":"No price","discount_percent":0}]},
                 "specials":{"items":[{"id":2,"name":"Cheap","discount_percent":50,"original_price":1000,"final_price":500,"currency":"EUR"},
                                      {"id":4,"name":"Free","discount_percent":0,"original_price":0,"final_price":0,"currency":"EUR"}]}}
                """;
            Assert.Equal(new[] { 1, 2 }, SteamDealsWidget.ParseCategory(json, "top_sellers").Select(d => d.AppId));
            Assert.Equal(new[] { 2 }, SteamDealsWidget.ParseCategory(json, "specials").Select(d => d.AppId));

            var all = SteamDealsWidget.ParseCategory(json, "top_sellers");
            Assert.Equal(new[] { 2 }, SteamDealsWidget.Filter(all, new SteamDealsWidget.Options { MinDiscount = 30 }).Select(d => d.AppId));
            Assert.Equal(new[] { 2 }, SteamDealsWidget.Filter(all, new SteamDealsWidget.Options { MaxPrice = 10 }).Select(d => d.AppId));
            Assert.Single(SteamDealsWidget.Filter(all, new SteamDealsWidget.Options { Count = 1 }));
        }
    }
}
