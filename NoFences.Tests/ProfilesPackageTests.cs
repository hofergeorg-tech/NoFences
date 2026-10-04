using System.Text.Json;
using NoFences.Model;

namespace NoFences.Tests
{
    public class ProfilesPackageTests
    {
        private static readonly List<TimedWallpaper> Plan = new()
        {
            new() { From = "19:00", Image = "evening.jpg" },
            new() { From = "07:00", Image = "morning.jpg" },
            new() { From = "12:00", Image = "day.jpg" },
        };

        [Theory]
        [InlineData(8, "morning.jpg")]
        [InlineData(12, "day.jpg")]
        [InlineData(18, "day.jpg")]
        [InlineData(23, "evening.jpg")]
        [InlineData(3, "evening.jpg")] // before the first entry: still last night's
        public void TimedWallpaper_PicksTheEntryThatStartedLast(int hour, string expected)
        {
            Assert.Equal(expected, WallpaperSchedule.Current(Plan, new DateTime(2026, 10, 4, hour, 30, 0)));
        }

        [Fact]
        public void TimedWallpaper_ProfileWallpaperWins()
        {
            var now = new DateTime(2026, 10, 4, 9, 0, 0);
            Assert.Equal("gaming.jpg", WallpaperSchedule.Wanted("gaming.jpg", Plan, now));
            Assert.Equal("morning.jpg", WallpaperSchedule.Wanted(null, Plan, now));
            Assert.Null(WallpaperSchedule.Wanted(null, new List<TimedWallpaper>(), now));
        }

        [Fact]
        public void ProfilePrograms_StartOnlyWhatIsNotRunning()
        {
            var programs = new[] { new ProfileProgram { Path = @"C:\Steam\steam.exe" }, new ProfileProgram { Path = @"C:\Discord\Discord.exe" } };
            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "steam" };
            Assert.Equal(new[] { "Discord" }, ProfileProgramPlan.ToStart(programs, running).Select(p => p.ProcessName));
        }

        [Fact]
        public void ProfilePrograms_CloseOnlyMarkedOwnAndUnneeded()
        {
            var steam = new ProfileProgram { Path = @"C:\Steam\steam.exe", CloseOnLeave = true };
            var discord = new ProfileProgram { Path = @"C:\Discord\Discord.exe", CloseOnLeave = true };
            var obs = new ProfileProgram { Path = @"C:\OBS\obs64.exe", CloseOnLeave = false };
            var started = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "steam", "Discord", "obs64" };
            // Discord is wanted by the new profile too, OBS isn't marked
            var close = ProfileProgramPlan.ToClose(new[] { steam, discord, obs }, new[] { new ProfileProgram { Path = @"D:\Discord.exe" } }, started);
            Assert.Equal(new[] { "steam" }, close);
            // Not started by NoFences: left alone
            Assert.Empty(ProfileProgramPlan.ToClose(new[] { steam }, Array.Empty<ProfileProgram>(), new HashSet<string>()));
        }

        [Fact]
        public void NewSettingsSurviveSaving()
        {
            var config = new AppConfig
            {
                TimedWallpapers = { new TimedWallpaper { From = "07:00", Image = @"C:\a.jpg" } },
                ProfilePrograms = { ["Gaming"] = new() { new ProfileProgram { Path = @"C:\Steam\steam.exe", CloseOnLeave = true } } }
            };
            var copy = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config, FenceStore.JsonOptions), FenceStore.JsonOptions)!;
            Assert.Equal(@"C:\a.jpg", copy.TimedWallpapers.Single().Image);
            Assert.True(copy.ProfilePrograms["Gaming"].Single().CloseOnLeave);
        }
    }
}

