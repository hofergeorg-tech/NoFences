using System.Diagnostics;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Looks for new GitHub releases and installs them with one click.</summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer firstUpdateCheck = new() { Interval = 20_000 };
        private readonly System.Windows.Forms.Timer updateTimer = new() { Interval = 6 * 60 * 60 * 1000 };
        private ReleaseInfo? availableUpdate;
        private Version? notifiedVersion;
        private bool installing;

        private void InitUpdates()
        {
            // Check a little after startup (not while Windows is still busy logging in), then every 6 hours.
            firstUpdateCheck.Tick += async (_, _) =>
            {
                firstUpdateCheck.Stop();
                await CheckForUpdatesAsync(manual: false);
                updateTimer.Start();
            };
            updateTimer.Tick += async (_, _) => await CheckForUpdatesAsync(manual: false);
            firstUpdateCheck.Start();
        }

        internal async Task CheckForUpdatesAsync(bool manual)
        {
            if (!manual && !Store.Config.CheckForUpdates)
                return;

            ReleaseInfo? latest;
            try
            {
                latest = await UpdateChecker.GetLatestAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Update check failed: {e.Message}");
                if (manual)
                    ShowBalloon(Strings.UpdateCheckFailed);
                return;
            }

            if (latest != null && UpdateChecker.IsNewer(latest))
            {
                availableUpdate = latest;
                if (manual || notifiedVersion != latest.Version)
                {
                    notifiedVersion = latest.Version;
                    var text = UpdateChecker.CanSelfUpdate && latest.ExeUrl != null
                        ? Strings.UpdateAvailable(latest.Version)
                        : Strings.UpdateAvailableManual(latest.Version);
                    ShowBalloon(text, InstallUpdate, timeout: 10_000);
                }
            }
            else if (manual)
            {
                ShowBalloon(Strings.UpToDate(UpdateChecker.CurrentVersion));
            }
        }

        internal async void InstallUpdate()
        {
            var release = availableUpdate;
            if (release == null || installing)
                return;

            // Development builds and unusual installs: just show the release page.
            if (!UpdateChecker.CanSelfUpdate || release.ExeUrl == null)
            {
                OpenUrl(release.PageUrl);
                return;
            }

            installing = true;
            try
            {
                ShowBalloon(Strings.UpdateDownloading);
                await UpdateChecker.InstallAsync(release);
                Store.SaveNow();
                ExitThread(); // the new version is already starting and waits for us to exit
            }
            catch (Exception e)
            {
                installing = false;
                MessageBox.Show(Strings.UpdateFailed(e.Message), "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                OpenUrl(release.PageUrl);
            }
        }


        private void DisposeUpdates()
        {
            firstUpdateCheck.Dispose();
            updateTimer.Dispose();
        }
    }
}
