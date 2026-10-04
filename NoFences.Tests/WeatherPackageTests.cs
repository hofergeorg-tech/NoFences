using NoFences.Widgets;

namespace NoFences.Tests
{
    public class WeatherPackageTests
    {
        private static List<(DateTime, double)> Slots(DateTime start, params double[] mm) =>
            mm.Select((v, i) => (start.AddMinutes(15 * i), v)).ToList();

        [Fact]
        public void Rain_StartsSoon()
        {
            var start = new DateTime(2026, 10, 4, 8, 0, 0);
            var outlook = RainForecast.Outlook(Slots(start, 0, 0, 0.4, 1.2), start.AddMinutes(7));
            Assert.Equal(new RainOutlook(true, 25), outlook);
        }

        [Fact]
        public void Rain_StopsSoon()
        {
            var start = new DateTime(2026, 10, 4, 8, 0, 0);
            Assert.Equal(new RainOutlook(false, 30), RainForecast.Outlook(Slots(start, 0.8, 0.5, 0.05, 0), start));
        }

        [Fact]
        public void Rain_NoChangeNoHint()
        {
            var start = new DateTime(2026, 10, 4, 8, 0, 0);
            Assert.Null(RainForecast.Outlook(Slots(start, 0, 0, 0.02, 0), start));
            Assert.Null(RainForecast.Outlook(Slots(start, 1, 1, 1), start));
            Assert.Null(RainForecast.Outlook(null, start));
        }

        [Theory]
        [InlineData(2024, 1, 11, 12, 0)] // new moon 11 Jan 2024, 11:57 UTC
        [InlineData(2024, 1, 25, 18, 4)] // full moon 25 Jan 2024, 17:54 UTC
        [InlineData(2025, 9, 7, 18, 4)]  // full moon 7 Sep 2025 (lunar eclipse)
        [InlineData(2025, 9, 21, 19, 0)] // new moon 21 Sep 2025
        public void MoonPhase_MatchesKnownDates(int y, int m, int d, int hour, int index)
        {
            var phase = MoonPhase.Of(new DateTime(y, m, d, hour, 0, 0, DateTimeKind.Utc));
            Assert.Equal(index, MoonPhase.Index(phase));
            if (index == 0)
                Assert.True(MoonPhase.Illumination(phase) < 0.03);
            else
                Assert.True(MoonPhase.Illumination(phase) > 0.97);
        }

        [Fact]
        public void Report_ParsesRainAndSun()
        {
            const string json = """
                {"utc_offset_seconds":7200,
                 "current":{"temperature_2m":10.5,"apparent_temperature":9,"weather_code":61,"wind_speed_10m":5,"is_day":1},
                 "minutely_15":{"time":["2026-10-04T08:00","2026-10-04T08:15"],"precipitation":[0.0,0.7]},
                 "daily":{"time":["2026-10-04"],"weather_code":[61],"temperature_2m_max":[14],"temperature_2m_min":[8],
                          "sunrise":["2026-10-04T06:57"],"sunset":["2026-10-04T18:27"]}}
                """;
            var r = WeatherService.ParseReport(json);
            Assert.Equal(2, r.Precipitation!.Count);
            Assert.Equal(0.7, r.Precipitation[1].Mm);
            Assert.Equal(new DateTime(2026, 10, 4, 6, 57, 0), r.Sunrise);
            Assert.Equal(new DateTime(2026, 10, 4, 18, 27, 0), r.Sunset);
            Assert.Equal(TimeSpan.FromHours(2), r.UtcOffset);
        }

        [Fact]
        public void Report_OldAnswersStillWork()
        {
            const string json = """
                {"current":{"temperature_2m":10.5,"apparent_temperature":9,"weather_code":0,"wind_speed_10m":5},
                 "daily":{"time":["2026-10-04"],"weather_code":[0],"temperature_2m_max":[14],"temperature_2m_min":[8]}}
                """;
            var r = WeatherService.ParseReport(json);
            Assert.Null(r.Precipitation);
            Assert.Null(r.Sunrise);
        }
    }
}
