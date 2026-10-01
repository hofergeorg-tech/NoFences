using NoFences.Model;

namespace NoFences.Tests
{
    public class NoteTextTests
    {
        [Fact]
        public void ToggleCheckbox_TicksAndUnticks()
        {
            var text = "Einkaufen:\n[ ] Milch\n[x] Brot";
            var once = NoteText.ToggleCheckbox(text, 1);
            Assert.Equal("Einkaufen:\n[x] Milch\n[x] Brot", once);
            Assert.Equal("Einkaufen:\n[x] Milch\n[ ] Brot", NoteText.ToggleCheckbox(once, 2));
        }

        [Fact]
        public void ToggleCheckbox_SupportsBulletPrefixAndUppercaseX()
        {
            Assert.Equal("- [x] Task", NoteText.ToggleCheckbox("- [ ] Task", 0));
            Assert.Equal("  * [ ] Task", NoteText.ToggleCheckbox("  * [X] Task", 0));
        }

        [Theory]
        [InlineData("Just text", 0)]
        [InlineData("[ ] a", 5)]
        [InlineData("[ ] a", -1)]
        public void ToggleCheckbox_LeavesOtherLinesAlone(string text, int line)
        {
            Assert.Equal(text, NoteText.ToggleCheckbox(text, line));
        }

        [Fact]
        public void FindLinks_FindsWebAddressesAndPaths()
        {
            var links = NoteText.FindLinks(@"Siehe www.example.com, Datei C:\Temp\liste.txt und https://x.org/a?b=1.").ToList();
            Assert.Equal(3, links.Count);
            Assert.Equal("https://www.example.com", links[0].Target);
            Assert.Equal(@"C:\Temp\liste.txt", links[1].Target);
            Assert.Equal("https://x.org/a?b=1", links[2].Target);
        }

        [Fact]
        public void FindLinks_PositionsMatchTheText()
        {
            const string line = "Web:\twww.rsi.com";
            var link = Assert.Single(NoteText.FindLinks(line));
            Assert.Equal("www.rsi.com", line.Substring(link.Start, link.Length));
        }

        [Fact]
        public void Summary_SkipsEmptyLinesAndCheckboxMarkup()
        {
            Assert.Equal("Milch kaufen", NoteText.Summary("\n\n[ ] Milch kaufen\nBrot"));
            Assert.Equal("", NoteText.Summary("   \n"));
        }

        [Fact]
        public void Summary_ShortensLongLines()
        {
            var summary = NoteText.Summary(new string('a', 200), 10);
            Assert.Equal(10, summary.Length);
            Assert.EndsWith("…", summary);
        }
    }
}
