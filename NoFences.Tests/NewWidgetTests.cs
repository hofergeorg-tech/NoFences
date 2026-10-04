using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class WeatherTests
    {
        private const string Report = """
            {"current":{"time":"2026-10-03T14:00","temperature_2m":14.6,"apparent_temperature":12.9,"weather_code":61,"wind_speed_10m":18.2,"is_day":1},
             "daily":{"time":["2026-10-03","2026-10-04","2026-10-05"],"weather_code":[61,2,null],
                      "temperature_2m_max":[15.1,17.4,16.0],"temperature_2m_min":[8.2,7.9,6.5]}}
            """;

        [Fact]
        public void ParseReport_ReadsCurrentAndSkipsIncompleteDays()
        {
            var r = WeatherService.ParseReport(Report);
            Assert.Equal(14.6, r.Temperature);
            Assert.Equal(12.9, r.FeelsLike);
            Assert.Equal(WeatherKind.Rain, r.Kind);
            Assert.True(r.IsDay);
            Assert.Equal(2, r.Days.Count);
            Assert.Equal(new DateTime(2026, 10, 4), r.Days[1].Date);
            Assert.Equal(WeatherKind.PartlyCloudy, r.Days[1].Kind);
            Assert.Equal(17.4, r.Days[1].Max);
        }

        [Fact]
        public void ParsePlaces_HandlesResultsAndNoResults()
        {
            var places = WeatherService.ParsePlaces("""
                {"results":[{"name":"Wien","latitude":48.20849,"longitude":16.37208,"country":"Österreich","admin1":"Wien"}]}
                """);
            var wien = Assert.Single(places);
            Assert.Equal("Wien", wien.Name);
            Assert.Equal("Wien, Österreich", wien.ToString());
            Assert.Empty(WeatherService.ParsePlaces("""{"generationtime_ms":0.5}"""));
        }

        [Fact]
        public void PlaceOption_RoundTripsInvariantly()
        {
            var place = new WeatherPlace("Sankt Pölten", 48.2047, 15.6256);
            var back = WeatherPlace.FromOption(place.ToOption());
            Assert.Equal("48.2047|15.6256|Sankt Pölten", place.ToOption());
            Assert.Equal(place, back);
            Assert.Null(WeatherPlace.FromOption("nonsense"));
            Assert.Null(WeatherPlace.FromOption(null));
        }

        [Theory]
        [InlineData(0, WeatherKind.Clear)]
        [InlineData(3, WeatherKind.Cloudy)]
        [InlineData(45, WeatherKind.Fog)]
        [InlineData(53, WeatherKind.Drizzle)]
        [InlineData(81, WeatherKind.Rain)]
        [InlineData(75, WeatherKind.Snow)]
        [InlineData(86, WeatherKind.Snow)]
        [InlineData(96, WeatherKind.Thunder)]
        public void KindOf_MapsWmoCodes(int code, WeatherKind kind) => Assert.Equal(kind, WeatherService.KindOf(code));
    }

    public class ClipboardHistoryTests
    {
        [Fact]
        public void Add_PutsNewestFirstWithoutDuplicatesAndKeepsTheLimit()
        {
            var h = new ClipboardHistory(3);
            foreach (var t in new[] { "a", "b", "c", "a", "  ", "d" })
                h.Add(t);
            Assert.Equal(new[] { "d", "a", "c" }, h.Texts);
        }

        [Fact]
        public void PasswordManagerFormats_AreExcluded()
        {
            Assert.True(ClipboardHistory.IsExcluded(new[] { "UnicodeText", "ExcludeClipboardContentFromMonitorProcessing" }));
            Assert.False(ClipboardHistory.IsExcluded(new[] { "UnicodeText", "Text" }));
        }

        [Fact]
        public void OneLine_ShowsFirstLineCompact() =>
            Assert.Equal("first line …", ClipboardHistory.OneLine("  first   line\r\nsecond"));
    }

    public class SmallWidgetTests
    {
        [Theory]
        [InlineData("Spotify.exe", "Spotify")]
        [InlineData("MSEdge", "Edge")]
        [InlineData("308046B0AF4A39CB", "Firefox")]
        [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", "Media Player")]
        [InlineData("ABCDEF0123", "")]
        [InlineData(null, "")]
        public void Media_AppName(string? id, string expected) => Assert.Equal(expected, MediaWidget.AppName(id));

        [Fact]
        public void Media_FormatTime()
        {
            Assert.Equal("3:07", MediaWidget.FormatTime(TimeSpan.FromSeconds(187)));
            Assert.Equal("1:02:03", MediaWidget.FormatTime(new TimeSpan(1, 2, 3)));
        }

        [Fact]
        public void Battery_FormatRemaining()
        {
            Assert.Equal("", BatteryWidget.FormatRemaining(-1));
            Assert.Equal("45 min", BatteryWidget.FormatRemaining(45 * 60));
            Assert.Equal("2 h 15 min", BatteryWidget.FormatRemaining(135 * 60));
        }

        [Fact]
        public void Registry_EveryWidgetIsInExactlyOneMenuGroup()
        {
            var grouped = WidgetRegistry.Groups.SelectMany(g => g.Types).ToList();
            Assert.Equal(grouped.Count, grouped.Distinct().Count());
            Assert.Equal(WidgetRegistry.Types.Select(t => t.Type).OrderBy(t => t), grouped.OrderBy(t => t));
        }

        [Fact]
        public void Registry_CreatesEveryOfferedWidget()
        {
            var host = new PreviewRenderer.Host();
            foreach (var (type, _, _) in WidgetRegistry.Types)
            {
                if (type is "clipboard" or "media")
                    continue; // need a message loop / the WinRT media service
                using var widget = WidgetRegistry.Create(new FenceInfo { Kind = FenceKind.Widget, WidgetType = type }, host);
                Assert.Equal(type, widget?.Type);
            }
        }
    }

    public class ProfileTests
    {
        [Fact]
        public void InProfile_FencesWithoutProfileShowEverywhere()
        {
            var always = new FenceInfo();
            var gaming = new FenceInfo { Profiles = new() { "Gaming" } };
            Assert.True(always.InProfile(null));
            Assert.True(always.InProfile("Work"));
            Assert.True(gaming.InProfile(null));
            Assert.True(gaming.InProfile("Gaming"));
            Assert.False(gaming.InProfile("Work"));
        }
    }
}
