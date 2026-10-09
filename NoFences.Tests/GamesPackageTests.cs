using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class GamesPackageTests
    {
        [Fact]
        public void SteamNews_Parse()
        {
            var json = """{"appnews":{"appid":10,"newsitems":[{"title":"Patch 1.2 &amp; more","url":"https://store.steampowered.com/news/1","date":1791000000}]}}""";
            var item = Assert.Single(NewsWidget.ParseSteamNews(json, "Game"));
            Assert.Equal("Patch 1.2 & more", item.Title);
            Assert.Equal("Game", item.Source);
        }

    }
}
