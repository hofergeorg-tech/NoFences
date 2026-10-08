using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Keeping fences in a shared folder (OneDrive, Dropbox, a NAS …) so several PCs use the same setup.
    /// Positions are stored per monitor setup, so PCs with different screens keep their own layout.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer syncTimer = new() { Interval = 10_000 };

        private void InitSync()
        {
            if (Store.SyncFolder == null)
                return;
            syncTimer.Tick += Util.UiWatchdog.Named("Sync check", (_, _) => CheckSyncedChanges());
            syncTimer.Start();
        }

        /// <summary>Another PC saved: show its version of the fences.</summary>
        private void CheckSyncedChanges()
        {
            if (!Store.ChangedOnDisk())
                return;
            foreach (var w in windows.ToList())
            {
                w.Close();
                w.Dispose();
            }
            windows.Clear();
            Store.Load();
            appliedTheme = DefaultThemeId;
            LoadCustomThemesQuiet();
            foreach (var info in Store.Config.Fences)
                OpenWindow(info);
            ShowBalloon(Strings.SyncReloaded);
        }

        /// <summary>Asks for a folder (OneDrive suggested) and switches to it after a restart.</summary>
        internal void ChooseSyncFolder(IWin32Window owner)
        {
            var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
            using var dialog = new FolderBrowserDialog
            {
                Description = Strings.SyncChooseTitle,
                UseDescriptionForTitle = true,
                SelectedPath = oneDrive != null && Directory.Exists(oneDrive) ? oneDrive : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            var folder = dialog.SelectedPath;
            if (!Path.GetFileName(folder.TrimEnd('\\')).Equals("NoFences", StringComparison.OrdinalIgnoreCase))
                folder = Path.Combine(folder, "NoFences");

            var useExisting = false;
            if (FenceStore.HasConfig(folder))
            {
                var answer = MessageBox.Show(owner, Strings.SyncExistingQuestion, "NoFences", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel)
                    return;
                useExisting = answer == DialogResult.Yes;
            }
            try
            {
                Store.StartSync(folder, useExisting);
            }
            catch (Exception e)
            {
                MessageBox.Show(owner, e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Restart();
        }

        internal void StopSync(IWin32Window owner)
        {
            if (MessageBox.Show(owner, Strings.SyncStopQuestion, "NoFences", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                Store.StopSync();
            }
            catch (Exception e)
            {
                MessageBox.Show(owner, e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Restart();
        }

        private void DisposeSync() => syncTimer.Dispose();
    }
}
