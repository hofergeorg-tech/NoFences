using System.Diagnostics;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Programs that start with a profile ("Gaming" → Steam, Discord) and, if wanted, close again when
    /// switching to another profile – but only those NoFences started itself, and only politely
    /// (like clicking the window's close button), never killed.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private readonly HashSet<string> startedByProfile = new(StringComparer.OrdinalIgnoreCase);

        private List<ProfileProgram> ProgramsOf(string? profile) =>
            profile != null && Store.Config.ProfilePrograms.TryGetValue(profile, out var list) ? list : new();

        private void RunProfilePrograms(string? oldProfile, string? newProfile)
        {
            var oldPrograms = ProgramsOf(oldProfile);
            var newPrograms = ProgramsOf(newProfile);
            if (oldPrograms.Count == 0 && newPrograms.Count == 0)
                return;

            foreach (var name in ProfileProgramPlan.ToClose(oldPrograms, newPrograms, startedByProfile))
            {
                startedByProfile.Remove(name);
                foreach (var process in Process.GetProcessesByName(name))
                {
                    try { process.CloseMainWindow(); }
                    catch (Exception) { }
                    finally { process.Dispose(); }
                }
            }

            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                try { running.Add(p.ProcessName); }
                catch (Exception) { }
                finally { p.Dispose(); }
            }
            foreach (var program in ProfileProgramPlan.ToStart(newPrograms, running))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(program.Path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(program.Path) ?? "" })?.Dispose();
                    startedByProfile.Add(program.ProcessName);
                }
                catch (Exception e)
                {
                    Log.Write("Profile programs", $"{program.Path}: {Log.Describe(e)}");
                }
            }
        }

        /// <summary>Tray, in the profile menu: "Programs for 'Gaming' ▸" with add, remove and "close on leave".</summary>
        private void AddProfileProgramItems(ToolStripItemCollection items, string profile)
        {
            var menu = new ToolStripMenuItem(Strings.ProfilePrograms(profile));
            foreach (var program in ProgramsOf(profile).ToList())
            {
                var entry = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(program.Path));
                entry.DropDownItems.Add(new ToolStripMenuItem(Strings.ProfileProgramClose, null, (_, _) =>
                {
                    program.CloseOnLeave = !program.CloseOnLeave;
                    Store.RequestSave();
                }) { Checked = program.CloseOnLeave });
                entry.DropDownItems.Add(Strings.RemoveItem, null, (_, _) =>
                {
                    Store.Config.ProfilePrograms[profile].Remove(program);
                    if (Store.Config.ProfilePrograms[profile].Count == 0)
                        Store.Config.ProfilePrograms.Remove(profile);
                    Store.RequestSave();
                });
                menu.DropDownItems.Add(entry);
            }
            if (menu.DropDownItems.Count > 0)
                menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(Strings.ProfileProgramAdd, null, (_, _) => AddProfileProgram(profile));
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.ProfileProgramsHint) { Enabled = false });
            items.Add(menu);
        }

        private void AddProfileProgram(string profile)
        {
            using var dialog = new OpenFileDialog
            {
                Title = Strings.ProfilePrograms(profile),
                Filter = Strings.ProgramFilter,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            if (!Store.Config.ProfilePrograms.TryGetValue(profile, out var list))
                Store.Config.ProfilePrograms[profile] = list = new();
            if (!list.Any(p => p.Path.Equals(dialog.FileName, StringComparison.OrdinalIgnoreCase)))
                list.Add(new ProfileProgram { Path = dialog.FileName });
            Store.RequestSave();
        }
    }
}
