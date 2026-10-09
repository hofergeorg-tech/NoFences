using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class CountdownTests
    {
        [Fact]
        public void Option_RoundTrips()
        {
            var option = CountdownWidget.Format(new DateTime(2026, 12, 24, 18, 0, 0), "Weihnachten");
            var parsed = CountdownWidget.Parse(option);
            Assert.Equal((new DateTime(2026, 12, 24, 18, 0, 0), "Weihnachten"), parsed);
            Assert.Null(CountdownWidget.Parse(null));
            Assert.Null(CountdownWidget.Parse("nonsense"));
        }

        [Theory]
        [InlineData(12 * 24 * 60 + 30, "12 days")]
        [InlineData(28 * 60, "1 day 4 h")]
        [InlineData(4 * 60 + 12, "4 h 12 min")]
        [InlineData(12, "12 min 0 s")]
        [InlineData(0, "")]
        public void Remaining_UsesTheCoarsestUsefulUnits(int minutes, string expected)
        {
            var language = NoFences.Util.Strings.Language;
            NoFences.Util.Strings.Language = "en";
            try
            {
                Assert.Equal(expected, CountdownWidget.Remaining(TimeSpan.FromMinutes(minutes)));
            }
            finally
            {
                NoFences.Util.Strings.Language = language;
            }
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
                Assert.Equal(expected, NoFences.Util.ByteSize.Format(bytes));
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
