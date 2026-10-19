using NoFences.Widgets;

namespace NoFences.Tests
{
    public class ClockWidgetTests
    {
        [Fact]
        public void Options_DefaultToDigitalWithWeekNumbers()
        {
            Assert.Equal(new ClockOptions(ClockFace.Digital, true), ClockOptions.Parse(null));
            Assert.Equal(new ClockOptions(ClockFace.Digital, true), ClockOptions.Parse("face=sundial;rubbish"));
        }

        [Fact]
        public void Options_RoundTrip()
        {
            var options = new ClockOptions(ClockFace.Binary, false);
            Assert.Equal("face=binary;weeks=0", options.Format());
            Assert.Equal(options, ClockOptions.Parse(options.Format()));
            Assert.Equal(ClockFace.Analog, ClockOptions.Parse("face=Analog").Face);
        }

        [Fact]
        public void MonthGrid_FillsUpToAYear()
        {
            Assert.Equal((1, 0, 0), ClockWidget.MonthGrid(200, 100, 180, 150, 10, 10));   // too low: no calendar
            Assert.Equal((1, 1, 1), ClockWidget.MonthGrid(250, 160, 180, 150, 10, 10));
            Assert.Equal((2, 1, 2), ClockWidget.MonthGrid(400, 160, 180, 150, 10, 10));
            Assert.Equal((3, 2, 6), ClockWidget.MonthGrid(600, 320, 180, 150, 10, 10));
            // Room for 5 × 3: a balanced 4 × 3 year instead
            Assert.Equal((4, 3, 12), ClockWidget.MonthGrid(1000, 480, 180, 150, 10, 10));
            Assert.Equal((12, 1, 12), ClockWidget.MonthGrid(3000, 160, 180, 150, 10, 10));
        }

        [Fact]
        public void FirstShown_FollowsPagingAndStartsTheYearInJanuary()
        {
            var today = new DateTime(2026, 10, 19);
            Assert.Equal(new DateTime(2026, 10, 1), ClockWidget.FirstShown(today, 0, 1));
            Assert.Equal(new DateTime(2027, 1, 1), ClockWidget.FirstShown(today, 3, 2));
            Assert.Equal(new DateTime(2025, 12, 1), ClockWidget.FirstShown(today, -10, 3));
            Assert.Equal(new DateTime(2026, 1, 1), ClockWidget.FirstShown(today, 0, 12));
            Assert.Equal(new DateTime(2027, 1, 1), ClockWidget.FirstShown(today, 12, 12));
        }
    }
}
