using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Things that happen by themselves: profile rules (a program runs, time of day), hiding fences
    /// while something runs full screen, and the light/dark default style.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly System.Windows.Forms.Timer automationTimer = new() { Interval = 1000 };
        private int automationTicks;
        private Screen? fullscreenScreen;

        /// <summary>A rule switched the profile; <see cref="profileBeforeRule"/> is restored when no rule applies any more.</summary>
        private bool ruleActive;
        private string? profileBeforeRule;
        private string? appliedTheme;

        private void InitAutomation()
        {
            automationTimer.Tick += (_, _) => AutomationTick();
            automationTimer.Start();
        }

        private void AutomationTick()
        {
            CheckFullscreen();
            // The process list and the clock don't need checking every second
            if (automationTicks++ % 3 != 0)
                return;
            ApplyProfileRules();
            ApplyAutoTheme();
            ApplyTimedWallpaper();
        }

        #region Default style (light/dark)

        /// <summary>The default style right now: fixed, or light/dark by Windows' mode or the time of day.</summary>
        public string DefaultThemeId
        {
            get
            {
                var c = Store.Config;
                if (c.AutoTheme == AutoThemeMode.Off)
                    return c.Theme;
                return ThemeSchedule.IsDark(c.AutoTheme, DateTime.Now, WindowsUsesLightTheme(), c.DarkFrom, c.DarkTo) ? c.DarkTheme : c.LightTheme;
            }
        }

        internal void ApplyAutoTheme()
        {
            var theme = DefaultThemeId;
            if (theme == appliedTheme)
                return;
            appliedTheme = theme;
            foreach (var w in windows.Where(w => w.Info.Theme == null))
                w.ApplySettings();
        }

        private static bool WindowsUsesLightTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int value && value != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Profile rules

        private void ApplyProfileRules()
        {
            var rules = Store.Config.ProfileRules;
            if (rules.Count == 0)
            {
                ruleActive = false;
                return;
            }

            var wanted = ProfileRules.Match(rules, DateTime.Now, RunningProcessNames(rules), Store.Config.Profiles);
            if (wanted != null)
            {
                if (!ruleActive)
                {
                    profileBeforeRule = ActiveProfile;
                    ruleActive = true;
                }
                if (wanted != ActiveProfile)
                    SwitchProfile(wanted, automatic: true);
            }
            else if (ruleActive)
            {
                ruleActive = false;
                if (profileBeforeRule != ActiveProfile)
                    SwitchProfile(profileBeforeRule, automatic: true);
            }
        }

        /// <summary>Names of running processes, only queried when a program rule exists.</summary>
        private static HashSet<string> RunningProcessNames(IEnumerable<ProfileRule> rules)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!rules.Any(r => r.Trigger == ProfileTrigger.Program))
                return names;
            foreach (var p in Process.GetProcesses())
            {
                try { names.Add(p.ProcessName); }
                catch (Exception) { }
                finally { p.Dispose(); }
            }
            return names;
        }

        #endregion

        #region Full screen

        private void CheckFullscreen()
        {
            var screen = Store.Config.HideOnFullscreen ? FullscreenScreen() : null;
            if (Equals(screen?.DeviceName, fullscreenScreen?.DeviceName))
                return;
            fullscreenScreen = screen;
            ApplyVisibility();
        }

        /// <summary>Hidden because a program covers this fence's monitor completely (and we're not peeking).</summary>
        private bool HiddenByFullscreen(FenceWindow w) =>
            fullscreenScreen != null && !peeking && Screen.FromRectangle(w.Bounds).DeviceName == fullscreenScreen.DeviceName;

        /// <summary>The monitor the foreground window covers completely, if any (not the desktop itself).</summary>
        private static Screen? FullscreenScreen()
        {
            var hwnd = Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero || hwnd == GetShellWindow() || Native.IsOwnWindow(hwnd))
                return null;
            var className = new StringBuilder(64);
            GetClassName(hwnd, className, className.Capacity);
            if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
                return null;
            if (!GetWindowRect(hwnd, out var r))
                return null;
            var screen = Screen.FromHandle(hwnd);
            var b = screen.Bounds;
            return r.Left <= b.Left && r.Top <= b.Top && r.Right >= b.Right && r.Bottom >= b.Bottom ? screen : null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int maxCount);

        #endregion

        private void DisposeAutomation() => automationTimer.Dispose();
    }
}
