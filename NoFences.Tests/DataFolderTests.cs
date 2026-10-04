using NoFences.Model;

namespace NoFences.Tests
{
    public class DataFolderTests
    {
        private static void WriteFlatData(TempFolder dir, string root = "")
        {
            dir.File(Path.Combine(root, "fences.json"), "{}");
            dir.File(Path.Combine(root, "playtime.json"), "{}");
            dir.File(Path.Combine(root, "log.txt"), "log");
            dir.File(Path.Combine(root, @"backups\fences-1.json"), "{}");
            dir.File(Path.Combine(root, @"themes\mine.json"), "{}");
            dir.File(Path.Combine(root, @"note-media\a.png"), "png");
            dir.File(Path.Combine(root, @"Shelf\parked.txt"), "x");
            dir.File(Path.Combine(root, "fps.json"), "{}");
        }

        [Fact]
        public void Prepare_CopiesOldDataNextToTheExeSorted()
        {
            using var dir = new TempFolder();
            WriteFlatData(dir, "legacy");
            var exe = Path.Combine(dir.Path, "exe");
            var legacy = Path.Combine(dir.Path, "legacy");
            Directory.CreateDirectory(exe);

            var folder = DataFolder.Prepare(exe, legacy, exeFolderUsable: true);

            Assert.True(folder.BesideExe);
            Assert.True(File.Exists(Path.Combine(exe, @"config\fences.json")));
            Assert.True(File.Exists(Path.Combine(exe, @"config\playtime.json")));
            Assert.True(File.Exists(Path.Combine(exe, @"logs\log.txt")));
            Assert.True(File.Exists(Path.Combine(exe, @"backups\fences-1.json")));
            Assert.True(File.Exists(Path.Combine(exe, @"themes\mine.json")));
            Assert.True(File.Exists(Path.Combine(exe, @"media\note-media\a.png")));
            Assert.True(File.Exists(Path.Combine(exe, @"media\Shelf\parked.txt")));
            Assert.False(File.Exists(Path.Combine(exe, @"cache\fps.json"))); // temporary, not copied
            // The old folder stays as a copy, marked so it isn't copied again
            Assert.True(File.Exists(Path.Combine(legacy, "fences.json")));
            Assert.True(File.Exists(Path.Combine(legacy, DataFolder.MovedMarker)));
            Assert.Equal(Path.Combine(exe, @"media\Shelf\x.txt"), folder.MapMovedPath(Path.Combine(legacy, @"Shelf\x.txt")));
            Assert.Equal(@"C:\Elsewhere", folder.MapMovedPath(@"C:\Elsewhere"));
        }

        [Fact]
        public void Prepare_StaysInLocalAppDataWhenTheExeFolderIsNotUsable()
        {
            using var dir = new TempFolder();
            WriteFlatData(dir, "legacy");
            var exe = Path.Combine(dir.Path, "exe");
            var legacy = Path.Combine(dir.Path, "legacy");
            Directory.CreateDirectory(exe);

            var folder = DataFolder.Prepare(exe, legacy, exeFolderUsable: false);

            Assert.False(folder.BesideExe);
            Assert.Equal(legacy, folder.Root);
            // Sorted in place
            Assert.True(File.Exists(Path.Combine(legacy, @"config\fences.json")));
            Assert.False(File.Exists(Path.Combine(legacy, "fences.json")));
            Assert.True(File.Exists(Path.Combine(legacy, @"media\Shelf\parked.txt")));
            Assert.False(Directory.Exists(Path.Combine(exe, "config")));
        }

        [Fact]
        public void Prepare_SortsOldPortableDataInPlace()
        {
            using var dir = new TempFolder();
            WriteFlatData(dir, "exe");
            var exe = Path.Combine(dir.Path, "exe");
            var legacy = Path.Combine(dir.Path, "legacy");

            // Even if the folder looks unusable: the portable data there is the user's
            var folder = DataFolder.Prepare(exe, legacy, exeFolderUsable: false);

            Assert.True(folder.BesideExe);
            Assert.True(File.Exists(Path.Combine(exe, @"config\fences.json")));
            Assert.False(File.Exists(Path.Combine(exe, "fences.json")));
            Assert.False(Directory.Exists(legacy));
        }

        [Fact]
        public void Prepare_DoesNotCopyTwice()
        {
            using var dir = new TempFolder();
            WriteFlatData(dir, "legacy");
            var exe = Path.Combine(dir.Path, "exe");
            var other = Path.Combine(dir.Path, "other-exe");
            var legacy = Path.Combine(dir.Path, "legacy");
            Directory.CreateDirectory(exe);
            Directory.CreateDirectory(other);

            DataFolder.Prepare(exe, legacy, exeFolderUsable: true);
            DataFolder.Prepare(other, legacy, exeFolderUsable: true);

            // A second copy of NoFences.exe elsewhere starts fresh instead of taking the old data again
            Assert.False(File.Exists(Path.Combine(other, @"config\fences.json")));
            Assert.True(Directory.Exists(Path.Combine(other, "config")));
        }

        [Fact]
        public void Find_UsesTheExeFolderOnlyWhenItHasData()
        {
            using var dir = new TempFolder();
            var exe = Path.Combine(dir.Path, "exe");
            var legacy = Path.Combine(dir.Path, "legacy");
            Assert.Equal(legacy, DataFolder.Find(exe, legacy).Root);
            Directory.CreateDirectory(Path.Combine(exe, "config"));
            Assert.Equal(exe, DataFolder.Find(exe, legacy).Root);
        }

        [Fact]
        public void IsProtected_ProgramFilesAndWinGet()
        {
            Assert.True(DataFolder.IsProtected(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NoFences")));
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Assert.True(DataFolder.IsProtected(Path.Combine(local, @"Microsoft\WinGet\Packages\hofergeorg-tech.NoFences")));
            Assert.False(DataFolder.IsProtected(@"D:\Tools\NoFences"));
        }

        [Fact]
        public void Store_MovedShelfFencePointsToTheNewPlace()
        {
            using var dir = new TempFolder();
            var legacy = Path.Combine(dir.Path, "legacy");
            var exe = Path.Combine(dir.Path, "exe");
            Directory.CreateDirectory(exe);
            dir.File(@"legacy\Shelf\parked.txt");
            var oldStore = new FenceStore(legacy);
            oldStore.Load();
            oldStore.Config.Fences.Add(new FenceInfo { Name = "Ablage", Kind = FenceKind.Folder, FolderPath = Path.Combine(legacy, "Shelf") });
            oldStore.SaveNow();
            // Back to the flat layout of 2.5
            File.Move(Path.Combine(legacy, @"config\fences.json"), Path.Combine(legacy, "fences.json"));
            Directory.Delete(Path.Combine(legacy, "config"));

            var store = new FenceStore(DataFolder.Prepare(exe, legacy, exeFolderUsable: true));
            store.Load();

            Assert.Equal(Path.Combine(exe, @"media\Shelf"), Assert.Single(store.Config.Fences).FolderPath);
        }
    }
}
