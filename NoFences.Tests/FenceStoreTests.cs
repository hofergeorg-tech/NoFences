using NoFences.Model;

namespace NoFences.Tests
{
    public class FenceStoreTests
    {
        [Fact]
        public void SaveAndLoad_RoundTripsAllSettings()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            store.Config.Theme = "nerd";
            store.Config.Fences.Add(new FenceInfo
            {
                Name = "Notiz",
                Kind = FenceKind.Note,
                NoteText = "[ ] a\nb",
                ReminderAt = new DateTime(2026, 10, 2, 9, 0, 0),
                SortMode = FenceSortMode.Size,
                AlwaysOnTop = true,
                Layouts = { ["0,0,1920,1080"] = new[] { 1, 2, 3, 4 } }
            });
            store.SaveNow();

            var reloaded = new FenceStore(dir.Path);
            reloaded.Load();
            var fence = Assert.Single(reloaded.Config.Fences);
            Assert.Equal("nerd", reloaded.Config.Theme);
            Assert.Equal(FenceKind.Note, fence.Kind);
            Assert.Equal("[ ] a\nb", fence.NoteText);
            Assert.Equal(new DateTime(2026, 10, 2, 9, 0, 0), fence.ReminderAt);
            Assert.Equal(FenceSortMode.Size, fence.SortMode);
            Assert.True(fence.AlwaysOnTop);
            Assert.Equal(new[] { 1, 2, 3, 4 }, fence.Layouts["0,0,1920,1080"]);
        }

        [Fact]
        public void Load_MigratesFencesFromVersion1()
        {
            using var dir = new TempFolder();
            var id = Guid.NewGuid();
            dir.File(Path.Combine(id.ToString(), "__fence_metadata.xml"), $"""
                <?xml version="1.0" encoding="utf-8"?>
                <FenceInfo xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Id>{id}</Id>
                  <Name>Alt</Name>
                  <PosX>10</PosX>
                  <PosY>20</PosY>
                  <Width>300</Width>
                  <Height>200</Height>
                  <Locked>true</Locked>
                  <CanMinify>false</CanMinify>
                  <TitleHeight>40</TitleHeight>
                  <Files><string>C:\Windows\notepad.exe</string></Files>
                </FenceInfo>
                """);

            var store = new FenceStore(dir.Path);
            store.Load();

            var fence = Assert.Single(store.Config.Fences);
            Assert.Equal(id, fence.Id);
            Assert.Equal("Alt", fence.Name);
            Assert.Equal(10, fence.PosX);
            Assert.True(fence.Locked);
            Assert.Equal(40, fence.TitleHeight);
            Assert.Equal(@"C:\Windows\notepad.exe", Assert.Single(fence.Files));
            Assert.True(File.Exists(Path.Combine(dir.Path, "config", "fences.json")));
        }

        [Fact]
        public void Load_FirstStartWithoutAnyConfig_Works()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            Assert.Empty(store.Config.Fences);
        }

        [Fact]
        public void Load_KeepsBrokenConfigAndStartsEmpty()
        {
            using var dir = new TempFolder();
            dir.File(@"config\fences.json", "{ this is not json");
            var store = new FenceStore(dir.Path);
            store.Load();
            Assert.Empty(store.Config.Fences);
            Assert.Contains(Directory.GetFiles(Path.Combine(dir.Path, "config")), f => Path.GetFileName(f).StartsWith("fences.json.broken-"));
        }

        [Fact]
        public void Backups_AreCreatedAndLimitedToTen()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            store.Config.Fences.Add(new FenceInfo { Name = "A" });
            store.SaveNow();

            for (var i = 0; i < 12; i++)
                dir.File($@"backups\fences-old-{i:00}.json", "{}", DateTime.Now.AddDays(-30 + i));
            store.BackupIfDue();

            var backups = store.ListBackups().ToList();
            Assert.Equal(10, backups.Count);
            Assert.True(backups[0].Time > DateTime.Now.AddMinutes(-1)); // the new one is the newest
        }

        [Fact]
        public void Backups_NotRepeatedWithinTwelveHours()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            store.SaveNow();
            store.BackupIfDue();
            store.BackupIfDue();
            Assert.Single(store.ListBackups());
        }

        [Fact]
        public void RestoreBackup_ReplacesConfigAndKeepsCurrentState()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            store.Config.Fences.Add(new FenceInfo { Name = "Vorher" });
            store.SaveNow();
            store.BackupIfDue();
            var backup = store.ListBackups().First().Path;

            store.Config.Fences.Clear();
            store.Config.Fences.Add(new FenceInfo { Name = "Nachher" });
            store.RestoreBackup(backup);

            var reloaded = new FenceStore(dir.Path);
            reloaded.Load();
            Assert.Equal("Vorher", Assert.Single(reloaded.Config.Fences).Name);
            Assert.Contains(store.ListBackups(), b => b.Path.EndsWith("-before-restore.json"));
        }

        [Fact]
        public void RestoreBackup_RejectsBrokenFiles()
        {
            using var dir = new TempFolder();
            var store = new FenceStore(dir.Path);
            store.Load();
            store.SaveNow();
            var broken = dir.File("broken.json", "nope");
            Assert.ThrowsAny<Exception>(() => store.RestoreBackup(broken));
        }
    }
}
