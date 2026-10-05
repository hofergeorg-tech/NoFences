using NoFences.Model;

namespace NoFences.Tests
{
    public class NoteFormatTests
    {
        [Fact]
        public void Runs_KnowAllFormats()
        {
            var runs = NoteText.Runs("a **b** *c* __d__ ~~e~~ ==f== =={g}g== _h_");
            Assert.Contains(runs, r => r.Text == "b" && r.Bold);
            Assert.Contains(runs, r => r.Text == "c" && r.Italic);
            Assert.Contains(runs, r => r.Text == "d" && r.Underline && !r.Italic);
            Assert.Contains(runs, r => r.Text == "e" && r.Strike);
            Assert.Contains(runs, r => r.Text == "f" && r.Highlight == 'y');
            Assert.Contains(runs, r => r.Text == "g" && r.Highlight == 'g');
            Assert.Contains(runs, r => r.Text == "h" && r.Italic);
            Assert.Equal("a b c d e f g h", string.Concat(runs.Select(r => r.Text)));
        }

        [Fact]
        public void Runs_CanBeCombined()
        {
            var runs = NoteText.Runs("**==wichtig== und __mehr__**");
            Assert.Contains(runs, r => r.Text == "wichtig" && r.Bold && r.Highlight == 'y');
            Assert.Contains(runs, r => r.Text == "mehr" && r.Bold && r.Underline);
            Assert.Contains(runs, r => r.Text == " und " && r.Bold && r.Highlight == null);
        }

        [Theory]
        [InlineData("2 * 3 * 4")]
        [InlineData("a == b")]
        [InlineData("snake_case_name")]
        [InlineData("~ ungefähr ~")]
        public void Runs_LeaveOrdinaryTextAlone(string text) => Assert.False(NoteText.HasInlineFormatting(text));

        [Theory]
        [InlineData("!!! Steuer", 3, "Steuer")]
        [InlineData("!! Arzt", 2, "Arzt")]
        [InlineData("! Später", 1, "Später")]
        [InlineData("!Achtung", 0, "!Achtung")]
        [InlineData("Normal", 0, "Normal")]
        public void Priority_IsReadFromTheLineStart(string line, int level, string rest)
        {
            var (l, r) = NoteText.Priority(line);
            Assert.Equal(level, l);
            Assert.Equal(rest, r);
        }

        [Fact]
        public void ToggleWrap_WrapsAndUnwraps()
        {
            // "Milch" selected
            var (text, start, length) = NoteText.ToggleWrap("Kauf Milch heute", 5, 5, "**");
            Assert.Equal("Kauf **Milch** heute", text);
            Assert.Equal("Milch", text.Substring(start, length));
            var back = NoteText.ToggleWrap(text, start, length, "**");
            Assert.Equal("Kauf Milch heute", back.Text);
            // Selection including the markers also unwraps
            Assert.Equal("Kauf Milch heute", NoteText.ToggleWrap(text, 5, 9, "**").Text);
        }

        [Fact]
        public void ToggleWrap_KeepsSpacesOutside_AndInsertsWithoutSelection()
        {
            Assert.Equal("a __word__ b", NoteText.ToggleWrap("a word b", 2, 5, "__").Text); // "word " selected
            var (text, start, length) = NoteText.ToggleWrap("ab", 1, 0, "**");
            Assert.Equal("a****b", text);
            Assert.Equal(3, start);
            Assert.Equal(0, length);
        }

        [Fact]
        public void SetHighlight_ChangesAndRemovesTheColor()
        {
            var yellow = NoteText.SetHighlight("ein Wort hier", 4, 4, 'y');
            Assert.Equal("ein ==Wort== hier", yellow.Text);
            var green = NoteText.SetHighlight(yellow.Text, yellow.Start, yellow.Length, 'g');
            Assert.Equal("ein =={g}Wort== hier", green.Text);
            var none = NoteText.SetHighlight(green.Text, green.Start, green.Length, null);
            Assert.Equal("ein Wort hier", none.Text);
        }

        [Fact]
        public void SetPriority_PutsTheMarkAfterCheckboxOrBullet()
        {
            Assert.Equal("Eins\n!!! Zwei", NoteText.SetPriority("Eins\nZwei", 7, 3).Text);
            Assert.Equal("[ ] !! Arzt", NoteText.SetPriority("[ ] Arzt", 5, 2).Text);
            Assert.Equal("- ! Brot", NoteText.SetPriority("- Brot", 0, 1).Text);
            Assert.Equal("[ ] ! Arzt", NoteText.SetPriority("[ ] !!! Arzt", 9, 1).Text);
            Assert.Equal("[ ] Arzt", NoteText.SetPriority("[ ] !!! Arzt", 9, 0).Text);
        }

        [Fact]
        public void Summary_IgnoresNewMarkup() =>
            Assert.Equal("Steuer bis Freitag", NoteText.Summary("[ ] !!! ==Steuer== __bis__ Freitag"));
    }
}
