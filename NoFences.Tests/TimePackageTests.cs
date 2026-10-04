using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class TimePackageTests
    {
        [Fact]
        public void Timer_RingsOnceThenDisappears()
        {
            var now = new DateTime(2026, 10, 4, 12, 0, 0);
            var set = new TimerSet { Timers = { new CountdownTimer { Label = "Pasta", Start = now, End = now.AddMinutes(10) } } };
            Assert.Empty(set.Due(now.AddMinutes(9)));
            Assert.Equal(new[] { "Pasta" }, set.Due(now.AddMinutes(10)));
            Assert.Empty(set.Due(now.AddMinutes(10).AddSeconds(5)));
            set.Due(now.AddMinutes(12));
            Assert.Empty(set.Timers);
        }

        [Fact]
        public void Alarm_DaysOnceAndMissed()
        {
            var monday = new DateTime(2026, 10, 5, 7, 0, 30);
            var weekdays = new Alarm { Time = "07:00", Days = { DayOfWeek.Monday, DayOfWeek.Tuesday }, Label = "Work" };
            var once = new Alarm { Time = "07:00", Once = true };
            var set = new TimerSet { Alarms = { weekdays, once } };
            Assert.Equal(new[] { "Work", "07:00" }, set.Due(monday));
            Assert.Empty(set.Due(monday.AddMinutes(1)));        // only once a day
            Assert.False(once.Enabled);                          // one-time alarm switches off
            Assert.Empty(set.Due(monday.AddDays(6)));           // Sunday: not on the list
            Assert.Empty(new TimerSet { Alarms = { new Alarm { Time = "07:00" } } }.Due(monday.AddMinutes(30))); // missed by too much
        }

        [Fact]
        public void BreakTracker_RemindsAfterActiveTime_ResetsWhenAway()
        {
            var t = new BreakTracker();
            var start = new DateTime(2026, 10, 4, 9, 0, 0);
            var hour = TimeSpan.FromMinutes(60);
            Assert.False(t.Update(start, TimeSpan.Zero, hour));
            Assert.False(t.Update(start.AddMinutes(59), TimeSpan.Zero, hour));
            Assert.True(t.Update(start.AddMinutes(60), TimeSpan.Zero, hour));
            Assert.False(t.Update(start.AddMinutes(90), TimeSpan.FromMinutes(6), hour)); // away = break
            Assert.False(t.Update(start.AddMinutes(140), TimeSpan.Zero, hour));
            Assert.True(t.Update(start.AddMinutes(200), TimeSpan.Zero, hour));
            Assert.False(new BreakTracker().Update(start, TimeSpan.Zero, TimeSpan.Zero)); // off
        }

        [Fact]
        public void Habit_StreakAndToggle()
        {
            var today = new DateTime(2026, 10, 4);
            var h = new Habit();
            h.Toggle(today.AddDays(-1));
            h.Toggle(today.AddDays(-2));
            Assert.Equal(2, h.Streak(today));   // today still open
            h.Toggle(today);
            Assert.Equal(3, h.Streak(today));
            h.Toggle(today.AddDays(-1));         // a gap
            Assert.Equal(1, h.Streak(today));
        }

        [Fact]
        public void Progress_Shares()
        {
            var (day, week, month, year) = ProgressWidget.Shares(new DateTime(2026, 7, 2, 12, 0, 0)); // Thursday noon, 2 July
            Assert.Equal(0.5, day, 6);
            Assert.Equal(3.5 / 7, week, 6);
            Assert.Equal(1.5 / 31, month, 6);
            Assert.Equal((182 + 0.5) / 365, year, 6);
        }

        [Fact]
        public void TimerFormat() => Assert.Equal("04:05", TimerWidget.FormatLeft(new TimeSpan(0, 4, 5)));
    }
}
