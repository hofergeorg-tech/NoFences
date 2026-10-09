using System.Drawing;
using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class SystemPackageTests
    {
        [Fact]
        public void Duplicates_SameContentOnlyOriginalFirst()
        {
            using var temp = new TempFolder();
            var original = temp.File("a/photo.jpg", "same content", DateTime.Now.AddDays(-10));
            var copy = temp.File("b/photo (1).jpg", "same content", DateTime.Now);
            temp.File("c/other.jpg", "different!!!"); // same size, other content
            temp.File("d/empty.txt", "");
            var files = DuplicateFinder.Scan(new[] { temp.Path, temp.Path }); // listed twice: no self-duplicates
            var groups = DuplicateFinder.Find(files, DuplicateFinder.Sha256);
            var group = Assert.Single(groups);
            Assert.Equal(new[] { original, copy }.Select(Path.GetFullPath), group.Select(f => f.Path));
        }

        [Fact]
        public void Duplicates_MostWastedSpaceFirst()
        {
            var now = DateTime.Now;
            var files = new[]
            {
                new DuplicateFinder.FileItem("small1", 10, now), new DuplicateFinder.FileItem("small2", 10, now),
                new DuplicateFinder.FileItem("big1", 1000, now), new DuplicateFinder.FileItem("big2", 1000, now),
            };
            var groups = DuplicateFinder.Find(files, p => p.StartsWith("big") ? "B" : "S");
            Assert.Equal("big1", groups[0][0].Path);
            // A file that can't be read is left out
            Assert.Empty(DuplicateFinder.Find(files, _ => null));
        }

        [Fact]
        public void Autostart_FlagsLikeTaskManager()
        {
            Assert.True(Autostart.IsEnabled(null));
            Assert.True(Autostart.IsEnabled(new byte[] { 2, 0, 0, 0 }));
            Assert.True(Autostart.IsEnabled(new byte[] { 6, 0 }));
            Assert.False(Autostart.IsEnabled(new byte[] { 3, 0, 0, 0 }));
            var off = Autostart.ApprovalValue(false, new DateTime(2026, 10, 4));
            Assert.Equal(12, off.Length);
            Assert.False(Autostart.IsEnabled(off));
            Assert.True(Autostart.IsEnabled(Autostart.ApprovalValue(true, DateTime.Now)));
            Assert.True(new AutostartEntry("a", "", AutostartSource.UserFolder, true).CanToggle);
            Assert.False(new AutostartEntry("a", "", AutostartSource.MachineRun, true).CanToggle);
            // Reading this PC's entries never throws
            Assert.NotNull(Autostart.ReadAll());
        }

        [Fact]
        public void SpeedTest_Mbit()
        {
            Assert.Equal(8, SpeedTest.Mbit(1_000_000, TimeSpan.FromSeconds(1)));
            Assert.Equal(0, SpeedTest.Mbit(100, TimeSpan.Zero));
        }

        [Fact]
        public void QrCode_DrawnWithQuietZoneAndLimits()
        {
            using var qr = QrCodeDialog.Create("https://github.com/hofergeorg-tech/NoFences", 4);
            Assert.NotNull(qr);
            Assert.Equal(qr!.Width, qr.Height);
            Assert.Equal(Color.White.ToArgb(), qr.GetPixel(0, 0).ToArgb());
            Assert.Null(QrCodeDialog.Create(""));
            Assert.Null(QrCodeDialog.Create(new string('x', 5000)));
        }

        [Fact]
        public void Magnifier_LensStaysOnScreenAndShowsTheCursorArea()
        {
            var area = Magnifier.SourceArea(new Point(500, 400), 240, 3);
            Assert.Equal(new Rectangle(460, 360, 80, 80), area);
            var screen = new Rectangle(0, 0, 1920, 1080);
            Assert.Equal(new Point(530, 430), Magnifier.LensPosition(new Point(500, 400), new Size(240, 240), screen));
            // Bottom right corner: flipped to the other side
            var flipped = Magnifier.LensPosition(new Point(1900, 1070), new Size(240, 240), screen);
            Assert.True(flipped.X + 240 <= 1920 && flipped.Y + 240 <= 1080);
        }

        [Fact]
        public void XInputLevels()
        {
            Assert.Null(DeviceBatteries.XInputLevel(1, 3));
            Assert.Equal(1.0, DeviceBatteries.XInputLevel(2, 3));
            Assert.True(DeviceBatteries.XInputLevel(2, 0) < 0.1);
        }
    }
}
