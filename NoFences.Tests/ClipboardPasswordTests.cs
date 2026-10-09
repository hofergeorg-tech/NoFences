using NoFences.Widgets;

namespace NoFences.Tests
{
    public class ClipboardPasswordTests
    {
        [Theory]
        [InlineData("Tr0ub4dor&3", true)]
        [InlineData("k9#Lmq2!xZ", true)]
        [InlineData("Ab3x9Kq2", true)]          // a harmless code looks the same – masked too
        [InlineData("correcthorsebattery", false)] // one kind of characters only
        [InlineData("sommer2026", false)]       // two kinds: not recognised
        [InlineData("Short1!", false)]          // under 8
        [InlineData("Hello World 123!", false)] // spaces: a sentence
        [InlineData("https://Example.com/A1", false)]
        [InlineData(@"C:\Users\Me\File1.txt", false)]
        [InlineData("Max.Muster1@example.com", false)]
        [InlineData("line1A!\nline2B?", false)]
        public void LooksLikePassword(string text, bool expected) => Assert.Equal(expected, ClipboardHistory.LooksLikePassword(text));

        [Fact]
        public void OldPinnedList_StillLoads_AndNewFormatKeepsTheSetting()
        {
            string? option = """[{"Text":"pinned note","Image":null}]""";
            var widget = new ClipboardWidget(() => option, o => option = o);
            Assert.Equal("pinned note", Assert.Single(widget.History.Items).Text);

            // Any save writes the new format, which still holds the pinned entry
            widget.AddMenuItems(new ContextMenuStrip().Items, null!);
            var toggle = new ContextMenuStrip();
            widget.AddMenuItems(toggle.Items, null!);
            toggle.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Last().PerformClick();
            Assert.Contains("\"ShowPasswords\":true", option);
            Assert.Contains("pinned note", option);
            widget.Dispose();

            var again = new ClipboardWidget(() => option, o => option = o);
            Assert.Equal("pinned note", Assert.Single(again.History.Items).Text);
            again.Dispose();
        }
    }
}
