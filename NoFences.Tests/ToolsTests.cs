using NoFences.Model;
using NoFences.Util;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class CalculatorTests
    {
        [Theory]
        [InlineData("12*7", 84)]
        [InlineData("2+3*4", 14)]
        [InlineData("(2+3)*4", 20)]
        [InlineData("2^10", 1024)]
        [InlineData("1,5 + 2", 3.5)]
        [InlineData("200*15%", 30)]
        [InlineData("10 x 3", 30)]
        [InlineData("-4+10", 6)]
        [InlineData("sqrt(81)+1", 10)]
        [InlineData("7 ÷ 2", 3.5)]
        public void Evaluates(string input, double expected)
        {
            Assert.True(Calculator.TryEvaluate(input, out var value));
            Assert.Equal(expected, value, 9);
        }

        [Theory]
        [InlineData("2024")]
        [InlineData("firefox")]
        [InlineData("1/0")]
        [InlineData("(2+3")]
        [InlineData("test-1")]
        public void IgnoresNonCalculations(string input) => Assert.False(Calculator.TryEvaluate(input, out _));
    }

    public class ExtendedSearchTests
    {
        [Fact]
        public void FenceItemsComeBeforeAppsAndSettings_AppsSkipFuzzy()
        {
            var items = new[]
            {
                new SearchItem("Steam (settings)", "ms-settings:x", "", null, SearchKind.Setting),
                new SearchItem("Steam", @"C:\start\Steam.lnk", "", null, SearchKind.App),
                new SearchItem("Steam", @"C:\fence\Steam.lnk", "Gaming", new FenceInfo()),
                new SearchItem("Something Than Ever Amazing Mode", @"C:\x.lnk", "", null, SearchKind.App),
            };
            var found = FenceSearch.Find(items, "steam");
            Assert.Equal(new[] { SearchKind.FenceItem, SearchKind.App, SearchKind.Setting }, found.Select(f => f.Kind));
        }

        [Fact]
        public void Calculation_IsOfferedForExpressionsOnly()
        {
            Assert.Equal(SearchKind.Calculation, FenceSearch.Calculation("6*7")?.Kind);
            Assert.Null(FenceSearch.Calculation("notepad"));
        }

        [Fact]
        public void Uninstallers_AreLeftOut()
        {
            Assert.True(FenceSearch.IsUninstaller("Uninstall Discord"));
            Assert.True(FenceSearch.IsUninstaller("Steam deinstallieren"));
            Assert.False(FenceSearch.IsUninstaller("Discord"));
        }
    }

    public class DesktopSorterTests
    {
        [Theory]
        [InlineData(@"C:\Users\x\Desktop\Elden Ring.url", false, "steam://rungameid/1245620", DesktopCategory.Games)]
        [InlineData(@"C:\Users\x\Desktop\RSI Launcher.lnk", false, @"C:\Program Files\Roberts Space Industries\RSI Launcher\RSI Launcher.exe", DesktopCategory.Games)]
        [InlineData(@"C:\Users\x\Desktop\Discord.lnk", false, @"C:\Users\x\AppData\Local\Discord\Update.exe --processStart Discord.exe", DesktopCategory.Programs)]
        [InlineData(@"C:\Users\x\Desktop\Rechnung.pdf", false, null, DesktopCategory.Documents)]
        [InlineData(@"C:\Users\x\Desktop\Urlaub.JPG", false, null, DesktopCategory.Images)]
        [InlineData(@"C:\Users\x\Desktop\song.flac", false, null, DesktopCategory.Media)]
        [InlineData(@"C:\Users\x\Desktop\backup.7z", false, null, DesktopCategory.Archives)]
        [InlineData(@"C:\Users\x\Desktop\Projekte", true, null, DesktopCategory.Folders)]
        [InlineData(@"C:\Users\x\Desktop\data.bin", false, null, DesktopCategory.Other)]
        public void Categorize(string path, bool folder, string? target, DesktopCategory expected) =>
            Assert.Equal(expected, DesktopSorter.Categorize(path, folder, target));
    }

    public class UsageLogTests
    {
        [Fact]
        public void Totals_SumDaysAndSortByUse()
        {
            var log = new UsageLog();
            var today = new DateTime(2026, 10, 3, 12, 0, 0);
            log.Add(today, @"C:\a.exe", "A", 600);
            log.Add(today, @"C:\b.exe", "B", 1200);
            log.Add(today.AddDays(-1), @"C:\a.exe", "A", 1800);
            Assert.Equal(new[] { ("B", 1200), ("A", 600) }, log.Totals(today, today).Select(t => (t.Name, t.Seconds)));
            Assert.Equal(new[] { ("A", 2400), ("B", 1200) }, log.Totals(today.AddDays(-6), today).Select(t => (t.Name, t.Seconds)));
        }

        [Fact]
        public void OldDays_AreDropped()
        {
            var log = new UsageLog();
            var now = new DateTime(2026, 10, 3);
            log.Add(now.AddDays(-60), @"C:\a.exe", "A", 60);
            log.Add(now, @"C:\a.exe", "A", 60);
            Assert.Single(log.Days);
        }
    }

    public class StatusTests
    {
        [Fact]
        public void Statuspage_SummaryWithProblems()
        {
            var json = """
                {"page":{"name":"Discord"},"status":{"indicator":"minor","description":"Partially Degraded Service"},
                 "components":[{"name":"API","status":"operational"},{"name":"Voice","status":"partial_outage"},
                               {"name":"Media Proxy","status":"degraded_performance"},{"name":"Group","status":"major_outage","group":true}]}
                """;
            var s = StatusWidget.ParseStatuspage(json, "https://discordstatus.com");
            Assert.Equal("Discord", s.Name);
            Assert.Equal(ServiceLevel.Degraded, s.Level);
            Assert.Equal(new[] { "Voice", "Media Proxy" }, s.Problems);
        }

        [Fact]
        public void CState_WorstSystemWins()
        {
            var json = """
                {"title":"RSI Status","systems":[{"name":"Platform","status":"operational"},{"name":"Persistent Universe","status":"disrupted"},
                 {"name":"Arena Commander","status":"operational"}]}
                """;
            var s = StatusWidget.ParseCState(json, "https://status.robertsspaceindustries.com");
            Assert.Equal("RSI", s.Name);
            Assert.Equal(ServiceLevel.Degraded, s.Level);
            Assert.Equal(new[] { "Persistent Universe" }, s.Problems);
        }
    }

    public class FocusModeTests
    {
        [Fact]
        public void Option_KeepsPresetAndProfile()
        {
            Assert.Equal((1, "Fokus"), FocusWidget.ParseOption(FocusWidget.FormatOption(1, "Fokus")));
            Assert.Equal((2, (string?)null), FocusWidget.ParseOption("2"));
            Assert.Equal((0, (string?)null), FocusWidget.ParseOption(null));
        }
    }
}
