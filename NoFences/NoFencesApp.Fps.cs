using System.ComponentModel;
using System.Diagnostics;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Starting/stopping the opt-in FPS helper (see <see cref="FpsHelper"/>).</summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer fpsStartDelay = new() { Interval = 5000 };

        public bool FpsEnabled => Store.Config.FpsHelperEnabled;

        private void InitFps()
        {
            fpsStartDelay.Tick += (_, _) =>
            {
                fpsStartDelay.Stop();
                StartFpsHelper(interactive: false);
            };
            if (FpsEnabled)
                fpsStartDelay.Start();
        }

        public void ToggleFps()
        {
            if (FpsEnabled)
            {
                Store.Config.FpsHelperEnabled = false;
                Store.RequestSave();
                StopFpsHelper();
                // Removing the scheduled task needs admin rights again (one more UAC prompt).
                RunElevated("--fps-helper-uninstall");
                return;
            }

            if (MessageBox.Show(Strings.FpsExplanation, Strings.FpsTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return;
            Store.Config.FpsHelperEnabled = true;
            Store.RequestSave();
            StartFpsHelper(interactive: true);
        }

        /// <summary>Starts via the scheduled task (no prompt) or, the first time, elevated with a UAC prompt.</summary>
        private void StartFpsHelper(bool interactive)
        {
            try { File.Delete(FpsHelper.StopFile); } catch { }

            if (FpsHelper.RunSchtasks($"/Run /TN \"{FpsHelper.TaskName}\"") == 0)
                return;

            if (!RunElevated($"--fps-helper {Environment.ProcessId} --register-task"))
            {
                // UAC declined: leave it off so NoFences doesn't ask at every start.
                Store.Config.FpsHelperEnabled = false;
                Store.RequestSave();
                if (interactive)
                    ShowBalloon(Strings.FpsDeclined);
            }
        }

        private static void StopFpsHelper()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FpsHelper.StopFile)!);
                File.WriteAllText(FpsHelper.StopFile, "stop");
            }
            catch { }
        }

        /// <summary>Starts this exe elevated; false if the user declined the UAC prompt.</summary>
        private static bool RunElevated(string arguments)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath!, arguments) { UseShellExecute = true, Verb = "runas" });
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private void DisposeFps()
        {
            fpsStartDelay.Dispose();
            // The helper also notices on its own that NoFences is gone.
            if (FpsEnabled)
                StopFpsHelper();
        }
    }
}
