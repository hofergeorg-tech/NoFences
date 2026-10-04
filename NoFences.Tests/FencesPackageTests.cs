using System.Drawing;
using System.Text.Json;
using NoFences.Model;

namespace NoFences.Tests
{
    public class FencesPackageTests
    {
        [Fact]
        public void MostUsed_SortsByOpenCountThenName()
        {
            using var temp = new TempFolder();
            var a = temp.File("a.txt");
            var b = temp.File("b.txt");
            var c = temp.File("c.txt");
            var info = new FenceInfo();
            info.CountOpen(c);
            info.CountOpen(c);
            info.CountOpen(b);
            var entries = new[] { a, b, c }.Select(FenceEntry.FromPath).OfType<FenceEntry>().ToList();
            var sorted = FenceEntry.Sort(entries, FenceSortMode.MostUsed, info.OpenCounts);
            Assert.Equal(new[] { c, b, a }, sorted.Select(e => e.Path));
        }

        [Fact]
        public void MarksAndNewFieldsSurviveSaving()
        {
            var info = new FenceInfo
            {
                Marks = new() { [@"C:\x.txt"] = MarkColor.Green },
                Hotkey = "Ctrl+Shift+F3",
                NoFade = true,
                AutoCleanDays = 7,
                AutoSource = "bookmarks:edge"
            };
            info.CountOpen(@"C:\x.txt");
            var copy = JsonSerializer.Deserialize<FenceInfo>(JsonSerializer.Serialize(info, FenceStore.JsonOptions), FenceStore.JsonOptions)!;
            Assert.Equal(MarkColor.Green, copy.Marks![@"C:\x.txt"]);
            Assert.Equal(1, copy.OpenCounts![@"C:\x.txt"]);
            Assert.Equal("Ctrl+Shift+F3", copy.Hotkey);
            Assert.True(copy.NoFade);
            Assert.Equal(7, copy.AutoCleanDays);
            Assert.Equal("bookmarks:edge", copy.AutoSource);
        }

        [Fact]
        public void Fade_FullNearbyAndFadedFarAway()
        {
            Assert.Equal(1, FenceExtras.FadeOpacity(0));
            Assert.Equal(1, FenceExtras.FadeOpacity(FenceExtras.FadeNear));
            Assert.Equal(FenceExtras.FadeMin, FenceExtras.FadeOpacity(5000));
            var middle = FenceExtras.FadeOpacity((FenceExtras.FadeNear + FenceExtras.FadeFar) / 2.0);
            Assert.InRange(middle, FenceExtras.FadeMin + 0.1, 0.9);

            var r = new Rectangle(100, 100, 200, 100);
            Assert.Equal(0, FenceExtras.Distance(r, new Point(150, 150)));
            Assert.Equal(50, FenceExtras.Distance(r, new Point(350, 150)));
            Assert.Equal(5, FenceExtras.Distance(r, new Point(303, 204)));
        }

        [Fact]
        public void Shelf_FindsOnlyOldItems()
        {
            using var temp = new TempFolder();
            var now = DateTime.Now;
            var old = temp.File("old.txt");
            File.SetCreationTime(old, now.AddDays(-10));
            File.SetLastWriteTime(old, now.AddDays(-10));
            var fresh = temp.File("fresh.txt");
            // Moved onto the shelf today, but written long ago: stays
            var moved = temp.File("moved.txt");
            File.SetLastWriteTime(moved, now.AddDays(-30));

            var expired = FenceExtras.ExpiredItems(temp.Path, 7, now);
            Assert.Equal(new[] { old }, expired);
            Assert.Empty(FenceExtras.ExpiredItems(temp.Path, 0, now));
            Assert.Empty(FenceExtras.ExpiredItems(Path.Combine(temp.Path, "missing"), 7, now));
        }

        private const string ChromeJson = """
            {
              "roots": {
                "bookmark_bar": {
                  "children": [
                    { "type": "url", "name": "News", "url": "https://example.com/news" },
                    { "type": "folder", "name": "Dev", "children": [
                        { "type": "url", "name": "Docs", "url": "https://example.com/docs" },
                        { "type": "folder", "name": "Deep", "children": [ { "type": "url", "name": "", "url": "https://example.com/deep" } ] }
                    ] }
                  ]
                },
                "other": { "children": [ { "type": "url", "name": "Elsewhere", "url": "https://example.com/other" } ] }
              }
            }
            """;

        [Fact]
        public void Bookmarks_ParsesTheBarWithOneFolderLevel()
        {
            var bookmarks = FenceExtras.ParseChromiumBookmarks(ChromeJson);
            Assert.Equal(3, bookmarks.Count);
            Assert.Equal(new FenceExtras.Bookmark("News", "https://example.com/news", null), bookmarks[0]);
            Assert.Equal("Dev", bookmarks[1].Folder);
            // Nested deeper: lands in the top folder, nameless ones use the URL
            Assert.Equal(new FenceExtras.Bookmark("https://example.com/deep", "https://example.com/deep", "Dev"), bookmarks[2]);
            Assert.Empty(FenceExtras.ParseChromiumBookmarks("not json"));
            Assert.Empty(FenceExtras.ParseChromiumBookmarks("{}"));
        }

