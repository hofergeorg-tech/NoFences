using System.Drawing;
using NoFences.Model;
using NoFences.Themes;
using NoFences.Util;

namespace NoFences.Tests
{
    public class SnapperTests
    {
        private static readonly Rectangle[] Screens = { new(0, 0, 1920, 1040) };

        [Fact]
        public void SnapMove_PullsToScreenEdgeWithinThreshold()
        {
            var offset = Snapper.SnapMove(new Rectangle(7, 300, 200, 200), Array.Empty<Rectangle>(), Screens, threshold: 12, gap: 8);
            Assert.Equal(new Point(-7, 0), offset);
        }

        [Fact]
        public void SnapMove_IgnoresEdgesFurtherAway()
        {
            var offset = Snapper.SnapMove(new Rectangle(40, 300, 200, 200), Array.Empty<Rectangle>(), Screens, threshold: 12, gap: 8);
            Assert.Equal(Point.Empty, offset);
        }

        [Fact]
        public void SnapMove_LinesUpNextToAnotherFenceWithGap()
        {
            var other = new Rectangle(100, 100, 300, 300); // right edge at 400
            var moving = new Rectangle(405, 120, 200, 200);
            var offset = Snapper.SnapMove(moving, new[] { other }, Screens, threshold: 12, gap: 8);
            Assert.Equal(400 + 8, moving.Left + offset.X);
        }

        [Fact]
        public void SnapMove_DoesNotSnapToFencesFarAboveOrBelow()
        {
            var other = new Rectangle(100, 900, 300, 100);
            var moving = new Rectangle(405, 100, 200, 200);
            var offset = Snapper.SnapMove(moving, new[] { other }, Screens, threshold: 12, gap: 8);
            Assert.Equal(0, offset.X);
        }

        [Fact]
        public void SnapEdge_SnapsOnlyTheDraggedEdge()
        {
            var moving = new Rectangle(100, 100, 300, 300);
            Assert.Equal(1040, Snapper.SnapEdge(1035, horizontal: false, moving, Array.Empty<Rectangle>(), Screens, 12, 8));
            Assert.Equal(1000, Snapper.SnapEdge(1000, horizontal: false, moving, Array.Empty<Rectangle>(), Screens, 12, 8));
        }
    }

    public class FenceEntrySortTests
    {
        [Fact]
        public void Sort_PutsFoldersFirstAndOrdersByKey()
        {
            using var dir = new TempFolder();
            var b = dir.File("b.txt", new string('x', 10), DateTime.Now.AddDays(-1));
            var a = dir.File("a.pdf", new string('x', 500), DateTime.Now.AddDays(-3));
            var c = dir.File("c.docx", new string('x', 50), DateTime.Now);
            var folder = Path.Combine(dir.Path, "zordner");
            Directory.CreateDirectory(folder);
            var entries = new[] { b, folder, a, c }.Select(FenceEntry.FromPath).OfType<FenceEntry>().ToList();

            string[] Names(FenceSortMode mode) => FenceEntry.Sort(entries, mode).Select(e => Path.GetFileName(e.Path)).ToArray();

            Assert.Equal(new[] { "b.txt", "zordner", "a.pdf", "c.docx" }, Names(FenceSortMode.Manual));
            Assert.Equal(new[] { "zordner", "a.pdf", "b.txt", "c.docx" }, Names(FenceSortMode.Name));
            Assert.Equal(new[] { "zordner", "c.docx", "a.pdf", "b.txt" }, Names(FenceSortMode.Type));
            Assert.Equal(new[] { "zordner", "a.pdf", "c.docx", "b.txt" }, Names(FenceSortMode.Size));
            Assert.Equal(new[] { "zordner", "c.docx", "b.txt", "a.pdf" }, Names(FenceSortMode.Modified));
        }

        [Fact]
        public void DisplayName_HidesShortcutExtensionsAlways()
        {
            using var dir = new TempFolder();
            var lnk = FenceEntry.FromPath(dir.File("Spiel.lnk"))!;
            var txt = FenceEntry.FromPath(dir.File("Notiz.txt"))!;
            Assert.Equal("Spiel", lnk.GetDisplayName(showExtensions: true));
            Assert.Equal("Notiz.txt", txt.GetDisplayName(showExtensions: true));
            Assert.Equal("Notiz", txt.GetDisplayName(showExtensions: false));
        }
    }

    public class UpdateCheckerTests
    {
        private static ReleaseInfo Release(Version v) => new(v, "v" + v, "https://example", null, 0);

        [Fact]
        public void IsNewer_ComparesWithRunningVersion()
        {
            var current = UpdateChecker.CurrentVersion;
            Assert.True(UpdateChecker.IsNewer(Release(new Version(current.Major, current.Minor + 1, 0))));
            Assert.False(UpdateChecker.IsNewer(Release(current)));
            Assert.False(UpdateChecker.IsNewer(Release(new Version(1, 0, 0))));
        }

        [Fact]
        public void FinishUpdate_PassesOtherArgumentsThrough()
        {
            Assert.Equal(new[] { "--preview", "x" }, UpdateChecker.FinishUpdate(new[] { "--preview", "x" }));
            Assert.Equal(new[] { "--preview", "x" }, UpdateChecker.FinishUpdate(new[] { "--after-update", "999999", "--preview", "x" }));
        }
    }

    public class ThemeTests
    {
        [Fact]
        public void ParseColor_ReadsRgbAndRgba()
        {
            Assert.Equal(Color.FromArgb(255, 0x1E, 0x1E, 0x2E), JsonTheme.ParseColor("#1E1E2E"));
            Assert.Equal(Color.FromArgb(0x80, 0xCB, 0xA6, 0xF7), JsonTheme.ParseColor("#CBA6F780"));
        }

        [Theory]
        [InlineData("red")]
        [InlineData("#12345")]
        [InlineData("#GGGGGG")]
        public void ParseColor_RejectsInvalidValues(string value)
        {
            Assert.Throws<FormatException>(() => JsonTheme.ParseColor(value));
        }

        [Fact]
        public void ExampleStyle_Parses()
        {
            var theme = JsonTheme.Parse(JsonTheme.ExampleJson, "fallback");
            Assert.Equal("example-mocha", theme.Id);
        }

        [Fact]
        public void LoadFolder_ReportsBrokenFilesAndKeepsGoodOnes()
        {
            using var dir = new TempFolder();
            dir.File("good.json", JsonTheme.ExampleJson);
            dir.File("bad.json", "{ \"background\": \"blue\" }");
            var (themes, errors) = JsonTheme.LoadFolder(dir.Path);
            Assert.Single(themes);
            Assert.Single(errors);
        }

        [Fact]
        public void Registry_HasUniqueIds_AndUserStylesCannotReplaceBuiltIns()
        {
            Assert.Equal(ThemeRegistry.All.Count, ThemeRegistry.All.Select(t => t.Id).Distinct().Count());
            ThemeRegistry.SetCustom(new[] { JsonTheme.Parse("""{ "id": "nerd", "name": "Fake" }""", "x") });
            Assert.IsNotType<JsonTheme>(ThemeRegistry.Get("nerd"));
            ThemeRegistry.SetCustom(Array.Empty<FenceTheme>());
        }
    }
}
