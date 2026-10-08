using System.ComponentModel;
using System.Diagnostics;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// The FPS measurement needed an elevated helper with a scheduled task and was removed in 2.11
    /// (no admin rights is safer). Whoever had it on gets the helper stopped and its task removed once;
    /// the task was created elevated, so removing it needs one more UAC prompt.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        public const string LegacyFpsTaskName = @"NoFences\FPS-Helper";

        private void RemoveLegacyFpsHelper()
        {
            if (!Store.Config.FpsHelperEnabled)
                return;
            Store.Config.FpsHelperEnabled = false;
            Store.RequestSave();
            try
            {
                // A helper that is still running stops when this file appears
                Directory.CreateDirectory(AppData.Cache);
                File.WriteAllText(Path.Combine(AppData.Cache, "fps.stop"), "stop");
            }
            catch (Exception)
            {
            }
            SynchronizationContext.Current?.Post(_ =>
            {
                if (MessageBox.Show(Strings.FpsRemoved, "NoFences", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                    return;
                try
                {
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--fps-helper-uninstall") { UseShellExecute = true, Verb = "runas" })?.Dispose();
                }
                catch (Win32Exception)
                {
                    // UAC declined; the task does nothing without the helper anyway
                }
            }, null);
        }

        /// <summary><c>NoFences.exe --fps-helper-uninstall</c> (elevated): deletes the old scheduled task.</summary>
        public static void DeleteLegacyFpsTask()
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo("schtasks.exe", $"/Delete /TN \"{LegacyFpsTaskName}\" /F") { CreateNoWindow = true, UseShellExecute = false });
                p?.WaitForExit(10_000);
            }
            catch (Exception)
            {
            }
        }
    }
}
