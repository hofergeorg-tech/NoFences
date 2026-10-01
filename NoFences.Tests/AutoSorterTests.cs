using NoFences.Model;

namespace NoFences.Tests
{
    public class AutoSorterTests
    {
        [Theory]
        [InlineData("Rechnung.pdf", "*.pdf; *.docx", true)]
        [InlineData("RECHNUNG.PDF", "*.pdf", true)]
        [InlineData("Brief.docx", "*.pdf;*.docx", true)]
        [InlineData("Foto.jpg", "*.pdf; *.docx", false)]
        [InlineData("Rechnung.pdf", "", false)]
        [InlineData("Rechnung.pdf", null, false)]
        [InlineData("Scan 2026-10.pdf", "Scan*", true)]
        public void Matches_UsesWildcards(string file, string? patterns, bool expected)
        {
            Assert.Equal(expected, AutoSorter.Matches(file, patterns));
        }

        [Theory]
        [InlineData("download.pdf.crdownload")]
        [InlineData("video.mp4.part")]
        [InlineData("~$Brief.docx")]
        [InlineData("desktop.ini")]
        public void Matches_IgnoresUnfinishedAndSystemFiles(string file)
        {
            Assert.False(AutoSorter.Matches(file, "*"));
        }

        [Fact]
        public void Presets_AreValidPatterns()
        {
            foreach (var (_, patterns) in AutoSorter.Presets)
                Assert.False(string.IsNullOrWhiteSpace(patterns));
            Assert.True(AutoSorter.Matches("bild.png", AutoSorter.Presets[0].Patterns));
        }
    }
}
