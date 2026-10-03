using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class ProfileRuleTests
    {
        private static readonly string[] Profiles = { "Work", "Gaming" };
        private static readonly HashSet<string> Nothing = new(StringComparer.OrdinalIgnoreCase);

        private static ProfileRule Work => new() { Profile = "Work", Trigger = ProfileTrigger.Time, From = "08:00", To = "17:00" };
        private static ProfileRule Gaming => new() { Profile = "Gaming", Trigger = ProfileTrigger.Program, Program = @"C:\Games\EldenRing\eldenring.exe" };

        [Fact]
        public void TimeRule_AppliesOnItsDaysAndHours()
        {
            var rules = new[] { Work };
            Assert.Equal("Work", ProfileRules.Match(rules, new DateTime(2026, 10, 5, 9, 0, 0), Nothing, Profiles)); // Monday
            Assert.Null(ProfileRules.Match(rules, new DateTime(2026, 10, 5, 17, 0, 0), Nothing, Profiles));
            Assert.Null(ProfileRules.Match(rules, new DateTime(2026, 10, 4, 9, 0, 0), Nothing, Profiles)); // Sunday
        }

        [Fact]
        public void ProgramRule_WinsOverTimeRule()
        {
            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EldenRing" };
            Assert.Equal("Gaming", ProfileRules.Match(new[] { Work, Gaming }, new DateTime(2026, 10, 5, 9, 0, 0), running, Profiles));
        }

        [Fact]
        public void RangePastMidnight_BelongsToTheStartDay()
        {
            var night = new ProfileRule { Profile = "Gaming", Trigger = ProfileTrigger.Time, Days = new() { DayOfWeek.Friday }, From = "22:00", To = "02:00" };
            Assert.True(night.Matches(new DateTime(2026, 10, 9, 23, 0, 0), Nothing));  // Friday night
            Assert.True(night.Matches(new DateTime(2026, 10, 10, 1, 0, 0), Nothing));  // early Saturday
            Assert.False(night.Matches(new DateTime(2026, 10, 11, 1, 0, 0), Nothing)); // early Sunday
        }

        [Fact]
        public void RulesForDeletedProfiles_AreIgnored() =>
            Assert.Null(ProfileRules.Match(new[] { Work }, new DateTime(2026, 10, 5, 9, 0, 0), Nothing, new[] { "Gaming" }));

        [Fact]
        public void ThemeSchedule_ByTimeAndWindows()
        {
            Assert.True(ThemeSchedule.IsDark(AutoThemeMode.Time, new DateTime(2026, 10, 3, 22, 0, 0), true, "19:00", "07:00"));
            Assert.False(ThemeSchedule.IsDark(AutoThemeMode.Time, new DateTime(2026, 10, 3, 12, 0, 0), true, "19:00", "07:00"));
            Assert.True(ThemeSchedule.IsDark(AutoThemeMode.Windows, DateTime.Now, false, "", ""));
            Assert.False(ThemeSchedule.IsDark(AutoThemeMode.Off, DateTime.Now, false, "", ""));
        }
    }

    public class FenceSearchTests
    {
        private static SearchItem Item(string name) => new(name, @"C:\x\" + name, "Fence", new FenceInfo());

        [Fact]
        public void Find_RanksPrefixThenWordThenContainsThenFuzzy()
        {
            var items = new[] { Item("Mozilla Firefox"), Item("Firefox Developer"), Item("Paint"), Item("Unfirefoxed"), Item("Foxit Reader") };
            var names = FenceSearch.Find(items, "fire").Select(i => i.Name).ToList();
            // Prefix, word start, contains – then loose ("fuzzy") matches like F-oxit Reader
            Assert.Equal(new[] { "Firefox Developer", "Mozilla Firefox", "Unfirefoxed", "Foxit Reader" }, names);
            Assert.DoesNotContain(FenceSearch.Find(items, "fire"), i => i.Name == "Paint");
            Assert.Contains(FenceSearch.Find(items, "ffx"), i => i.Name == "Mozilla Firefox");
            Assert.Empty(FenceSearch.Find(items, "  "));
        }

        [Fact]
        public void Find_SearchesNoteTexts()
        {
            var note = new FenceInfo { Kind = FenceKind.Note, Name = "Einkauf", NoteText = "Milch\nBrot\nKaffee" };
            var items = FenceSearch.Collect(new[] { note }, false);
            Assert.Single(FenceSearch.Find(items, "kaffee"));
        }
    }

    public class GameLibraryTests
    {
        [Fact]
        public void Steam_LibraryFoldersAndManifest()
        {
            var vdf = "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"C:\\\\Program Files (x86)\\\\Steam\"\n\t}\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"D:\\\\SteamLibrary\"\n\t}\n}";
            Assert.Equal(new[] { @"C:\Program Files (x86)\Steam", @"D:\SteamLibrary" }, GameLibrary.ParseLibraryFolders(vdf));

            var acf = "\"AppState\"\n{\n\t\"appid\"\t\t\"1245620\"\n\t\"name\"\t\t\"ELDEN RING\"\n\t\"StateFlags\"\t\t\"4\"\n\t\"LastPlayed\"\t\t\"1790000000\"\n}";
            var game = GameLibrary.ParseAppManifest(acf, @"C:\nowhere");
            Assert.NotNull(game);
            Assert.Equal("ELDEN RING", game!.Name);
            Assert.Equal("steam://rungameid/1245620", game.Launch);
            Assert.True(game.LastPlayed > new DateTime(2026, 1, 1));
        }

        [Fact]
        public void Steam_SkipsToolsAndUnfinishedInstalls()
        {
            Assert.Null(GameLibrary.ParseAppManifest("\"appid\" \"228980\"\n\"name\" \"Steamworks Common Redistributables\"\n\"StateFlags\" \"4\"", ""));
            Assert.Null(GameLibrary.ParseAppManifest("\"appid\" \"10\"\n\"name\" \"Half-Done\"\n\"StateFlags\" \"1026\"", ""));
        }

        [Fact]
        public void Epic_Manifest()
        {
            var json = """
                {"DisplayName":"Rocket League","AppName":"Sugar","CatalogNamespace":"ns1","CatalogItemId":"item1",
                 "InstallLocation":"C:\\Nowhere","LaunchExecutable":"rl.exe","bIsIncompleteInstall":false,"AppCategories":["public","games"]}
                """;
            var game = GameLibrary.ParseEpicManifest(json);
            Assert.Equal("Rocket League", game!.Name);
            Assert.Equal("com.epicgames.launcher://apps/ns1%3Aitem1%3ASugar?action=launch&silent=true", game.Launch);
            Assert.Null(GameLibrary.ParseEpicManifest(json.Replace("\"games\"", "\"addons\"")));
        }

        [Fact]
        public void Arrange_RecentFirstAndHidesHidden()
        {
            var games = new[]
            {
                new GameInfo("a", "Alpha", GameSource.Gog, "", null, null),
                new GameInfo("b", "Beta", GameSource.Steam, "", null, null, new DateTime(2026, 9, 1)),
                new GameInfo("c", "Gamma", GameSource.Steam, "", null, null, new DateTime(2026, 10, 1)),
            };
            Assert.Equal(new[] { "Gamma", "Beta", "Alpha" }, GamesWidget.Arrange(games, new()).Select(g => g.Name));
            Assert.Equal(new[] { "Alpha", "Gamma" }, GamesWidget.Arrange(games, new() { SortByName = true, Hidden = { "b" } }).Select(g => g.Name));
        }
    }

    public class IcsTests
    {
        private const string Header = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\n";
        private const string Footer = "END:VCALENDAR\r\n";

        [Fact]
        public void SingleEvents_WithFoldingEscapesAndAllDay()
        {
            var ics = Header +
                "BEGIN:VEVENT\r\nUID:1\r\nDTSTART:20261005T090000\r\nDTEND:20261005T100000\r\nSUMMARY:Team\\, weekly\r\n  sync\r\nEND:VEVENT\r\n" +
                "BEGIN:VEVENT\r\nUID:2\r\nDTSTART;VALUE=DATE:20261006\r\nSUMMARY:Holiday\r\nEND:VEVENT\r\n" + Footer;
            var events = IcsCalendar.Parse(ics, new DateTime(2026, 10, 1), new DateTime(2026, 10, 15));
            Assert.Equal(2, events.Count);
            Assert.Equal("Team, weekly sync", events[0].Title);
            Assert.Equal(new DateTime(2026, 10, 5, 9, 0, 0), events[0].Start);
            Assert.True(events[1].AllDay);
        }

        [Fact]
        public void Weekly_WithExceptionAndMovedOccurrence()
        {
            // Every Monday and Wednesday at 18:00 since 2020; 7 Oct cancelled, 12 Oct moved to 19:00
            var ics = Header +
                "BEGIN:VEVENT\r\nUID:x\r\nDTSTART:20200106T180000\r\nDTEND:20200106T190000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO,WE\r\nEXDATE:20261007T180000\r\nSUMMARY:Training\r\nEND:VEVENT\r\n" +
                "BEGIN:VEVENT\r\nUID:x\r\nRECURRENCE-ID:20261012T180000\r\nDTSTART:20261012T190000\r\nDTEND:20261012T200000\r\nSUMMARY:Training (later)\r\nEND:VEVENT\r\n" + Footer;
            var events = IcsCalendar.Parse(ics, new DateTime(2026, 10, 5), new DateTime(2026, 10, 15));
            Assert.Equal(new[]
            {
                new DateTime(2026, 10, 5, 18, 0, 0), new DateTime(2026, 10, 12, 19, 0, 0), new DateTime(2026, 10, 14, 18, 0, 0)
            }, events.Select(e => e.Start));
            Assert.Equal("Training (later)", events[1].Title);
        }

        [Fact]
        public void Monthly_NthWeekdayAndCount()
        {
            // Second Tuesday of the month, 3 times from January 2026
            var ics = Header + "BEGIN:VEVENT\r\nUID:m\r\nDTSTART:20260113T100000\r\nRRULE:FREQ=MONTHLY;BYDAY=2TU;COUNT=3\r\nSUMMARY:Board\r\nEND:VEVENT\r\n" + Footer;
            var events = IcsCalendar.Parse(ics, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            Assert.Equal(new[] { new DateTime(2026, 1, 13, 10, 0, 0), new DateTime(2026, 2, 10, 10, 0, 0), new DateTime(2026, 3, 10, 10, 0, 0) }, events.Select(e => e.Start));
        }

        [Fact]
        public void Utc_IsConvertedToLocal()
        {
            var ics = Header + "BEGIN:VEVENT\r\nUID:u\r\nDTSTART:20261005T070000Z\r\nSUMMARY:Call\r\nEND:VEVENT\r\n" + Footer;
            var e = Assert.Single(IcsCalendar.Parse(ics, new DateTime(2026, 10, 1), new DateTime(2026, 10, 10)));
            Assert.Equal(new DateTime(2026, 10, 5, 7, 0, 0, DateTimeKind.Utc).ToLocalTime(), e.Start);
        }
    }

    public class FeedAndQuoteTests
    {
        [Fact]
        public void Rss_And_Atom()
        {
            var rss = """
                <?xml version="1.0"?><rss version="2.0"><channel><title>Daily</title>
                <item><title>First &amp; best</title><link>https://example.com/1</link><pubDate>Sat, 03 Oct 2026 10:00:00 GMT</pubDate></item>
                </channel></rss>
                """;
            var item = Assert.Single(NewsWidget.ParseFeed(rss));
            Assert.Equal("First & best", item.Title);
            Assert.Equal("Daily", item.Source);
            Assert.Equal(new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc).ToLocalTime(), item.Published);

            var atom = """
                <feed xmlns="http://www.w3.org/2005/Atom"><title>Tech</title>
                <entry><title>Atom entry</title><link rel="alternate" href="https://example.com/a"/><updated>2026-10-03T08:00:00Z</updated></entry></feed>
                """;
            var entry = Assert.Single(NewsWidget.ParseFeed(atom));
            Assert.Equal("https://example.com/a", entry.Link);
        }

        [Fact]
        public void WebPage_InsteadOfFeed_IsRecognizedAndItsFeedFound()
        {
            var html = """
                <!DOCTYPE html><html lang="en"><head><title>Comm-Link</title>
                <link rel="canonical" href="https://example.com/en/comm-link">
                <link rel="alternate" type="application/rss+xml" title="RSS" href="/comm-link/rss?x=1&amp;y=2"/>
                </head><body></body></html>
                """;
            Assert.True(NewsWidget.LooksLikeHtml(html));
            Assert.False(NewsWidget.LooksLikeHtml("<?xml version=\"1.0\"?><rss version=\"2.0\"></rss>"));
            Assert.Equal("https://example.com/comm-link/rss?x=1&y=2", NewsWidget.DiscoverFeed(html, "https://example.com/en/comm-link"));
            Assert.Null(NewsWidget.DiscoverFeed("<html><head></head></html>", "https://example.com/"));
        }

        [Theory]
        [InlineData("Hacker News: Front Page", "Hacker News")]
        [InlineData("heise online News", "heise online")]
        [InlineData("BBC News", "BBC News")]
        public void ShortSource(string feedTitle, string expected) => Assert.Equal(expected, NewsWidget.ShortSource(feedTitle));

        [Fact]
        public void YahooChart()
        {
            var json = """
                {"chart":{"result":[{"meta":{"currency":"EUR","symbol":"BTC-EUR","shortName":"Bitcoin EUR","regularMarketPrice":110.0,"chartPreviousClose":100.0},
                "indicators":{"quote":[{"close":[100.0,null,105.5,110.0]}]}}],"error":null}}
                """;
            var q = TickerWidget.ParseChart(json, "BTC-EUR");
            Assert.Equal("Bitcoin EUR", q!.Name);
            Assert.Equal(0.1, q.Change, 6);
            Assert.Equal(3, q.Day.Count);
        }

        [Fact]
        public void FocusPhases_LongBreakAfterFourRounds()
        {
            Assert.Equal((FocusWidget.Phase.ShortBreak, 1), FocusWidget.After(FocusWidget.Phase.Focus, 1));
            Assert.Equal((FocusWidget.Phase.Focus, 2), FocusWidget.After(FocusWidget.Phase.ShortBreak, 1));
            Assert.Equal((FocusWidget.Phase.LongBreak, 4), FocusWidget.After(FocusWidget.Phase.Focus, 4));
            Assert.Equal((FocusWidget.Phase.Focus, 1), FocusWidget.After(FocusWidget.Phase.LongBreak, 4));
        }

        [Fact]
        public void PhotoOption_RoundTrip()
        {
            Assert.Equal((30, @"D:\Bilder"), PhotoWidget.Parse(PhotoWidget.Format(30, @"D:\Bilder")));
            Assert.Equal((60, (string?)null), PhotoWidget.Parse(null));
        }
    }
}
