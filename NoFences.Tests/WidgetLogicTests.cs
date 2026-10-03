using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class PlaytimeTests
    {
        private static double Unix(DateTime local) => new DateTimeOffset(local).ToUnixTimeSeconds();

        private static PlaySession Session(DateTime start, TimeSpan length, string channel = "LIVE") =>
            new(channel, Unix(start), Unix(start + length));

        [Fact]
        public void Totals_CountTodayWeekMonthAndAll()
        {
            var now = new DateTime(2026, 10, 3, 18, 0, 0); // Saturday
            var sessions = new[]
            {
                Session(new DateTime(2026, 10, 3, 14, 0, 0), TimeSpan.FromHours(2)),   // today
                Session(new DateTime(2026, 9, 29, 20, 0, 0), TimeSpan.FromHours(3)),   // Tuesday this week, last month
                Session(new DateTime(2026, 9, 10, 20, 0, 0), TimeSpan.FromHours(5)),   // earlier in September
            };
            var s = PlaytimeSummary.Compute(sessions, now);
            Assert.Equal(TimeSpan.FromHours(2), s.Today);
            Assert.Equal(TimeSpan.FromHours(5), s.Week);   // Monday 28 Sept onwards
            Assert.Equal(TimeSpan.FromHours(2), s.Month);  // October only
            Assert.Equal(TimeSpan.FromHours(10), s.Total);
            Assert.False(s.Live);
            Assert.Equal(3, s.Days);
        }

        [Fact]
        public void SessionOverMidnight_CountsOnlyTheTodayPart()
        {
            var now = new DateTime(2026, 10, 3, 1, 0, 0);
            var s = PlaytimeSummary.Compute(new[] { Session(new DateTime(2026, 10, 2, 23, 0, 0), TimeSpan.FromHours(1.5)) }, now);
            Assert.Equal(TimeSpan.FromMinutes(30), s.Today);
            Assert.Equal(TimeSpan.FromMinutes(90), s.Total);
        }

        [Fact]
        public void RecentlyUpdatedSession_IsLive()
        {
            var now = new DateTime(2026, 10, 3, 18, 0, 0);
            var s = PlaytimeSummary.Compute(new[] { Session(now.AddMinutes(-42), TimeSpan.FromMinutes(41.5), "PTU") }, now);
            Assert.True(s.Live);
            Assert.Equal("PTU", s.LiveChannel);
            Assert.InRange(s.LiveFor.TotalMinutes, 41.9, 42.1);
        }

        [Theory]
        [InlineData(0, "0m")]
        [InlineData(45, "45m")]
        [InlineData(187, "3h 07m")]
        [InlineData(7695, "128h 15m")]
        public void Format_ShowsHoursAndMinutes(int minutes, string expected)
        {
            Assert.Equal(expected, PlaytimeSummary.Format(TimeSpan.FromMinutes(minutes)));
        }
    }

    public class SizeFormatTests
    {
        [Theory]
        [InlineData(500, "500 B")]
        [InlineData(1536, "1,5 KB")]
        [InlineData(13_600_000_000, "12,7 GB")]
        [InlineData(602_000_000_000, "561 GB")]
        public void FormatSize_UsesBinaryUnits(long bytes, string expected)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-AT");
            try
            {
                Assert.Equal(expected, DrivesWidget.FormatSize(bytes));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = culture;
            }
        }
    }

    public class ExportTests
    {
        [Fact]
        public void ExportImport_RoundTripsFencesAndStyles()
        {
            using var dir = new TempFolder();
            dir.File(@"themes\mine.json", "{ \"id\": \"mine\" }");
            var original = new FenceInfo
            {
                Name = "Spiele",
                Kind = FenceKind.Links,
                Files = { @"C:\Games\a.lnk" },
                Tabs = { new FenceTab { Name = "A" }, new FenceTab { Name = "B", Files = { @"C:\b.lnk" } } },
                Layouts = { ["0,0,1920,1080"] = new[] { 1, 2, 3, 4 } },
                VirtualDesktop = Guid.NewGuid()
            };

            var json = FenceExport.Create(new[] { original }, Path.Combine(dir.Path, "themes")).ToJson();
            var imported = FenceExport.FromJson(json);
            var fence = Assert.Single(imported.PrepareForImport());

            Assert.NotEqual(original.Id, fence.Id);              // new id, importing twice is fine
            Assert.Equal("Spiele", fence.Name);
            Assert.Equal(@"C:\Games\a.lnk", Assert.Single(fence.Files));
            Assert.Equal(2, fence.Tabs.Count);
            Assert.Equal(@"C:\b.lnk", Assert.Single(fence.Tabs[1].Files));
            Assert.Empty(fence.Layouts);                          // other PC, other screens
            Assert.Null(fence.VirtualDesktop);

            var target = Path.Combine(dir.Path, "other-themes");
            Assert.Equal(1, imported.WriteThemes(target));
            Assert.Equal(0, imported.WriteThemes(target));        // existing styles are not overwritten
        }

        [Fact]
        public void FromJson_RejectsOtherFiles()
        {
            Assert.ThrowsAny<Exception>(() => FenceExport.FromJson("{ \"Fences\": [] }"));
            Assert.ThrowsAny<Exception>(() => FenceExport.FromJson("not json"));
        }
    }
}