        [Fact]
        public void Bookmarks_WrittenAsUrlFilesAndKeptInSync()
        {
            using var temp = new TempFolder();
            var first = FenceExtras.ParseChromiumBookmarks(ChromeJson);
            Assert.True(FenceExtras.WriteBookmarks(temp.Path, first));
            Assert.True(File.Exists(Path.Combine(temp.Path, "News.url")));
            Assert.Contains("URL=https://example.com/docs", File.ReadAllText(Path.Combine(temp.Path, "Dev", "Docs.url")));
            // Same again: nothing changes
            Assert.False(FenceExtras.WriteBookmarks(temp.Path, first));
            // A bookmark removed in the browser disappears, empty folders too
            Assert.True(FenceExtras.WriteBookmarks(temp.Path, first.Take(1).ToList()));
            Assert.False(Directory.Exists(Path.Combine(temp.Path, "Dev")));
            Assert.Single(Directory.GetFiles(temp.Path));
        }

        [Fact]
        public void SafeFileName_ReplacesInvalidCharacters()
        {
            Assert.Equal("a_b_c", FenceExtras.SafeFileName("a/b:c"));
            Assert.Equal("_", FenceExtras.SafeFileName("  "));
            Assert.Equal("Name", FenceExtras.SafeFileName("Name..."));
            Assert.True(FenceExtras.SafeFileName(new string('x', 200)).Length <= 80);
        }

        [Fact]
        public void RecentFolders_NewestFirstWithoutDuplicates()
        {
            var now = DateTime.Now;
            var targets = new Dictionary<string, string?>
            {
                ["1.lnk"] = @"C:\Projects\App",           // a folder
                ["2.lnk"] = @"C:\Projects\App\readme.md", // a file in the same folder
                ["3.lnk"] = @"D:\Photos\cat.jpg",
                ["4.lnk"] = null,                          // broken shortcut
                ["5.lnk"] = @"E:\Gone\file.txt"            // folder no longer there
            };
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\Projects\App", @"D:\Photos" };
            var links = new[] { ("1.lnk", now.AddMinutes(-5)), ("2.lnk", now), ("3.lnk", now.AddMinutes(-1)), ("4.lnk", now), ("5.lnk", now) };
            var result = FenceExtras.RecentFolders(links, l => targets[l], folders.Contains, 10);
            Assert.Equal(new[] { @"C:\Projects\App", @"D:\Photos" }, result);
            Assert.Single(FenceExtras.RecentFolders(links, l => targets[l], folders.Contains, 1));
        }

        [Fact]
        public void MoveToScreen_KeepsTheRelativePlace()
        {
            var left = new Rectangle(0, 0, 1920, 1040);
            var right = new Rectangle(1920, 0, 2560, 1400);
            // Top left stays top left
            Assert.Equal(new Rectangle(1920, 0, 300, 200), FenceExtras.MoveToScreen(new Rectangle(0, 0, 300, 200), left, right));
            // Bottom right stays bottom right
            Assert.Equal(new Rectangle(1920 + 2560 - 300, 1400 - 200, 300, 200), FenceExtras.MoveToScreen(new Rectangle(1620, 840, 300, 200), left, right));
            // Too big for the smaller screen: shrinks to fit
            var big = FenceExtras.MoveToScreen(new Rectangle(1920, 0, 2500, 1300), right, left);
            Assert.True(left.Contains(big));
        }

        [Fact]
        public void FenceHotkeys_ParseFunctionKeys()
        {
            Assert.Equal(12, FenceExtras.FenceHotkeys.Count);
            Assert.Equal(Keys.F1, FenceExtras.FenceHotkeyKey("Ctrl+Shift+F1"));
            Assert.Equal(Keys.F12, FenceExtras.FenceHotkeyKey("Ctrl+Shift+F12"));
            Assert.Null(FenceExtras.FenceHotkeyKey("Ctrl+Shift+F13"));
            Assert.Null(FenceExtras.FenceHotkeyKey(null));
            Assert.Null(FenceExtras.FenceHotkeyKey("Ctrl+Alt+D"));
        }

        [Fact]
        public void Templates_FitOnTheScreenWithoutOverlapping()
        {
            var area = new Rectangle(0, 0, 1920, 1040);
            foreach (var id in FenceExtras.TemplateIds)
            {
                var fences = FenceExtras.Template(id, area);
                Assert.NotEmpty(fences);
                var rects = fences.Select(f => new Rectangle(f.PosX, f.PosY, f.Width, f.Height)).ToList();
                Assert.All(rects, r => Assert.True(area.Contains(r), $"{id}: {r} outside"));
                for (var i = 0; i < rects.Count; i++)
                    for (var j = i + 1; j < rects.Count; j++)
                        Assert.False(rects[i].IntersectsWith(rects[j]), $"{id}: fences {i} and {j} overlap");
                Assert.All(fences.Where(f => f.Kind == FenceKind.Widget), f => Assert.Contains(Widgets.WidgetRegistry.Types, t => t.Type == f.WidgetType));
            }
        }

        [Fact]
        public void HoverPreview_KnowsWhichFilesHaveOne()
        {
            Assert.True(HoverPopup.HasPreview(@"C:\a\photo.JPG"));
            Assert.True(HoverPopup.HasPreview(@"C:\a\doc.pdf"));
            Assert.False(HoverPopup.HasPreview(@"C:\a\setup.exe"));
            Assert.Equal("1.5 KB", HoverPopup.FormatSize(1536).Replace(',', '.'));
            Assert.Equal("12 B", HoverPopup.FormatSize(12));
        }
    }
}
