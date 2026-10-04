using Microsoft.Win32;

namespace NoFences.Model
{
    public enum AutostartSource { UserRun, MachineRun, UserFolder, MachineFolder }

    public sealed record AutostartEntry(string Name, string Command, AutostartSource Source, bool Enabled)
    {
        /// <summary>Machine-wide entries need admin rights to switch; NoFences only shows them.</summary>
        public bool CanToggle => Source is AutostartSource.UserRun or AutostartSource.UserFolder;
    }

    /// <summary>
    /// Windows' autostart programs, switched on and off the same way Task Manager does it: the entries
    /// stay, a flag under "StartupApproved" says whether they run.
    /// </summary>
    public static class Autostart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

        /// <summary>Task Manager's flag: even first byte = enabled, odd = disabled; no flag = enabled.</summary>
        public static bool IsEnabled(byte[]? approval) => approval is not { Length: > 0 } || (approval[0] & 1) == 0;

        /// <summary>The 12-byte flag: state plus, when disabled, the time it was switched off.</summary>
        public static byte[] ApprovalValue(bool enabled, DateTime now)
        {
            var value = new byte[12];
            value[0] = enabled ? (byte)2 : (byte)3;
            if (!enabled)
                BitConverter.GetBytes(now.ToFileTimeUtc()).CopyTo(value, 4);
            return value;
        }

        public static List<AutostartEntry> ReadAll()
        {
            var result = new List<AutostartEntry>();
            ReadRun(Registry.CurrentUser, RunKey, AutostartSource.UserRun, "Run", result);
            ReadRun(Registry.LocalMachine, RunKey, AutostartSource.MachineRun, "Run", result);
            ReadRun(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", AutostartSource.MachineRun, "Run32", result);
            ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Registry.CurrentUser, AutostartSource.UserFolder, result);
            ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), Registry.LocalMachine, AutostartSource.MachineFolder, result);
            return result.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static void ReadRun(RegistryKey root, string path, AutostartSource source, string approvedName, List<AutostartEntry> result)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                using var approved = root.OpenSubKey(ApprovedKey + approvedName);
                if (key == null)
                    return;
                foreach (var name in key.GetValueNames().Where(n => n.Length > 0))
                    result.Add(new AutostartEntry(name, key.GetValue(name)?.ToString() ?? "", source, IsEnabled(approved?.GetValue(name) as byte[])));
            }
            catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
            }
        }

        private static void ReadFolder(string folder, RegistryKey root, AutostartSource source, List<AutostartEntry> result)
        {
            if (!Directory.Exists(folder))
                return;
            try
            {
                using var approved = root.OpenSubKey(ApprovedKey + "StartupFolder");
                foreach (var file in Directory.EnumerateFiles(folder).Where(f => !Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)))
                {
                    var name = Path.GetFileName(file);
                    result.Add(new AutostartEntry(Path.GetFileNameWithoutExtension(file), file, source, IsEnabled(approved?.GetValue(name) as byte[])));
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
            }
        }

        /// <summary>Switches a user entry on or off; false if not possible.</summary>
        public static bool SetEnabled(AutostartEntry entry, bool enabled)
        {
            if (!entry.CanToggle)
                return false;
            try
            {
                var (sub, valueName) = entry.Source == AutostartSource.UserRun
                    ? ("Run", entry.Name)
                    : ("StartupFolder", Path.GetFileName(entry.Command));
                using var key = Registry.CurrentUser.CreateSubKey(ApprovedKey + sub);
                key.SetValue(valueName, ApprovalValue(enabled, DateTime.Now), RegistryValueKind.Binary);
                return true;
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                return false;
            }
        }
    }
}
