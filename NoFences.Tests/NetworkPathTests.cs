using System.Diagnostics;
using NoFences.Model;

namespace NoFences.Tests
{
    public class NetworkPathTests
    {
        [Theory]
        [InlineData(@"\\server\share\game.lnk", true)]
        [InlineData(@"\\192.168.1.5\c$", true)]
        [InlineData(@"//server/share/x.png", true)]
        [InlineData(@"\\?\UNC\server\share\x", true)]
        [InlineData(@"""\\server\share\x.exe""", true)]
        [InlineData("file://server/share/x.png", true)]
        [InlineData(@"\\?\C:\very\long\path", false)]
        [InlineData(@"\\.\pipe\something", false)]
        [InlineData(@"C:\Users\me\Desktop\Steam.lnk", false)]
        [InlineData("file:///C:/Users/me/x.png", false)]
        [InlineData("https://example.com/feed", false)]
        [InlineData("media/note-1.png", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsNetworkPath(string? path, bool expected) => Assert.Equal(expected, NetworkPath.IsNetworkPath(path));

        [Fact]
        public void FindIn_FindsPathsInsideTextButNotWebLinks()
        {
            var text = "Shopping\n![pic](\\\\evil\\share\\p.png)\nsee https://example.com/a//b and C:\\Temp\\x\n//other/share/y";
            Assert.Equal(new[] { @"\\evil\share\p.png", "//other/share/y" }, NetworkPath.FindIn(text));
        }

        [Fact]
        public void Import_ListsAndRemovesNetworkPaths()
        {
            var links = new FenceInfo
            {
                Name = "Gaming",
                Files = { @"C:\Games\Steam.lnk", @"\\evil\share\Steam.lnk" },
                Tabs = { new FenceTab { Name = "More", Files = { @"\\evil\share\more.exe", @"D:\ok.txt" } } },
                OpenWith = @"\\evil\share\opener.exe",
                BackgroundImage = @"\\evil\share\bg.png",
                ItemNotes = new() { [@"\\evil\share\Steam.lnk"] = "note", [@"C:\Games\Steam.lnk"] = "mine" },
            };
            var note = new FenceInfo { Name = "Note", Kind = FenceKind.Note, NoteText = "Buy milk\n![x](\\\\evil\\share\\pixel.png)\nCall mum" };
            var folder = new FenceInfo { Name = "NAS", Kind = FenceKind.Folder, FolderPath = @"\\nas\music" };
            var export = new FenceExport { Fences = { links, note, folder } };

            var found = export.NetworkPaths();
            Assert.Contains(("Gaming", @"\\evil\share\Steam.lnk"), found);
            Assert.Contains(("Gaming", @"\\evil\share\opener.exe"), found);
            Assert.Contains(("Note", @"\\evil\share\pixel.png"), found);
            Assert.Contains(("NAS", @"\\nas\music"), found);
            Assert.Equal(found.Count, found.Distinct().Count());

            export.RemoveNetworkPaths();
            Assert.Empty(export.NetworkPaths());
            var cleaned = export.Fences[0];
            Assert.Equal("Gaming", cleaned.Name);
            Assert.Equal(new[] { @"C:\Games\Steam.lnk" }, cleaned.Files);
            Assert.Equal(new[] { @"D:\ok.txt" }, cleaned.Tabs[0].Files);
            Assert.Null(cleaned.OpenWith);
            Assert.Null(cleaned.BackgroundImage);
            Assert.Equal(new[] { @"C:\Games\Steam.lnk" }, cleaned.ItemNotes!.Keys);
            Assert.Equal("Buy milk\nCall mum", export.Fences[1].NoteText);
            Assert.Equal(FenceKind.Note, export.Fences[1].Kind);
            Assert.Null(export.Fences[2].FolderPath);
        }

        [Fact]
        public void Import_WritesOnlyJsonStyles()
        {
            using var temp = new TempFolder();
            var export = new FenceExport
            {
                Themes = { ["mine.json"] = "{}", ["evil.cmd"] = "del *", ["sub/../trick.lnk"] = "x" }
            };
            Assert.Equal(1, export.WriteThemes(temp.Path));
            Assert.Equal(new[] { "mine.json" }, Directory.GetFiles(temp.Path).Select(Path.GetFileName));
        }

        [Fact]
        public void NetworkEntry_IsShownWithoutTouchingTheServer()
        {
            // An unreachable server would take seconds per check
            var watch = Stopwatch.StartNew();
            var file = FenceEntry.FromPath(@"\\nonexistent-server-for-tests\share\game.lnk");
            var dir = FenceEntry.FromPath(@"\\nonexistent-server-for-tests\share\Music");
            Assert.True(watch.ElapsedMilliseconds < 200, $"took {watch.ElapsedMilliseconds} ms");
            Assert.False(file!.IsFolder);
            Assert.True(dir!.IsFolder);
        }

        [Theory]
        [InlineData(@"\\nonexistent-server-for-tests\share\doc.pdf")]
        [InlineData(@"\\nonexistent-server-for-tests\share\Music")]
        public void NetworkEntry_GetsATypeIconWithoutAccess(string path)
        {
            var watch = Stopwatch.StartNew();
            using var icon = NoFences.Util.IconCache.TypeIcon(path, 48);
            Assert.NotNull(icon);
            Assert.Equal(48, icon!.Width);
            Assert.True(watch.ElapsedMilliseconds < 500, $"took {watch.ElapsedMilliseconds} ms");
        }
    }
}
