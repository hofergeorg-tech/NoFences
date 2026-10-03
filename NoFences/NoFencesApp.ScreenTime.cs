using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NoFences.Model;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Screen time: every 5 seconds, adds the program in front to usage.json – only while a screen time
    /// widget exists, and not while nobody has touched mouse or keyboard for 5 minutes.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private const int ScreenTimeTick = 5;
        private readonly System.Windows.Forms.Timer screenTimeTimer = new() { Interval = ScreenTimeTick * 1000 };
        private readonly Dictionary<string, string> programNames = new(StringComparer.OrdinalIgnoreCase);
        private UsageLog? usage;
        private DateTime usageSaved = DateTime.Now;
        private bool usageDirty;

        private string UsagePath => Path.Combine(Store.DataDirectory, "usage.json");

        public UsageLog ScreenTime => usage ??= UsageLog.Load(UsagePath);

        private void InitScreenTime()
        {
            screenTimeTimer.Tick += (_, _) => TrackScreenTime();
            screenTimeTimer.Start();
        }

        private void TrackScreenTime()
        {
            if (!Store.Config.Fences.Any(f => f.Kind == FenceKind.Widget && f.WidgetType == "screentime"))
                return;
            if (IdleTime() > TimeSpan.FromMinutes(5))
                return;
            var exe = ForegroundExe();
            if (exe == null)
                return;
            if (!programNames.TryGetValue(exe, out var name))
                programNames[exe] = name = ProgramName(exe);
            ScreenTime.Add(DateTime.Now, exe, name, ScreenTimeTick);
            usageDirty = true;
            if (DateTime.Now - usageSaved > TimeSpan.FromMinutes(1))
                SaveScreenTime();
        }

        private void SaveScreenTime()
        {
            if (usage == null || !usageDirty)
                return;
            usage.Save(UsagePath);
            usageSaved = DateTime.Now;
            usageDirty = false;
        }

        /// <summary>The exe of the window in front; null for the desktop, NoFences itself or when unknown.</summary>
        private static string? ForegroundExe()
        {
            var hwnd = Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero || Native.IsOwnWindow(hwnd))
                return null;
            var cls = new StringBuilder(64);
            GetClassName(hwnd, cls, cls.Capacity);
            if (cls.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd")
                return null;
            Native.GetWindowThreadProcessId(hwnd, out var pid);
            var process = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, pid);
            if (process == IntPtr.Zero)
                return null;
            try
            {
                var path = new StringBuilder(1024);
                var size = path.Capacity;
                return QueryFullProcessImageName(process, 0, path, ref size) ? path.ToString() : null;
            }
            finally
            {
                CloseHandle(process);
            }
        }

        /// <summary>"Google Chrome" rather than "chrome.exe".</summary>
        private static string ProgramName(string exe)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(exe);
                var name = !string.IsNullOrWhiteSpace(info.FileDescription) ? info.FileDescription : info.ProductName;
                if (!string.IsNullOrWhiteSpace(name) && name.Length <= 40)
                    return name.Trim();
            }
            catch (Exception) { }
            return Path.GetFileNameWithoutExtension(exe);
        }

        private static TimeSpan IdleTime()
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            return GetLastInputInfo(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime)) : TimeSpan.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        private void DisposeScreenTime()
        {
            screenTimeTimer.Dispose();
            SaveScreenTime();
        }
    }
}
