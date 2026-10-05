using NoFences.Model;

namespace NoFences.Tests
{
    [Collection(nameof(LanguageSwitching))]
    public class NoteListsTests
    {
        private const string List = "Einkauf\n[ ] Milch\n[x] Brot\n\t[x] Vollkorn\n\t[ ] Semmeln\n[ ] Käse\n[x] Butter";

        private static string[] Lines(string t) => t.Split('\n');

        [Theory]
        [InlineData("text", 0)]
        [InlineData("\ttext", 1)]
        [InlineData("  text", 1)]
        [InlineData("\t\t[ ] x", 2)]
        [InlineData("   x", 1)]
        public void Indent_CountsTabsAndSpacePairs(string line, int level) => Assert.Equal(level, NoteLists.Indent(line));

        [Fact]
        public void HasChildren_AndBlockEnd()
        {
            var lines = Lines(List);
            Assert.True(NoteLists.HasChildren(lines, 2));
            Assert.False(NoteLists.HasChildren(lines, 1));
            Assert.Equal(5, NoteLists.BlockEnd(lines, 2)); // Brot + its two sub-items
        }

        [Fact]
        public void Progress_CountsAllCheckboxes() => Assert.Equal((3, 6), NoteLists.Progress(List));

        [Fact]
        public void DisplayOrder_Normal_ShowsEverything() =>
            Assert.Equal(Enumerable.Range(0, 7), NoteLists.DisplayOrder(Lines(List), NoteDoneMode.Normal));

        [Fact]
        public void DisplayOrder_Bottom_MovesFinishedBlocksDown() =>
            Assert.Equal(new[] { 0, 1, 5, 2, 3, 4, 6 }, NoteLists.DisplayOrder(Lines(List), NoteDoneMode.Bottom));

        [Fact]
        public void DisplayOrder_Hidden_HidesFinishedItemsWithTheirSubItems() =>
            Assert.Equal(new[] { 0, 1, 5 }, NoteLists.DisplayOrder(Lines(List), NoteDoneMode.Hidden));

        [Fact]
        public void DisplayOrder_LeavesOutSubItemsOfFoldedLines() =>
            Assert.Equal(new[] { 0, 1, 2, 5, 6 }, NoteLists.DisplayOrder(Lines(List), NoteDoneMode.Normal, new[] { "Brot" }));

        [Fact]
        public void Reset_ClearsTicksAndCounters() =>
            Assert.Equal("[ ] a\n  [ ] b\nWasser [0/8] und [0/2]", NoteLists.Reset("[x] a\n  [X] b\nWasser [5/8] und [2/2]"));

        [Theory]
        [InlineData(null, Repeat.Daily, true)]
        [InlineData("2026-10-04 23:00", Repeat.Daily, true)]
        [InlineData("2026-10-05 06:00", Repeat.Daily, false)]
        [InlineData("2026-10-04 10:00", Repeat.Weekly, true)]   // Sunday → Monday is a new week
        [InlineData("2026-10-05 06:00", Repeat.Weekly, false)]
        [InlineData("2026-09-30 10:00", Repeat.Monthly, true)]
        [InlineData("2026-10-01 10:00", Repeat.Monthly, false)]
        [InlineData("2026-01-01 10:00", Repeat.None, false)]
        public void ResetDue_ByPeriod(string? last, Repeat repeat, bool due) =>
            Assert.Equal(due, NoteLists.ResetDue(last == null ? null : DateTime.Parse(last), repeat, new DateTime(2026, 10, 5, 9, 0, 0)));

        [Fact]
        public void StepCounter_CountsWithinTheGoal()
        {
            var text = "Wasser [7/8]\nSport [0/3] Dehnen [1/2]";
            Assert.Equal("Wasser [8/8]\nSport [0/3] Dehnen [1/2]", NoteLists.StepCounter(text, 0, 0, 1));
            Assert.Equal(text, NoteLists.StepCounter(NoteLists.StepCounter(text, 0, 0, 1), 0, 0, -1));
            Assert.Equal("Wasser [7/8]\nSport [0/3] Dehnen [2/2]", NoteLists.StepCounter(text, 1, 1, 1));
            Assert.Equal(text, NoteLists.StepCounter(text, 1, 0, -1)); // not below 0
            Assert.Equal("Wasser [8/8]\nSport [0/3] Dehnen [1/2]", NoteLists.StepCounter(NoteLists.StepCounter(text, 0, 0, 1), 0, 0, 1)); // not above the goal
        }

        [Theory]
        [InlineData("Miete 650 + Strom 80 =", "730")]
        [InlineData("3 x 4,5 € =", "13,5")]
        [InlineData("(100 - 20) / 4 =", "20")]
        [InlineData("200 * 15% =", "30")]
        public void TryCalculate_LinesEndingInEquals(string line, string result)
        {
            var before = Util.Strings.Language;
            Util.Strings.Language = "de";
            try
            {
                Assert.True(NoteLists.TryCalculate(line, out var value));
                Assert.Equal(result, value);
            }
            finally
            {
                Util.Strings.Language = before;
            }
        }

        [Theory]
        [InlineData("Termin = Dienstag")]
        [InlineData("Nur Text =")]
        [InlineData("a == b")]
        [InlineData("650 + 80")]
        public void TryCalculate_IgnoresOtherLines(string line) => Assert.False(NoteLists.TryCalculate(line, out _));

        [Fact]
        public void Tags_AreFound_ButNotHeadingsOrColors()
        {
            Assert.Equal(new[] { "#arbeit", "#Projekt-X" }, NoteLists.Tags("Bericht #arbeit fertig\n# Überschrift\nFarbe &#123; #Projekt-X #arbeit"));
            Assert.False(NoteLists.HasTag("Nummer #5"));
            Assert.Equal(NoteLists.TagHue("#arbeit"), NoteLists.TagHue("#Arbeit"));
        }

        [Fact]
        public void Tables_CellsAndSeparator()
        {
            Assert.True(NoteLists.IsTableLine("| Name | Preis |"));
            Assert.False(NoteLists.IsTableLine("Text | mit Strich"));
            Assert.Equal(new[] { "Name", "Preis" }, NoteLists.Cells("| Name | Preis |"));
            Assert.Null(NoteLists.Cells("|------|:---:|"));
            Assert.Equal(new[] { "a", "", "c" }, NoteLists.Cells("|a||c|"));
        }

        [Fact]
        public void Versions_KeepTheNewestAndSkipRepeats()
        {
            var versions = new List<NoteVersion>();
            for (var i = 0; i < 25; i++)
                NoteLists.AddVersion(versions, new NoteVersion { Text = "v" + i });
            NoteLists.AddVersion(versions, new NoteVersion { Text = "v24" });
            Assert.Equal(20, versions.Count);
            Assert.Equal("v5", versions[0].Text);
            Assert.Equal("v24", versions[^1].Text);
        }

        [Fact]
        public void ToMarkdown_AddsTheNameAsHeading() =>
            Assert.Equal("# Einkauf\n\n[ ] Milch\n", NoteLists.ToMarkdown("Einkauf", "[ ] Milch\n\n"));
    }
}
