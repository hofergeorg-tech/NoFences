using NoFences.Model;

namespace NoFences.Tests
{
    public class SmallPackageTests
    {
        // Sunday, 4 October 2026, 10:00
        private static readonly DateTime Now = new(2026, 10, 4, 10, 0, 0);

        [Theory]
        [InlineData("Mo 14:00 Zahnarzt", "de", "2026-10-05 14:00", "Zahnarzt")]
        [InlineData("Montag 9:30 Meeting", "de", "2026-10-05 09:30", "Meeting")]
        [InlineData("morgen 8:15 Bäcker", "de", "2026-10-05 08:15", "Bäcker")]
        [InlineData("übermorgen 18 Uhr Kino", "de", "2026-10-06 18:00", "Kino")]
        [InlineData("12.10. 15:00 Friseur", "de", "2026-10-12 15:00", "Friseur")]
        [InlineData("[ ] 2026-10-20 8:15 Flug nach Wien", "de", "2026-10-20 08:15", "Flug nach Wien")]
        [InlineData("15:00 Call", "de", "2026-10-04 15:00", "Call")]
        [InlineData("09:00 Laufen", "de", "2026-10-05 09:00", "Laufen")] // passed today: tomorrow
        [InlineData("So 9:00 Markt", "de", "2026-10-11 09:00", "Markt")] // today, but passed: next week
        [InlineData("So 11:00 Markt", "de", "2026-10-04 11:00", "Markt")]
        [InlineData("Tue 2pm dentist", "en", "2026-10-06 14:00", "dentist")]
        [InlineData("tomorrow 7:30am gym", "en", "2026-10-05 07:30", "gym")]
        [InlineData("domani 14h30 riunione", "it", "2026-10-05 14:30", "riunione")]
        [InlineData("lundi 10h dentiste", "fr", "2026-10-05 10:00", "dentiste")]
        [InlineData("mañana 16:00 médico", "es", "2026-10-05 16:00", "médico")]
        [InlineData("2.1. 10:00 Neujahrsbrunch", "de", "2027-01-02 10:00", "Neujahrsbrunch")] // past date without year: next year
        public void FindsAppointments(string line, string language, string expected, string title)
        {
            var found = Assert.Single(NoteAppointments.Find(line, Now, language));
            Assert.Equal(DateTime.Parse(expected), found.When);
            Assert.Equal(title, found.Title);
            Assert.Equal(line, found.Line);
        }

        [Theory]
        [InlineData("Einkaufen: Milch, Brot")]
        [InlineData("Rezept: Nudeln 10 Minuten kochen")]
        [InlineData("Version 2.5.0 ist da")]
        [InlineData("Treffen um 9.30")]                 // no ':' – could be anything
        [InlineData("Notiz 15:00")]                      // a time in the middle without a day
        [InlineData("[x] Mo 14:00 Zahnarzt")]            // done
        [InlineData("1.10.2026 14:00 schon vorbei")]      // date with year in the past
        public void IgnoresOtherLines(string line)
        {
            Assert.Empty(NoteAppointments.Find(line, Now, "de"));
        }

        [Fact]
        public void FindsSeveralLinesOfANote()
        {
            var text = "Diese Woche\nMo 14:00 Zahnarzt\n- Di 18:30 Sport\nEinkaufen";
            var found = NoteAppointments.Find(text, Now, "de");
            Assert.Equal(new[] { "Zahnarzt", "Sport" }, found.Select(f => f.Title));
        }

        [Fact]
        public void Password_HasTheLengthAndEveryKind()
        {
            for (var i = 0; i < 50; i++)
            {
                var p = PasswordGenerator.Generate(12);
                Assert.Equal(12, p.Length);
                Assert.Contains(p, char.IsUpper);
                Assert.Contains(p, char.IsLower);
                Assert.Contains(p, char.IsDigit);
                Assert.Contains(p, c => PasswordGenerator.Symbols.Contains(c));
                Assert.DoesNotContain(p, c => PasswordGenerator.Ambiguous.Contains(c));
            }
        }

        [Fact]
        public void Password_OnlyChosenKinds_AndLimits()
        {
            var digits = PasswordGenerator.Generate(20, upper: false, lower: false, symbols: false, avoidAmbiguous: false);
            Assert.All(digits, c => Assert.True(char.IsDigit(c)));
            Assert.Equal(PasswordGenerator.MinLength, PasswordGenerator.Generate(2).Length);
            Assert.Equal(PasswordGenerator.MaxLength, PasswordGenerator.Generate(500).Length);
            Assert.NotEqual(PasswordGenerator.Generate(20), PasswordGenerator.Generate(20));
            Assert.Equal(124, PasswordGenerator.Bits(20, true, true, true, true)); // 20 × log2(75)
        }

        [Fact]
        public void Mac_IsFormattedOrHidden()
        {
            Assert.Equal("00-1A-2B-3C-4D-5E", NetworkInfo.FormatMac(new byte[] { 0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E }));
            Assert.Null(NetworkInfo.FormatMac(new byte[6]));
            Assert.Null(NetworkInfo.FormatMac(Array.Empty<byte>()));
        }

        [Fact]
        public void Adapters_AreListedWithoutError()
        {
            var adapters = NetworkInfo.Adapters();
            Assert.All(adapters, a => Assert.True(a.IPv4.Count + a.IPv6.Count > 0));
            NetworkInfo.WifiConnections(); // no exception, also without Wi-Fi
        }

        [Fact]
        public void Snooze_AddsATimerThatRingsLater()
        {
            var set = new TimerSet();
            set.Snooze("Pizza", 5, Now);
            Assert.Empty(set.Due(Now.AddMinutes(4)));
            Assert.Equal(new[] { "Pizza" }, set.Due(Now.AddMinutes(5)));
        }

        [Fact]
        public void Usage_LeastOpenedFirst()
        {
            var info = new FenceInfo();
            info.CountOpen(@"C:\a.txt");
            info.CountOpen(@"C:\a.txt");
            info.CountOpen(@"C:\c.txt");
            var usage = FenceStatsDialog.Usage(info, new[] { @"C:\a.txt", @"C:\b.txt", @"C:\c.txt" });
            Assert.Equal(new[] { (@"C:\b.txt", 0), (@"C:\c.txt", 1), (@"C:\a.txt", 2) }, usage);
        }

        [Fact]
        public void FolderChildren_FoldersFirstWithoutHiddenFiles()
        {
            using var dir = new TempFolder();
            dir.File("b.txt");
            dir.File("a.txt");
            Directory.CreateDirectory(Path.Combine(dir.Path, "zz"));
            var hidden = dir.File("hidden.txt");
            File.SetAttributes(hidden, FileAttributes.Hidden);

            var children = FenceWindow.FolderChildren(dir.Path);

            Assert.Equal(new[] { "zz", "a.txt", "b.txt" }, children.Select(c => Path.GetFileName(c.Path)));
            Assert.Empty(FenceWindow.FolderChildren(Path.Combine(dir.Path, "missing")));
        }
    }
}
