using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class RecurrenceTests
    {
        [Fact]
        public void Next_SkipsMissedOccurrences()
        {
            var at = new DateTime(2026, 10, 1, 9, 0, 0); // Thursday
            var now = new DateTime(2026, 10, 3, 12, 0, 0);
            Assert.Equal(new DateTime(2026, 10, 4, 9, 0, 0), Recurrence.Next(at, Repeat.Daily, now));
            Assert.Equal(new DateTime(2026, 10, 5, 9, 0, 0), Recurrence.Next(at, Repeat.Weekdays, now)); // Monday
            Assert.Equal(new DateTime(2026, 10, 8, 9, 0, 0), Recurrence.Next(at, Repeat.Weekly, now));
            Assert.Null(Recurrence.Next(at, Repeat.None, now));
        }

        [Fact]
        public void Monthly_KeepsTheDayOrUsesTheLastDay()
        {
            var at = new DateTime(2026, 1, 31, 8, 0, 0);
            Assert.Equal(new DateTime(2026, 2, 28, 8, 0, 0), Recurrence.Next(at, Repeat.Monthly, at));
            Assert.Equal(new DateTime(2026, 3, 31, 8, 0, 0), Recurrence.Next(at, Repeat.Monthly, new DateTime(2026, 3, 1)));
        }

        [Fact]
        public void Todo_RepeatingMovesOn_NormalTogglesDone()
        {
            var now = new DateTime(2026, 10, 3, 12, 0, 0);
            var weekly = new TodoItem { Text = "Bins out", Due = new DateTime(2026, 10, 3, 7, 0, 0), Repeat = Repeat.Weekly, Notified = true };
            weekly.Toggle(now);
            Assert.False(weekly.Done);
            Assert.False(weekly.Notified);
            Assert.Equal(new DateTime(2026, 10, 10, 7, 0, 0), weekly.Due);

            var once = new TodoItem { Text = "Call" };
            once.Toggle(now);
            Assert.True(once.Done);
            once.Toggle(now);
            Assert.False(once.Done);
        }

        [Fact]
        public void TodoList_RoundTripAndOrder()
        {
            var items = new List<TodoItem>
            {
                new() { Text = "done", Done = true },
                new() { Text = "later", Due = new DateTime(2026, 10, 9) },
                new() { Text = "undated" },
                new() { Text = "soon", Due = new DateTime(2026, 10, 4), Repeat = Repeat.Daily },
            };
            var back = TodoList.Parse(TodoList.Format(items));
            Assert.Equal(Repeat.Daily, back[3].Repeat);
            Assert.Equal(new[] { "soon", "later", "undated", "done" }, TodoList.Ordered(back).Select(i => i.Text));
            Assert.Empty(TodoList.Parse("not json"));
        }
    }

    public class NoteMarkdownTests
    {
        [Theory]
        [InlineData("# Title", NoteText.LineKind.Heading1, "Title")]
        [InlineData("## Sub", NoteText.LineKind.Heading2, "Sub")]
        [InlineData("### Small", NoteText.LineKind.Heading3, "Small")]
        [InlineData("- milk", NoteText.LineKind.Bullet, "milk")]
        [InlineData("* bread", NoteText.LineKind.Bullet, "bread")]
        [InlineData("> quoted", NoteText.LineKind.Quote, "quoted")]
        [InlineData("---", NoteText.LineKind.Rule, "")]
        [InlineData("#hashtag", NoteText.LineKind.Text, "#hashtag")]
        [InlineData("plain", NoteText.LineKind.Text, "plain")]
        public void ParseLine(string line, NoteText.LineKind kind, string content)
        {
            var (k, c) = NoteText.ParseLine(line);
            Assert.Equal(kind, k);
            Assert.Equal(content, c);
        }

        [Fact]
        public void Runs_BoldItalicPlain()
        {
            var runs = NoteText.Runs("Buy **milk** and *fresh* bread, 2*3*4 stays");
            Assert.Contains(runs, r => r.Text == "milk" && r.Bold);
            Assert.Contains(runs, r => r.Text == "fresh" && r.Italic);
            Assert.Equal("Buy milk and fresh bread, 2*3*4 stays", string.Concat(runs.Select(r => r.Text)));
            Assert.False(NoteText.HasInlineFormatting("no markup here, a * b"));
        }

        [Fact]
        public void Summary_IgnoresMarkup() => Assert.Equal("Shopping list", NoteText.Summary("# **Shopping** list\n- milk"));
    }

    public class WorldClockTests
    {
        [Fact]
        public void Parse_SkipsUnknownZonesAndKeepsLabels()
        {
            var clocks = WorldClockWidget.Parse("Tokyo Standard Time|Tokyo\nNot A Zone|X\nUTC");
            Assert.Equal(2, clocks.Count);
            Assert.Equal("Tokyo", clocks[0].Label);
            Assert.Equal("Tokyo Standard Time|Tokyo\nUTC|" + clocks[1].Label, WorldClockWidget.Format(clocks));
        }

        [Fact]
        public void Difference_IsRelativeToLocal() =>
            Assert.Equal("±0", WorldClockWidget.Difference(TimeZoneInfo.Local, DateTime.UtcNow));
    }

    public class SmallToolTests
    {
        [Fact]
        public void Downloads_OldOnes_BiggestFirst()
        {
            var now = new DateTime(2026, 10, 3);
            var all = new[]
            {
                new DownloadsCleaner.Entry("a", "a", 10, now.AddDays(-40), false),
                new DownloadsCleaner.Entry("b", "b", 500, now.AddDays(-100), false),
                new DownloadsCleaner.Entry("c", "c", 999, now.AddDays(-2), false),
            };
            Assert.Equal(new[] { "b", "a" }, DownloadsCleaner.Old(all, now, 30).Select(e => e.Name));
        }

        [Fact]
        public void ColorFormats()
        {
            Assert.Equal("#0A7BFF", ColorPicker.Hex(Color.FromArgb(10, 123, 255)));
            Assert.Equal("rgb(10, 123, 255)", ColorPicker.Rgb(Color.FromArgb(10, 123, 255)));
        }

        [Fact]
        public void Steam_SpecialsAndPrices()
        {
            var specials = SteamDealsWidget.ParseSpecials("""
                {"specials":{"items":[{"id":1091500,"name":"Cyberpunk 2077","discount_percent":70,"original_price":5999,"final_price":1799,"currency":"EUR","small_capsule_image":"https://x/y.jpg"},
                                      {"id":1,"name":"Full price","discount_percent":0,"original_price":100,"final_price":100,"currency":"EUR"}]}}
                """);
            var deal = Assert.Single(specials);
            Assert.Equal(70, deal.DiscountPercent);
            Assert.Equal(1799, deal.FinalCents);

            var prices = SteamDealsWidget.ParsePrices("""
                {"292030":{"success":true,"data":{"price_overview":{"currency":"EUR","initial":4999,"final":2499,"discount_percent":50}}},"5":{"success":true,"data":[]}}
                """);
            Assert.Equal((292030, (50, 2499, 4999, "EUR")), Assert.Single(prices));
        }
    }
}
