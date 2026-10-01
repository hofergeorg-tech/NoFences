using Microsoft.Win32;

namespace NoFences.Util
{
    public static class SystemSettings
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "NoFences";

        public static bool AutostartEnabled
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(RunValue) is string;
            }
            set
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKey);
                if (value)
                    key.SetValue(RunValue, $"\"{Environment.ProcessPath}\"");
                else
                    key.DeleteValue(RunValue, throwOnMissingValue: false);
            }
        }

        /// <summary>Explorer's "File name extensions" checkbox.</summary>
        public static bool ExplorerShowsExtensions
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                return key?.GetValue("HideFileExt") is int hide && hide == 0;
            }
        }

        /// <summary>The "Apps" dark/light setting.</summary>
        public static bool AppsUseDarkTheme
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
            }
        }
    }
}
