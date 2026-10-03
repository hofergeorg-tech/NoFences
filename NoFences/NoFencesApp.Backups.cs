using System.Diagnostics;
using Microsoft.Win32;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Monitor changes (layout per setup) and configuration backups with restore.</summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer backupTimer = new() { Interval = 60 * 60 * 1000 };
        private SynchronizationContext? uiContext;
        private bool skipSaveOnExit;

        private void InitBackupsAndScreens()
        {
            uiContext = SynchronizationContext.Current;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            Store.BackupIfDue();
            backupTimer.Tick += (_, _) => Store.BackupIfDue();
            backupTimer.Start();
        }

        // Raised on a system thread; give Windows a moment to finish rearranging the screens.
        private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
            uiContext?.Post(async _ =>
            {
                await Task.Delay(1500);
                foreach (var w in windows)
                    w.ApplyLayoutForCurrentScreens();
            }, null);


        internal void RestoreBackup(string path, DateTime time)
        {
            if (MessageBox.Show(Strings.ConfirmRestore(time), "NoFences", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                Store.RestoreBackup(path);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Restart();
        }

        /// <summary>Starts a fresh instance (which waits for this one to exit) and exits without saving.</summary>
        private void Restart()
        {
            skipSaveOnExit = true;
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"--after-update {Environment.ProcessId}") { UseShellExecute = false });
            ExitThread();
        }

        private void DisposeBackupsAndScreens()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            backupTimer.Dispose();
        }
    }
}
