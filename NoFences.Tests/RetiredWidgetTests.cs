using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class RetiredWidgetTests
    {
        [Fact]
        public void RemovedWidgets_AreTakenOutAndReported()
        {
            var fences = new List<FenceInfo>
            {
                new() { Name = "Links" },
                new() { Name = "Uhr", Kind = FenceKind.Widget, WidgetType = "clock" },
                new() { Name = "System", Kind = FenceKind.Widget, WidgetType = "system" },
                new() { Name = "Star Citizen", Kind = FenceKind.Widget, WidgetType = "starcitizen" },
                new() { Name = "Dashboard", Kind = FenceKind.Widget, WidgetType = "webpage" },
            };
            var removed = WidgetRegistry.TakeOutRemoved(fences);
            Assert.Equal(new[] { "System", "Star Citizen", "Dashboard" }, removed.Select(f => f.Name));
            Assert.Equal(new[] { "Links", "Uhr" }, fences.Select(f => f.Name));
            Assert.Empty(WidgetRegistry.TakeOutRemoved(fences));
        }

        [Fact]
        public void EveryOfferedWidget_CanBeCreated_AndNoneIsRetired()
        {
            foreach (var (type, _, _) in WidgetRegistry.Types)
                Assert.DoesNotContain(type, WidgetRegistry.Removed);
            foreach (var (_, types) in WidgetRegistry.Groups)
                Assert.All(types, t => Assert.Contains(t, WidgetRegistry.Types.Select(x => x.Type)));
            Assert.Equal(WidgetRegistry.Types.Count, WidgetRegistry.Groups.Sum(g => g.Types.Length));
        }
    }
}
