using NoFences.Model;

namespace NoFences.Tests
{
    public class DockTests
    {
        private static readonly Rectangle WorkArea = new(0, 0, 1920, 1040); // taskbar below

        [Theory]
        [InlineData(DockEdge.Left, 0, 0, 300, 1040)]
        [InlineData(DockEdge.Right, 1620, 0, 300, 1040)]
        [InlineData(DockEdge.Top, 0, 0, 1920, 300)]
        [InlineData(DockEdge.Bottom, 0, 740, 1920, 300)]
        public void Strip_SitsAtTheEdge(DockEdge edge, int x, int y, int w, int h) =>
            Assert.Equal(new Rectangle(x, y, w, h), DockLayout.Strip(WorkArea, edge, 300));

        [Fact]
        public void Sidebar_StacksFencesWithTheirOwnHeights()
        {
            var strip = DockLayout.Strip(WorkArea, DockEdge.Right, 300);
            var places = DockLayout.Arrange(strip, DockEdge.Right, new[] { 200, 35, 400 }, 6);
            Assert.Equal(new[]
            {
                new Rectangle(1620, 6, 300, 200),
                new Rectangle(1620, 212, 300, 35),   // a folded fence: just its title
                new Rectangle(1620, 253, 300, 400)
            }, places);
        }

        [Fact]
        public void TopBar_PutsFencesSideBySide_AndLimitsTooLongOnes()
        {
            var strip = DockLayout.Strip(WorkArea, DockEdge.Top, 250);
            var places = DockLayout.Arrange(strip, DockEdge.Top, new[] { 400, 5000 }, 10);
            Assert.Equal(new Rectangle(10, 0, 400, 250), places[0]);
            Assert.Equal(new Rectangle(420, 0, 1900, 250), places[1]);
        }

        [Fact]
        public void Trigger_IsAThinBandAtTheMonitorEdge()
        {
            var screen = new Rectangle(1920, 0, 2560, 1440); // second monitor
            Assert.Equal(new Rectangle(1920, 0, 2, 1440), DockLayout.Trigger(screen, DockEdge.Left));
            Assert.Equal(new Rectangle(4478, 0, 2, 1440), DockLayout.Trigger(screen, DockEdge.Right));
            Assert.True(DockLayout.Trigger(screen, DockEdge.Bottom).Contains(3000, 1439));
            Assert.False(DockLayout.Trigger(screen, DockEdge.Bottom).Contains(3000, 1430));
        }

        [Fact]
        public void Order_FollowsWhereTheFencesWereDropped()
        {
            Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
            var order = DockLayout.OrderByPosition(new[]
            {
                (a, new Rectangle(0, 10, 300, 200)),
                (b, new Rectangle(0, 500, 300, 100)),
                (c, new Rectangle(0, 150, 300, 100)) // dragged between a and b
            }, DockEdge.Left);
            Assert.Equal(new[] { a, c, b }, order);
        }

        [Fact]
        public void InOrder_KnownFirstThenNewOnes()
        {
            Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
            var result = DockLayout.InOrder(new[] { a, b, c }, x => x, new[] { c, a });
            Assert.Equal(new[] { c, a, b }, result);
        }

        [Fact]
        public void Docks_AreSavedWithTheConfig()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            var id = Guid.NewGuid();
            store.Config.Docks.Add(new DockBar { Group = "Seitenleiste", Edge = DockEdge.Left, AutoHide = false, Thickness = 280, Theme = "jause", Order = { id }, Saved = { [id] = new[] { 1, 2, 3, 4 } } });
            store.SaveNow();

            var reloaded = new FenceStore(dir.Path);
            reloaded.Load();
            var dock = Assert.Single(reloaded.Config.Docks);
            Assert.Equal(DockEdge.Left, dock.Edge);
            Assert.False(dock.AutoHide);
            Assert.Equal(280, dock.Thickness);
            Assert.Equal("jause", dock.Theme);
            Assert.Equal(new[] { 1, 2, 3, 4 }, dock.Saved[id]);
        }

        [Fact]
        public void JauseStyle_DrawsAsFenceAndAsBar()
        {
            var theme = NoFences.Themes.ThemeRegistry.Get("jause");
            Assert.Equal("jause", theme.Id);
            Assert.Equal(NoFences.Themes.ThemeRegistry.Group.Leisure, NoFences.Themes.ThemeRegistry.GroupOf(theme));
            using var bitmap = new Bitmap(320, 400);
            using var g = Graphics.FromImage(bitmap);
            theme.DrawFrame(g, new Rectangle(0, 0, 320, 400), 35, new FenceInfo(), 1f);
            using var font = theme.CreateTitleFont(35);
            theme.DrawTitle(g, new Rectangle(0, 0, 320, 35), "Brotzeit", font, 1f);
            theme.DrawBar(g, new Rectangle(0, 0, 320, 400), true, new FenceInfo(), 1f);
            theme.DrawBar(g, new Rectangle(0, 0, 320, 120), false, new FenceInfo(), 1.5f);
            Assert.Equal(255, bitmap.GetPixel(160, 200).A); // a table, not a hole
        }
    }
}