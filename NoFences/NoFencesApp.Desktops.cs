using Microsoft.Win32;
using NoFences.Model;

namespace NoFences
{
    /// <summary>
    /// Fences that only show on one virtual desktop (Win+Ctrl+arrows). Windows has no public "current
    /// desktop" event, but Explorer keeps the current desktop's id in the registry, which is polled.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer virtualDesktopTimer = new() { Interval = 700 };

        public Guid? CurrentVirtualDesktop { get; private set; }

        private void InitVirtualDesktops()
        {
            CurrentVirtualDesktop = ReadCurrentVirtualDesktop();
            virtualDesktopTimer.Tick += (_, _) =>
            {
                var current = ReadCurrentVirtualDesktop();
                if (current == CurrentVirtualDesktop)
                    return;
                CurrentVirtualDesktop = current;
                ApplyVisibility();
            };
            virtualDesktopTimer.Start();
            ApplyVisibility();
        }

        /// <summary>
        /// Shown if fences aren't hidden, the fence belongs to all desktops or the current one, and it is
        /// part of the active profile.
        /// </summary>
        private bool ShouldBeVisible(FenceInfo info) =>
            fencesVisible
            && (info.VirtualDesktop == null || CurrentVirtualDesktop == null || info.VirtualDesktop == CurrentVirtualDesktop)
            && info.InProfile(Store.Config.ActiveProfile);

        private void ApplyVisibility()
        {
            foreach (var w in windows)
            {
                var visible = ShouldBeVisible(w.Info) && !HiddenByFullscreen(w);
                if (w.Visible != visible)
                    w.Visible = visible;
            }
        }

        /// <summary>Pins a fence to the current virtual desktop, or makes it show on all again.</summary>
        public void TogglePinToDesktop(FenceInfo info)
        {
            info.VirtualDesktop = info.VirtualDesktop == null ? CurrentVirtualDesktop : null;
            Store.RequestSave();
            ApplyVisibility();
        }

        public static Guid? ReadCurrentVirtualDesktop()
        {
            try
            {
                // Windows 11 keeps it here; Windows 10 per session under SessionInfo.
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops");
                if (key?.GetValue("CurrentVirtualDesktop") is byte[] { Length: 16 } bytes)
                    return new Guid(bytes);

                using var sessions = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\SessionInfo");
                var session = System.Diagnostics.Process.GetCurrentProcess().SessionId.ToString();
                using var old = sessions?.OpenSubKey($@"{session}\VirtualDesktops");
                if (old?.GetValue("CurrentVirtualDesktop") is byte[] { Length: 16 } oldBytes)
                    return new Guid(oldBytes);
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
}
